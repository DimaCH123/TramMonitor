
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Threading;
using TramMonitor.Models;
using TramMonitor.Services;
using TramMonitor.Simulation;

namespace TramMonitor.ViewModels
{
    public partial class MonitoringViewModel : ObservableObject
    {
        public SimulationEngine Simulation { get; }
        public RouteDataService RouteDataService { get; }
        public ObservableCollection<TramLogEntry> RecentEvents { get; } = new();
        private readonly DispatcherTimer _timer;
        private bool _isLoadingRouteSettings;

        [ObservableProperty]
        private RouteInfo? _selectedRouteForSettings;

        [ObservableProperty]
        private int _selectedRouteTramCount;

        [ObservableProperty]
        private int _selectedRouteInterval;

        [ObservableProperty]
        private string _selectedTimeText = "00:00:00";

        [ObservableProperty]
        private double _selectedRouteTravelTime = 2.0;

        [ObservableProperty]
        private int _selectedRouteStopDuration = 20;

        [ObservableProperty]
        private bool _isRunning;

        [ObservableProperty]
        private int _selectedRouteCirclesBeforeLunch = 4;

        [ObservableProperty]
        private int _selectedRouteCirclesAfterLunch = 3;

        [ObservableProperty]
        private int _selectedRouteShiftChange = 15;

        [ObservableProperty]
        private int _selectedRouteLunchTime = 20;

        public event Action? SimulationUpdated;
        private int _statisticsTickCounter;
        public string CurrentTimeText =>
            TimeSpan.FromTicks(
                Simulation.CurrentTime.Ticks % TimeSpan.TicksPerDay
            ).ToString(@"hh\:mm\:ss");

        public MonitoringViewModel(
            SimulationEngine simulation,
            RouteDataService routeDataService)
        {
            Simulation = simulation;
            RouteDataService = routeDataService;

            SelectedRouteForSettings =
                RouteDataService.Routes.FirstOrDefault(
                    r => r.RouteNumber == "4");

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };

            _timer.Tick += Timer_Tick;

            UpdateTramCounts();
        }

        partial void OnSelectedRouteForSettingsChanged(RouteInfo? value)
        {
            if (value == null)
                return;

            _isLoadingRouteSettings = true;

            SelectedRouteTramCount = value.TramCount;
            SelectedRouteInterval = value.IntervalMinutes;
            SelectedRouteTravelTime = value.TravelTimeMinutes;
            SelectedRouteStopDuration = value.StopDurationSeconds;
            SelectedRouteCirclesBeforeLunch = value.CirclesBeforeLunch;
            SelectedRouteCirclesAfterLunch = value.CirclesAfterLunch;
            SelectedRouteShiftChange = value.ShiftChangeMinutes;
            SelectedRouteLunchTime = value.LunchMinutes;

            _isLoadingRouteSettings = false;
        }

        // ---------- ПОДСЧЁТ ТРАМВАЕВ ----------

        private int CountTrams(
            string route,
            Func<SimulatedTram, bool> predicate)
        {
            return Simulation.Trams.Count(t =>
                t.RouteNumber == route && predicate(t));
        }

        public int TramsOnLineCount =>
            Simulation.Trams.Count(t =>
                t.Status == TramStatus.Moving ||
                t.Status == TramStatus.AtStop);

        public int TramsOnLineRoute4Count =>
            CountTrams("4", t =>
                t.Status == TramStatus.Moving ||
                t.Status == TramStatus.AtStop);

        public int TramsOnLineRoute8Count =>
            CountTrams("8", t =>
                t.Status == TramStatus.Moving ||
                t.Status == TramStatus.AtStop);

        public string TramsOnLineBreakdown =>
            $"№4: {TramsOnLineRoute4Count}   №8: {TramsOnLineRoute8Count}";

        public int TramsOnLunchCount =>
            Simulation.Trams.Count(t =>
                t.Status == TramStatus.OnLunch);

        public int TramsOnLunchRoute4Count =>
            CountTrams("4", t => t.Status == TramStatus.OnLunch);

        public int TramsOnLunchRoute8Count =>
            CountTrams("8", t => t.Status == TramStatus.OnLunch);

        public string TramsOnLunchBreakdown =>
            $"№4: {TramsOnLunchRoute4Count}   №8: {TramsOnLunchRoute8Count}";

