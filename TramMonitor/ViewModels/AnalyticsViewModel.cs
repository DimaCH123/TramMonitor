
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TramMonitor.Services;
using TramMonitor.Simulation;

namespace TramMonitor.ViewModels
{
    public partial class AnalyticsViewModel : ObservableObject
    {
        private readonly SimulationEngine _simulation;
        private readonly RouteDataService _routeDataService;

        private readonly List<HeadwaySample> _headwaySamples = new();

        public ObservableCollection<HeadwayReportRow> HeadwayRows { get; }
            = new();

        public ObservableCollection<DeviationChartRow> DeviationRows { get; }
            = new();

        public ObservableCollection<RouteSummaryRow> RouteSummaryRows { get; }
            = new();

        public ObservableCollection<FleetReportRow> FleetRows { get; }
            = new();

        public ObservableCollection<AbsenceReportRow> AbsenceRows { get; }
            = new();

        [ObservableProperty]
        private string _currentTimeText = "00:00:00";

        [ObservableProperty]
        private string _totalTramsText = "0";

        [ObservableProperty]
        private string _finishedTramsText = "0";

        [ObservableProperty]
        private string _absenceEventsText = "0";

        [ObservableProperty]
        private string _logEntriesText = "0";

        [ObservableProperty]
        private string _averageAbsenceDurationText = "—";

        [ObservableProperty]
        private string _totalDelayText = "—";

        [ObservableProperty]
        private string _averageDelayText = "—";

        [ObservableProperty]
        private string _maximumDelayText = "—";

        [ObservableProperty]
        private string _delayEventsText = "0";

        public AnalyticsViewModel(
            SimulationEngine simulation,
            RouteDataService routeDataService)
        {
            _simulation = simulation;
            _routeDataService = routeDataService;

            _simulation.Updated += Recalculate;

            Recalculate();
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time == TimeSpan.MaxValue)
                return "—";

            return $"{(long)time.TotalHours:00}:" +
                   $"{time.Minutes:00}:" +
                   $"{time.Seconds:00}";
        }

        private static string FormatMinutes(double minutes)
        {
            return minutes.ToString("0.0") + " мин";
        }

        private double GetPlannedInterval(string route)
        {
            return _routeDataService.Routes
                .FirstOrDefault(r => r.RouteNumber == route)?
                .IntervalMinutes ?? 0;
        }

        // =========================================================
        // ПОЛНЫЙ ПЕРЕСЧЁТ
        // =========================================================

        private void Recalculate()
        {
            BuildHeadways();
            BuildDeviationChart();
            BuildRouteSummary();
            BuildFleetHistory();
            BuildAbsenceReport();
            UpdateSummary();

            CurrentTimeText = FormatTime(_simulation.CurrentTime);
            LogEntriesText = _simulation.LogEntries.Count.ToString();
        }

        // =========================================================
        // ФАКТИЧЕСКИЕ ИНТЕРВАЛЫ
        // =========================================================

        private void BuildHeadways()
        {
            HeadwayRows.Clear();
            _headwaySamples.Clear();

            var arrivals = _simulation.LogEntries
                .Where(e =>
                    e.Event == "Прибытие на остановку" &&
                    e.CurrentStop == "Олимпийская")
                .GroupBy(e => e.RouteNumber);

            var result = new List<HeadwayReportRow>();

            foreach (var routeGroup in arrivals)
            {
                var routeArrivals = routeGroup
                    .OrderBy(e => e.Time)
                    .ThenBy(e => e.Sequence)
                    .ToList();

                double planned = GetPlannedInterval(routeGroup.Key);

                for (int i = 1; i < routeArrivals.Count; i++)
                {
                    var previous = routeArrivals[i - 1];
                    var current = routeArrivals[i];

                    double actual =
                        (current.Time - previous.Time).TotalMinutes;

                    if (actual <= 0)
                        continue;

                    double deviation = actual - planned;

                    var sample = new HeadwaySample
                    {
                        Route = routeGroup.Key,
                        Time = current.Time,
                        Minutes = actual,
                        PlannedMinutes = planned,
                        Deviation = deviation,
                        TramNumber = current.NumberOnRoute
                    };

                    _headwaySamples.Add(sample);

                    result.Add(new HeadwayReportRow
                    {
                        TimeText = FormatTime(current.Time),
                        RouteNumber = current.RouteNumber,
                        TramNumber = current.NumberOnRoute,
                        ActualInterval = actual,
                        PlannedInterval = planned,
                        Deviation = deviation,
                        ActualLabel = FormatMinutes(actual),
                        PlannedLabel = FormatMinutes(planned),
                        DeviationLabel =
                            (deviation > 0 ? "+" : "") +
                            deviation.ToString("0.0") + " мин"
                    });
                }
            }

            foreach (var row in result
                         .OrderByDescending(r => r.TimeText)
                         .Take(100)
                         .Reverse())
            {
                HeadwayRows.Add(row);
            }
        }

