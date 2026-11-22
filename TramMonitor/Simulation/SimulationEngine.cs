using System;
using System.Collections.Generic;
using System.Linq;
using TramMonitor.Models;
using TramMonitor.Services;
using System.IO;
using System.Text;

namespace TramMonitor.Simulation
{
    public class TramLogEntry
    {
        public int Sequence { get; set; }
        public TimeSpan Time { get; set; }
        public int TramId { get; set; }
        public string RouteNumber { get; set; } = "";
        public int NumberOnRoute { get; set; }

        public string Event { get; set; } = "";
        public string StatusBefore { get; set; } = "";
        public string StatusAfter { get; set; } = "";

        public string CurrentStop { get; set; } = "";
        public string NextStop { get; set; } = "";

        public int CirclesBeforeLunch { get; set; }
        public int CirclesAfterLunch { get; set; }
        public int CurrentShift { get; set; }

        public string Details { get; set; } = "";
    }
    public class SimulationEngine
    {
        private readonly Dictionary<string, int> _stopOwners =
            new(StringComparer.OrdinalIgnoreCase);
        public List<TramLogEntry> LogEntries { get; } = new();
        private readonly HashSet<string> _loggedStopWaits = new();
        private readonly TrackGeometryService _trackGeometryService;
        public List<SimulatedTram> Trams { get; } = new();
        public TimeSpan CurrentTime { get; private set; }
        public event Action? Updated;
        public SimulationEngine(RouteDataService routeDataService)
    : this(routeDataService, new TrackGeometryService())
        {
        }
        public SimulationEngine(
            RouteDataService routeDataService,
            TrackGeometryService trackGeometryService)
        {
            _trackGeometryService = trackGeometryService;

            RebuildTrams(routeDataService);
        }

        public void RebuildTrams(RouteDataService routeDataService)
        {
            RebuildTrams(routeDataService, CurrentTime);
        }

        public void SetTimeOfDay(RouteDataService routeDataService, TimeSpan timeOfDay)
        {
            if (timeOfDay < TimeSpan.Zero || timeOfDay >= TimeSpan.FromDays(1))
                throw new ArgumentOutOfRangeException(nameof(timeOfDay));

            RebuildTrams(routeDataService, timeOfDay);
        }
        public void Reset(RouteDataService routeDataService)
        {
            RebuildTrams(routeDataService, TimeSpan.Zero);
        }
        private void RebuildTrams(RouteDataService routeDataService, TimeSpan targetTime)
        {

            // Полностью очищаем старую модель.
            Trams.Clear();
            _stopOwners.Clear();
            LogEntries.Clear();
            _loggedStopWaits.Clear();

            // Начинаем воспроизведение с начала смены.
            CurrentTime = TimeSpan.Zero;

            int tramId = 1;

            foreach (var route in routeDataService.Routes)
            {
                if (!route.IsEnabledInSimulation || route.TramCount <= 0)
                    continue;

                var fullRouteStops = route.GetFullRouteStops();

                if (fullRouteStops.Count < 2)
                    continue;

                var routeGeometry = _trackGeometryService
                    .GetRouteGeometry(route.RouteNumber);

                if (routeGeometry.Count != fullRouteStops.Count)
                {
                    throw new InvalidOperationException(
                        $"Маршрут {route.RouteNumber}: количество остановок " +
                        $"({fullRouteStops.Count}) не совпадает с количеством " +
                        $"координат ({routeGeometry.Count}).");
                }

                for (int i = 1; i <= route.TramCount; i++)
                {

                    TimeSpan departureTime = route.StartTime
                        + TimeSpan.FromMinutes((i - 1) * route.IntervalMinutes);

                    // Рассчитываем время одного перегона.
                    TimeSpan travelTime =
                        TimeSpan.FromMinutes(route.TravelTimeMinutes);

                    TimeSpan stopDuration =
                        TimeSpan.FromSeconds(route.StopDurationSeconds);

                    // Количество переходов между остановками за один круг.
                    int transitionsPerCircle = fullRouteStops.Count - 1;

                    // Продолжительность одного полного круга.
                    TimeSpan circleDuration = TimeSpan.FromTicks(
                        (travelTime + stopDuration).Ticks * transitionsPerCircle
                    );

                    // Продолжительность одной смены:
                    // все круги + обед.
                    TimeSpan shiftDuration = TimeSpan.FromTicks(
                        circleDuration.Ticks *
                        (route.CirclesBeforeLunch + route.CirclesAfterLunch)
                    ) + TimeSpan.FromMinutes(route.LunchMinutes);

                    // Полная продолжительность работы:
                    // первая смена + пересменка + вторая смена.
                    TimeSpan totalServiceDuration =
                        shiftDuration * 2 +
                        TimeSpan.FromMinutes(route.ShiftChangeMinutes);

                    // Индивидуальное плановое время окончания работы.
                    TimeSpan serviceEndTime =
                        departureTime + totalServiceDuration;

                    Trams.Add(new SimulatedTram(
                        tramId++,
                        route.RouteNumber,
                        i,
                        new List<StopInfo>(fullRouteStops),
                        new List<TrackLocation>(routeGeometry),
                        departureTime)
                    {
                        DepotLocation = _trackGeometryService.DepotLocation,

                        ServiceEndTime = serviceEndTime,
                        NextEventTime = departureTime,

                        TravelTime = travelTime,
                        StopDuration = stopDuration,

                        CirclesBeforeLunch = route.CirclesBeforeLunch,
                        CirclesAfterLunch = route.CirclesAfterLunch,

                        LunchTime = TimeSpan.FromMinutes(route.LunchMinutes),
                        ShiftChangeTime = TimeSpan.FromMinutes(route.ShiftChangeMinutes)
                    });

                    var createdTram = Trams[^1];

                    LogEvent(
                        createdTram,
                        "Запланирован выход на линию",
                        $"Плановое время отправления: {FormatLogTime(departureTime)}");

                }
            }

            // Повторно проигрываем симуляцию до прежнего виртуального времени.
            Advance(targetTime);
        }
        private static string GetStopKey(SimulatedTram tram, int stopIndex)
        {
            string stopName = tram.RouteStops[stopIndex].Name.Trim();
            string trackId = tram.TrackGeometry[stopIndex].TrackId;

            return $"{stopName}|{trackId}";
        }