        public int TramsOnShiftChangeCount =>
            Simulation.Trams.Count(t =>
                t.Status == TramStatus.OnShiftChange);

        public int TramsOnShiftChangeRoute4Count =>
            CountTrams("4", t => t.Status == TramStatus.OnShiftChange);

        public int TramsOnShiftChangeRoute8Count =>
            CountTrams("8", t => t.Status == TramStatus.OnShiftChange);

        public string TramsOnShiftChangeBreakdown =>
            $"№4: {TramsOnShiftChangeRoute4Count}   №8: {TramsOnShiftChangeRoute8Count}";

        public int TotalTramsCount => Simulation.Trams.Count;

        public int TotalRoute4Count =>
            Simulation.Trams.Count(t => t.RouteNumber == "4");

        public int TotalRoute8Count =>
            Simulation.Trams.Count(t => t.RouteNumber == "8");

        public string TotalTramsBreakdown =>
            $"№4: {TotalRoute4Count}   №8: {TotalRoute8Count}";

        private void UpdateTramCounts()
        {
            OnPropertyChanged(nameof(TramsOnLineCount));
            OnPropertyChanged(nameof(TramsOnLineRoute4Count));
            OnPropertyChanged(nameof(TramsOnLineRoute8Count));
            OnPropertyChanged(nameof(TramsOnLineBreakdown));

            OnPropertyChanged(nameof(TramsOnLunchCount));
            OnPropertyChanged(nameof(TramsOnLunchRoute4Count));
            OnPropertyChanged(nameof(TramsOnLunchRoute8Count));
            OnPropertyChanged(nameof(TramsOnLunchBreakdown));

            OnPropertyChanged(nameof(TramsOnShiftChangeCount));
            OnPropertyChanged(nameof(TramsOnShiftChangeRoute4Count));
            OnPropertyChanged(nameof(TramsOnShiftChangeRoute8Count));
            OnPropertyChanged(nameof(TramsOnShiftChangeBreakdown));

            OnPropertyChanged(nameof(TotalTramsCount));
            OnPropertyChanged(nameof(TotalRoute4Count));
            OnPropertyChanged(nameof(TotalRoute8Count));
            OnPropertyChanged(nameof(TotalTramsBreakdown));
            UpdateRecentEvents();
        }
        private void UpdateRecentEvents()
        {
            RecentEvents.Clear();

            TimeSpan cutoff =
                Simulation.CurrentTime - TimeSpan.FromMinutes(20);

            for (int i = Simulation.LogEntries.Count - 1; i >= 0; i--)
            {
                var entry = Simulation.LogEntries[i];

                if (entry.Time < cutoff)
                    break;

                if (entry.Time <= Simulation.CurrentTime)
                    RecentEvents.Add(entry);
            }
        }
        // ---------- ИЗМЕНЕНИЕ ПАРАМЕТРОВ ----------

        private void ApplySettings()
        {
            if (_isLoadingRouteSettings)
                return;

            if (SelectedRouteForSettings == null)
                return;

            // Пересоздаём модель, сохраняя текущее виртуальное время.
            Simulation.RebuildTrams(RouteDataService);

            SelectedTimeText = CurrentTimeText;

            OnPropertyChanged(nameof(CurrentTimeText));
            UpdateTramCounts();

            SimulationUpdated?.Invoke();
        }

        partial void OnSelectedRouteTramCountChanged(int value)
        {
            if (_isLoadingRouteSettings || SelectedRouteForSettings == null)
                return;

            SelectedRouteForSettings.TramCount = value;
            ApplySettings();
        }

        partial void OnSelectedRouteIntervalChanged(int value)
        {
            if (_isLoadingRouteSettings || SelectedRouteForSettings == null)
                return;

            SelectedRouteForSettings.IntervalMinutes = value;
            ApplySettings();
        }

        partial void OnSelectedRouteTravelTimeChanged(double value)
        {
            if (_isLoadingRouteSettings || SelectedRouteForSettings == null)
                return;

            SelectedRouteForSettings.TravelTimeMinutes = value;
            ApplySettings();
        }

        partial void OnSelectedRouteStopDurationChanged(int value)
        {
            if (_isLoadingRouteSettings || SelectedRouteForSettings == null)
                return;

            SelectedRouteForSettings.StopDurationSeconds = value;
            ApplySettings();
        }