        // =========================================================
        // ДИАГРАММА ОТКЛОНЕНИЙ ОТ ПЛАНОВОГО ИНТЕРВАЛА
        // =========================================================

        private void BuildDeviationChart()
        {
            DeviationRows.Clear();

            if (_headwaySamples.Count == 0)
                return;

            // Максимальное абсолютное отклонение определяет масштаб.
            double maxDeviation = _headwaySamples
                .Max(s => Math.Abs(s.Deviation));

            if (maxDeviation < 0.1)
                maxDeviation = 1;

            const double maxBarWidth = 140;

            var samples = _headwaySamples
                .OrderByDescending(s => s.Time)
                .Take(100)
                .OrderBy(s => s.Time)
                .ToList();

            foreach (var sample in samples)
            {
                double barWidth =
                    Math.Abs(sample.Deviation) /
                    maxDeviation *
                    maxBarWidth;

                DeviationRows.Add(new DeviationChartRow
                {
                    TimeText = FormatTime(sample.Time),
                    RouteNumber = sample.Route,
                    TramNumber = sample.TramNumber,
                    Deviation = sample.Deviation,

                    LeftBarWidth = sample.Deviation < 0
                        ? barWidth
                        : 0,

                    RightBarWidth = sample.Deviation > 0
                        ? barWidth
                        : 0,

                    DeviationLabel =
                        (sample.Deviation > 0 ? "+" : "") +
                        sample.Deviation.ToString("0.0") + " мин",

                    IsEarly = sample.Deviation < 0,
                    IsLate = sample.Deviation > 0
                });
            }
        }

        // =========================================================
        // СВОДКА ПО КАЖДОМУ МАРШРУТУ
        // =========================================================

        private void BuildRouteSummary()
        {
            RouteSummaryRows.Clear();

            foreach (var route in _routeDataService.Routes)
            {
                var samples = _headwaySamples
                    .Where(s => s.Route == route.RouteNumber)
                    .ToList();

                double planned = route.IntervalMinutes;

                if (samples.Count == 0)
                {
                    RouteSummaryRows.Add(new RouteSummaryRow
                    {
                        RouteNumber = route.RouteNumber,
                        PlannedInterval = FormatMinutes(planned),
                        AverageInterval = "—",
                        MinimumInterval = "—",
                        MaximumInterval = "—",
                        AverageDeviation = "—",
                        AverageDelay = "—",
                        MaximumDelay = "—",
                        DelayCount = "0"
                    });

                    continue;
                }

                var positiveDeviations = samples
                    .Where(s => s.Deviation > 0)
                    .Select(s => s.Deviation)
                    .ToList();

                RouteSummaryRows.Add(new RouteSummaryRow
                {
                    RouteNumber = route.RouteNumber,

                    PlannedInterval = FormatMinutes(planned),

                    AverageInterval = FormatMinutes(
                        samples.Average(s => s.Minutes)),

                    MinimumInterval = FormatMinutes(
                        samples.Min(s => s.Minutes)),

                    MaximumInterval = FormatMinutes(
                        samples.Max(s => s.Minutes)),

                    AverageDeviation = FormatSignedMinutes(
                        samples.Average(s => s.Deviation)),

                    AverageDelay = positiveDeviations.Count == 0
                        ? "—"
                        : FormatMinutes(positiveDeviations.Average()),

                    MaximumDelay = positiveDeviations.Count == 0
                        ? "—"
                        : FormatMinutes(positiveDeviations.Max()),

                    DelayCount = positiveDeviations.Count.ToString()
                });
            }
        }

        private static string FormatSignedMinutes(double minutes)
        {
            return (minutes > 0 ? "+" : "") +
                   minutes.ToString("0.0") + " мин";
        }