        private void ReleaseStop(SimulatedTram tram)
        {
            if (tram.IsAtDepot)
                return;

            string stopKey = GetStopKey(tram, tram.CurrentStopIndex);

            if (_stopOwners.TryGetValue(stopKey, out int ownerId)
                && ownerId == tram.Id)
            {
                _stopOwners.Remove(stopKey);
            }
        }
        private void SendToDepot(SimulatedTram tram)
        {
            // Сначала освобождаем остановку, пока трамвай
            // ещё считается находящимся на ней.
            ReleaseStop(tram);

            // В депо трамвай не занимает остановку.
            tram.IsAtDepot = true;
        }
        private void FinishTram(SimulatedTram tram)
        {
            SendToDepot(tram);
            tram.Status = TramStatus.Finished;
            tram.NextEventTime = TimeSpan.MaxValue;
        }
        private bool TryReserveStop(SimulatedTram tram, int stopIndex)
        {
            string stopKey = GetStopKey(tram, stopIndex);

            if (_stopOwners.TryGetValue(stopKey, out int ownerId))
            {
                if (ownerId != tram.Id)
                    return false;
            }

            _stopOwners[stopKey] = tram.Id;
            return true;
        }
        private void ProcessTramEvent(SimulatedTram tram)
        {
            TramStatus oldStatus = tram.Status;

            int oldStopIndex = tram.CurrentStopIndex;
            bool wasAtDepot = tram.IsAtDepot;

            int oldCirclesBeforeLunch = tram.CirclesDoneBeforeLunch;
            int oldCirclesAfterLunch = tram.CirclesDoneAfterLunch;
            int oldShift = tram.CurrentShift;

            ProcessTramEventCore(tram);

            bool statusChanged = oldStatus != tram.Status;
            bool stopChanged = oldStopIndex != tram.CurrentStopIndex;
            bool depotChanged = wasAtDepot != tram.IsAtDepot;

            bool countersChanged =
                oldCirclesBeforeLunch != tram.CirclesDoneBeforeLunch ||
                oldCirclesAfterLunch != tram.CirclesDoneAfterLunch ||
                oldShift != tram.CurrentShift;

            if (!statusChanged && !stopChanged && !depotChanged && !countersChanged)
                return;

            string eventName;

            if (oldStatus == TramStatus.OnLunch &&
                tram.Status == TramStatus.WaitingForDeparture)
            {
                eventName = "Окончание обеда";
            }
            else if (oldStatus == TramStatus.OnShiftChange &&
                     tram.Status == TramStatus.WaitingForDeparture)
            {
                eventName = "Окончание пересменки";
            }
            else if (tram.Status == TramStatus.Finished)
            {
                eventName = "Завершение смены";
            }
            else if (tram.Status == TramStatus.Moving)
            {
                eventName = "Начало движения";
            }
            else if (tram.Status == TramStatus.AtStop)
            {
                eventName = "Прибытие на остановку";
            }
            else if (tram.Status == TramStatus.OnLunch)
            {
                eventName = "Начало обеда";
            }
            else if (tram.Status == TramStatus.OnShiftChange)
            {
                eventName = "Начало пересменки";
            }
            else if (tram.Status == TramStatus.WaitingForDeparture)
            {
                eventName = "Возвращение к работе";
            }
            else
            {
                eventName = "Изменение состояния";
            }

            string oldStop = wasAtDepot
                ? "Депо"
                : GetStopName(tram, oldStopIndex);

            string newStop = tram.IsAtDepot
                ? "Депо"
                : GetStopName(tram, tram.CurrentStopIndex);

            string nextEvent = FormatLogTime(tram.NextEventTime);

            string details =
                $"Местоположение: {oldStop} → {newStop}; " +
                $"Круги до обеда: {tram.CirclesDoneBeforeLunch}; " +
                $"Круги после обеда: {tram.CirclesDoneAfterLunch}; " +
                $"Смена: {tram.CurrentShift}; " +
                $"Следующее событие: {nextEvent}";

            LogEvent(tram, eventName, details, oldStatus);
        }
        private void ProcessTramEventCore(SimulatedTram tram)
        {
            if (tram.Status == TramStatus.Finished)
            {
                tram.NextEventTime = TimeSpan.MaxValue;
                return;
            }

            // 1. Возвращение из обеда или пересменки.
            if (tram.Status == TramStatus.OnLunch
                || tram.Status == TramStatus.OnShiftChange)
            {
                if (tram.NextEventTime >= tram.BackToWorkTime)
                {
                    TramStatus previousStatus = tram.Status;

                    tram.CurrentStopIndex = 0;
                    tram.NextStopIndex = 0;
                    tram.IsAtDepot = true;

                    tram.Status = TramStatus.WaitingForDeparture;

                    tram.NextEventTime = CurrentTime;

                    if (previousStatus == TramStatus.OnLunch)
                    {
                        tram.IsLunchCompleted = true;
                    }
                    else if (previousStatus == TramStatus.OnShiftChange)
                    {
                        tram.CurrentShift = 2;
                        tram.IsLunchCompleted = false;
                        tram.CirclesDoneBeforeLunch = 0;
                        tram.CirclesDoneAfterLunch = 0;
                    }
                }

                return;
            }

            // 2. Ожидание отправления или стоянка на остановке.
            if (tram.Status == TramStatus.WaitingForDeparture
                || tram.Status == TramStatus.AtStop)
            {

                // Проверяем, достиг ли трамвай конечной остановки
                // и завершил ли полный круг.

                bool completesCircle = false;

                // Проверяем, достиг ли трамвай конечной остановки
                // и завершил ли полный круг.
                if (tram.CurrentStopIndex >= tram.RouteStops.Count - 1)
                {

                    // Рассчитываем количество кругов после завершения
                    // текущего круга, пока не изменяя реальные счётчики.
                    int nextCirclesBeforeLunch = tram.CirclesDoneBeforeLunch
                        + (tram.IsLunchCompleted ? 0 : 1);

                    int nextCirclesAfterLunch = tram.CirclesDoneAfterLunch
                        + (tram.IsLunchCompleted ? 1 : 0);

                    // А. Завершение второй смены.
                    if (tram.CurrentShift == 2
                        && tram.IsLunchCompleted
                        && nextCirclesAfterLunch >= tram.CirclesAfterLunch)
                    {
                        tram.CirclesDoneAfterLunch = nextCirclesAfterLunch;
                        FinishTram(tram);
                        return;
                    }

                    // Б. Пересменка после первой смены.
                    if (tram.CurrentShift == 1
                        && tram.IsLunchCompleted
                        && nextCirclesAfterLunch >= tram.CirclesAfterLunch)
                    {
                        tram.CirclesDoneAfterLunch = nextCirclesAfterLunch;

                        SendToDepot(tram);

                        tram.Status = TramStatus.OnShiftChange;
                        tram.BackToWorkTime = CurrentTime + tram.ShiftChangeTime;

                        tram.NextEventTime = tram.BackToWorkTime;

                        return;
                    }

                    // В. Обед.
                    if (!tram.IsLunchCompleted
                        && nextCirclesBeforeLunch >= tram.CirclesBeforeLunch)
                    {
                        tram.CirclesDoneBeforeLunch = nextCirclesBeforeLunch;

                        SendToDepot(tram);

                        tram.Status = TramStatus.OnLunch;
                        tram.BackToWorkTime = CurrentTime + tram.LunchTime;

                        tram.NextEventTime = tram.BackToWorkTime;

                        return;
                    }

                    // Следующий круг. Счётчик пока не увеличиваем:
                    // сначала нужно получить свободную остановку.
                    tram.NextStopIndex = 1;
                    completesCircle = true;
                }
                else
                {
                    // Движение к следующей остановке.
                    tram.NextStopIndex = tram.CurrentStopIndex + 1;
                }


                // Проверяем, свободна ли следующая остановка.
                if (!TryReserveStop(tram, tram.NextStopIndex))
                {
                    string waitKey = $"{tram.Id}|{tram.NextStopIndex}";

                    if (_loggedStopWaits.Add(waitKey))
                    {
                        LogEvent(
                            tram,
                            "Ожидание свободной остановки",
                            $"Не удалось занять остановку: " +
                            $"{GetStopName(tram, tram.NextStopIndex)}");
                    }

                    tram.NextEventTime = CurrentTime + TimeSpan.FromSeconds(1);
                    return;
                }
                // Круг засчитывается только после успешного
                // резервирования первой остановки следующего круга.
                if (completesCircle)
                {
                    if (!tram.IsLunchCompleted)
                        tram.CirclesDoneBeforeLunch++;
                    else
                        tram.CirclesDoneAfterLunch++;
                }
                // Трамвай получил остановку — ожидание завершено.
                _loggedStopWaits.Remove($"{tram.Id}|{tram.NextStopIndex}");

                // Освобождаем предыдущую остановку.
                ReleaseStop(tram);

                // Если завершили круг, начинаем новый
                // с первой точки маршрута — Олимпийской.
                if (completesCircle)
                {
                    tram.CurrentStopIndex = 0;
                }

                tram.IsAtDepot = false;
                tram.Status = TramStatus.Moving;
                tram.NextEventTime = CurrentTime + tram.TravelTime;
            }
            // 3. Прибытие на следующую остановку.
            else if (tram.Status == TramStatus.Moving)
            {
                tram.CurrentStopIndex = tram.NextStopIndex;

                tram.Status = TramStatus.AtStop;
                tram.NextEventTime = CurrentTime + tram.StopDuration;
            }
        }