        partial void OnSelectedRouteCirclesBeforeLunchChanged(int value)
        {
            if (_isLoadingRouteSettings || SelectedRouteForSettings == null)
                return;

            SelectedRouteForSettings.CirclesBeforeLunch = value;
            ApplySettings();
        }

        partial void OnSelectedRouteLunchTimeChanged(int value)
        {
            if (_isLoadingRouteSettings || SelectedRouteForSettings == null)
                return;

            SelectedRouteForSettings.LunchMinutes = value;
            ApplySettings();
        }

        partial void OnSelectedRouteCirclesAfterLunchChanged(int value)
        {
            if (_isLoadingRouteSettings || SelectedRouteForSettings == null)
                return;

            SelectedRouteForSettings.CirclesAfterLunch = value;
            ApplySettings();
        }

        partial void OnSelectedRouteShiftChangeChanged(int value)
        {
            if (_isLoadingRouteSettings || SelectedRouteForSettings == null)
                return;

            SelectedRouteForSettings.ShiftChangeMinutes = value;
            ApplySettings();
        }

        // ---------- УПРАВЛЕНИЕ СИМУЛЯЦИЕЙ ----------

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var sw = Stopwatch.StartNew();

            var simulationWatch = Stopwatch.StartNew();

            // 100 мс реального времени = 100 мс виртуального времени.
            Simulation.Advance(TimeSpan.FromMilliseconds(1));

            simulationWatch.Stop();

            // Статистику обновляем раз в 500 мс.
            _statisticsTickCounter++;

            if (_statisticsTickCounter >= 5)
            {
                _statisticsTickCounter = 0;

                var statisticsWatch = Stopwatch.StartNew();

                UpdateTramCounts();

                statisticsWatch.Stop();

                Debug.WriteLine(
                    $"[STATISTICS] {statisticsWatch.Elapsed.TotalMilliseconds:F2} ms");
            }

            OnPropertyChanged(nameof(CurrentTimeText));

            // Маркеры и остальные элементы мониторинга обновляются каждый тик.
            SimulationUpdated?.Invoke();

            sw.Stop();

            // Не засоряем отладочный вывод сообщениями на каждом тике.
            if (sw.ElapsedMilliseconds >= 50)
            {
                Debug.WriteLine(
                    $"[SLOW TICK] " +
                    $"Simulation: {simulationWatch.Elapsed.TotalMilliseconds:F2} ms | " +
                    $"Total: {sw.Elapsed.TotalMilliseconds:F2} ms");
            }
        }

        [RelayCommand]
        private void StartSimulation()
        {
            if (IsRunning)
                return;

            _timer.Start();
            IsRunning = true;
        }

        [RelayCommand]
        private void PauseSimulation()
        {
            _timer.Stop();
            IsRunning = false;

            Simulation.ExportLog(
                System.IO.Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.Desktop),
                    "TramMonitor_Log.csv"));
        }

        [RelayCommand]
        private void RestartSimulation()
        {
            _timer.Stop();
            IsRunning = false;

            // Полностью сбрасываем модель и журнал.
            Simulation.Reset(RouteDataService);

            SelectedTimeText = "00:00:00";

            OnPropertyChanged(nameof(CurrentTimeText));
            UpdateTramCounts();

            SimulationUpdated?.Invoke();
        }

        [RelayCommand]
        private void SeekTime()
        {
            if (!TimeSpan.TryParseExact(
                    SelectedTimeText,
                    new[]
                    {
                        @"h\:mm",
                        @"hh\:mm",
                        @"h\:mm\:ss",
                        @"hh\:mm\:ss"
                    },
                    CultureInfo.InvariantCulture,
                    out TimeSpan selectedTime))
            {
                return;
            }

            if (selectedTime < TimeSpan.Zero ||
                selectedTime >= TimeSpan.FromDays(1))
            {
                return;
            }

            _timer.Stop();
            IsRunning = false;

            Simulation.SetTimeOfDay(
                RouteDataService,
                selectedTime);

            SelectedTimeText = selectedTime.ToString(@"hh\:mm\:ss");

            OnPropertyChanged(nameof(CurrentTimeText));
            UpdateTramCounts();

            SimulationUpdated?.Invoke();
        }
    }
}