        // =========================================================
        // ИСТОРИЯ КОЛИЧЕСТВА ТРАМВАЕВ НА ЛИНИИ
        // =========================================================

        private void BuildFleetHistory()
        {
            FleetRows.Clear();

            var states = _simulation.Trams.ToDictionary(
                t => t.Id,
                t => "Ожидание");

            var points = new List<FleetReportRow>();

            foreach (var timeGroup in _simulation.LogEntries
                         .OrderBy(e => e.Time)
                         .ThenBy(e => e.Sequence)
                         .GroupBy(e => e.Time))
            {
                foreach (var entry in timeGroup)
                {
                    if (states.ContainsKey(entry.TramId))
                        states[entry.TramId] = entry.StatusAfter;
                }

                int onLine = states.Values.Count(status =>
                    status == "В пути" ||
                    status == "На остановке");

                points.Add(new FleetReportRow
                {
                    TimeText = FormatTime(timeGroup.Key),
                    OnLineCount = onLine,
                    TotalCount = _simulation.Trams.Count,
                    OnLineLabel = $"{onLine} / {_simulation.Trams.Count}"
                });
            }

            int step = Math.Max(1, points.Count / 80);

            for (int i = 0; i < points.Count; i += step)
                FleetRows.Add(points[i]);

            if (points.Count > 0 &&
                (FleetRows.Count == 0 ||
                 FleetRows[^1].TimeText != points[^1].TimeText))
            {
                FleetRows.Add(points[^1]);
            }
        }

        // =========================================================
        // ОБЕДЫ И ПЕРЕСМЕНКИ
        // =========================================================

        private void BuildAbsenceReport()
        {
            AbsenceRows.Clear();

            var result = new List<AbsenceReportRow>();

            foreach (var tramGroup in _simulation.LogEntries
                         .GroupBy(e => e.TramId))
            {
                var entries = tramGroup
                    .OrderBy(e => e.Sequence)
                    .ToList();

                TramLogEntry? lunchStart = null;
                TramLogEntry? shiftStart = null;

                foreach (var entry in entries)
                {
                    if (entry.Event == "Начало обеда")
                    {
                        lunchStart = entry;
                    }
                    else if (entry.Event == "Окончание обеда" &&
                             lunchStart != null)
                    {
                        result.Add(CreateAbsenceRow(
                            lunchStart, entry, "Обед"));

                        lunchStart = null;
                    }

                    if (entry.Event == "Начало пересменки")
                    {
                        shiftStart = entry;
                    }
                    else if (entry.Event == "Окончание пересменки" &&
                             shiftStart != null)
                    {
                        result.Add(CreateAbsenceRow(
                            shiftStart, entry, "Пересменка"));

                        shiftStart = null;
                    }
                }
            }

            foreach (var row in result.OrderBy(r => r.StartTime))
                AbsenceRows.Add(row);
        }

        private AbsenceReportRow CreateAbsenceRow(
            TramLogEntry start,
            TramLogEntry end,
            string type)
        {
            TimeSpan duration = end.Time - start.Time;

            string before = GetAverageInterval(
                start.RouteNumber,
                start.Time - TimeSpan.FromMinutes(30),
                start.Time);

            string during = GetAverageInterval(
                start.RouteNumber,
                start.Time,
                end.Time);

            string after = GetAverageInterval(
                start.RouteNumber,
                end.Time,
                end.Time + TimeSpan.FromMinutes(30));

            return new AbsenceReportRow
            {
                StartTime = FormatTime(start.Time),
                EndTime = FormatTime(end.Time),
                RouteNumber = start.RouteNumber,
                TramNumber = start.NumberOnRoute,
                Type = type,
                Duration = FormatMinutes(duration.TotalMinutes),
                BeforeInterval = before,
                DuringInterval = during,
                AfterInterval = after,
                EffectSummary =
                    $"До: {before}; во время: {during}; после: {after}"
            };
        }

        private string GetAverageInterval(
            string route,
            TimeSpan from,
            TimeSpan to)
        {
            var values = _headwaySamples
                .Where(s =>
                    s.Route == route &&
                    s.Time >= from &&
                    s.Time <= to)
                .Select(s => s.Minutes)
                .ToList();

            if (values.Count == 0)
                return "—";

            return FormatMinutes(values.Average());
        }