        private void ProcessEventsAtCurrentTime()
        {
            foreach (var tram in Trams
                .OrderBy(t => t.NextEventTime)
                .ThenBy(t => t.Id))
            {
                int safetyCounter = 0;

                while (tram.NextEventTime <= CurrentTime)
                {
                    ProcessTramEvent(tram);

                    safetyCounter++;

                    if (safetyCounter > 100)
                    {
                        throw new InvalidOperationException(
                            $"Слишком много событий подряд у трамвая №{tram.Id}.");
                    }
                }
            }
        }
        private static string GetStopName(SimulatedTram tram, int index)
        {
            if (index < 0 || index >= tram.RouteStops.Count)
                return "—";

            return tram.RouteStops[index].Name;
        }

        private static string GetStatusName(TramStatus status)
        {
            return status switch
            {
                TramStatus.WaitingForDeparture => "Ожидание",
                TramStatus.Moving => "В пути",
                TramStatus.AtStop => "На остановке",
                TramStatus.OnLunch => "Обед",
                TramStatus.OnShiftChange => "Пересменка",
                TramStatus.Finished => "Смена окончена",
                _ => "Неизвестно"
            };
        }

        private static string FormatLogTime(TimeSpan time)
        {
            if (time == TimeSpan.MaxValue)
                return "—";

            return $"{(long)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}";
        }