        // =========================================================
        // ОБЩАЯ СТАТИСТИКА
        // =========================================================

        private void UpdateSummary()
        {
            TotalTramsText = _simulation.Trams.Count.ToString();

            FinishedTramsText = _simulation.Trams.Count(
                t => t.Status == TramStatus.Finished).ToString();

            AbsenceEventsText = AbsenceRows.Count.ToString();

            var positiveDeviations = _headwaySamples
                .Where(s => s.Deviation > 0)
                .Select(s => s.Deviation)
                .ToList();

            DelayEventsText = positiveDeviations.Count.ToString();

            TotalDelayText = positiveDeviations.Count == 0
                ? "0,0 мин"
                : FormatMinutes(positiveDeviations.Sum());

            AverageDelayText = positiveDeviations.Count == 0
                ? "—"
                : FormatMinutes(positiveDeviations.Average());

            MaximumDelayText = positiveDeviations.Count == 0
                ? "—"
                : FormatMinutes(positiveDeviations.Max());

            double totalMinutes = 0;
            int count = 0;

            foreach (var start in _simulation.LogEntries.Where(e =>
                         e.Event == "Начало обеда" ||
                         e.Event == "Начало пересменки"))
            {
                string endEvent = start.Event == "Начало обеда"
                    ? "Окончание обеда"
                    : "Окончание пересменки";

                var end = _simulation.LogEntries.FirstOrDefault(e =>
                    e.TramId == start.TramId &&
                    e.Event == endEvent &&
                    e.Sequence > start.Sequence);

                if (end == null)
                    continue;

                totalMinutes += (end.Time - start.Time).TotalMinutes;
                count++;
            }

            AverageAbsenceDurationText = count == 0
                ? "—"
                : FormatMinutes(totalMinutes / count);
        }
    }

    // =============================================================
    // МОДЕЛИ ДАННЫХ
    // =============================================================

    public class HeadwaySample
    {
        public string Route { get; set; } = "";
        public TimeSpan Time { get; set; }
        public double Minutes { get; set; }
        public double PlannedMinutes { get; set; }
        public double Deviation { get; set; }
        public int TramNumber { get; set; }
    }

    public class HeadwayReportRow
    {
        public string TimeText { get; set; } = "";
        public string RouteNumber { get; set; } = "";
        public int TramNumber { get; set; }
        public double ActualInterval { get; set; }
        public double PlannedInterval { get; set; }
        public double Deviation { get; set; }
        public string ActualLabel { get; set; } = "";
        public string PlannedLabel { get; set; } = "";
        public string DeviationLabel { get; set; } = "";
    }

    public class DeviationChartRow
    {
        public string TimeText { get; set; } = "";
        public string RouteNumber { get; set; } = "";
        public int TramNumber { get; set; }
        public double Deviation { get; set; }
        public double LeftBarWidth { get; set; }
        public double RightBarWidth { get; set; }
        public string DeviationLabel { get; set; } = "";
        public bool IsEarly { get; set; }
        public bool IsLate { get; set; }
    }

    public class RouteSummaryRow
    {
        public string RouteNumber { get; set; } = "";
        public string PlannedInterval { get; set; } = "";
        public string AverageInterval { get; set; } = "";
        public string MinimumInterval { get; set; } = "";
        public string MaximumInterval { get; set; } = "";
        public string AverageDeviation { get; set; } = "";
        public string AverageDelay { get; set; } = "";
        public string MaximumDelay { get; set; } = "";
        public string DelayCount { get; set; } = "";
    }

    public class FleetReportRow
    {
        public string TimeText { get; set; } = "";
        public int OnLineCount { get; set; }
        public int TotalCount { get; set; }
        public string OnLineLabel { get; set; } = "";
    }

    public class AbsenceReportRow
    {
        public string StartTime { get; set; } = "";
        public string EndTime { get; set; } = "";
        public string RouteNumber { get; set; } = "";
        public int TramNumber { get; set; }
        public string Type { get; set; } = "";
        public string Duration { get; set; } = "";
        public string BeforeInterval { get; set; } = "";
        public string DuringInterval { get; set; } = "";
        public string AfterInterval { get; set; } = "";
        public string EffectSummary { get; set; } = "";
    }
}