        private void LogEvent(
            SimulatedTram tram,
            string eventName,
            string details = "",
            TramStatus? previousStatus = null)
        {
            LogEntries.Add(new TramLogEntry
            {
                Sequence = LogEntries.Count + 1,
                Time = CurrentTime,

                TramId = tram.Id,
                RouteNumber = tram.RouteNumber,
                NumberOnRoute = tram.NumberOnRoute,

                Event = eventName,

                StatusBefore = previousStatus.HasValue
                    ? GetStatusName(previousStatus.Value)
                    : GetStatusName(tram.Status),

                StatusAfter = GetStatusName(tram.Status),

                CurrentStop = tram.IsAtDepot
                    ? "Депо"
                    : GetStopName(tram, tram.CurrentStopIndex),

                NextStop = tram.Status == TramStatus.Moving
                    ? GetStopName(tram, tram.NextStopIndex)
                    : "—",

                CirclesBeforeLunch = tram.CirclesDoneBeforeLunch,
                CirclesAfterLunch = tram.CirclesDoneAfterLunch,
                CurrentShift = tram.CurrentShift,

                Details = details
            });
        }

        public void Advance(TimeSpan elapsed)
        {
            if (elapsed < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(elapsed));

            TimeSpan targetTime = CurrentTime + elapsed;

            // Обрабатываем события, назначенные на текущее время.
            ProcessEventsAtCurrentTime();

            while (CurrentTime < targetTime)
            {
                // Находим ближайшее событие среди всех трамваев.
                TimeSpan nextEventTime = Trams
                    .Where(t =>
                        t.NextEventTime > CurrentTime &&
                        t.NextEventTime <= targetTime &&
                        t.NextEventTime != TimeSpan.MaxValue)
                    .Select(t => t.NextEventTime)
                    .DefaultIfEmpty(targetTime)
                    .Min();

                // Перемещаем виртуальное время точно к событию.
                CurrentTime = nextEventTime;

                // Обрабатываем все события в этот момент.
                ProcessEventsAtCurrentTime();
            }
            Updated?.Invoke();
        }

        private static string EscapeCsv(string value)
        {
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        public void ExportLog(string filePath)
        {
            var lines = new List<string>
    {
        "№ записи;Время;ID трамвая;Маршрут;Номер на маршруте;" +
        "Событие;Предыдущий статус;Новый статус;" +
        "Текущая остановка;Следующая остановка;" +
        "Круги до обеда;Круги после обеда;Смена;Подробности"
    };

            foreach (var entry in LogEntries)
            {
                lines.Add(string.Join(";",
                    entry.Sequence,
                    EscapeCsv(FormatLogTime(entry.Time)),
                    entry.TramId,
                    EscapeCsv(entry.RouteNumber),
                    entry.NumberOnRoute,
                    EscapeCsv(entry.Event),
                    EscapeCsv(entry.StatusBefore),
                    EscapeCsv(entry.StatusAfter),
                    EscapeCsv(entry.CurrentStop),
                    EscapeCsv(entry.NextStop),
                    entry.CirclesBeforeLunch,
                    entry.CirclesAfterLunch,
                    entry.CurrentShift,
                    EscapeCsv(entry.Details)
                ));
            }

            File.WriteAllLines(filePath, lines, new UTF8Encoding(true));
        }
    }
}
