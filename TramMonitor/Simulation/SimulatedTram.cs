using System;
using System.Collections.Generic;
using TramMonitor.Services;
using TramMonitor.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TramMonitor.Simulation
{
    public enum TramStatus
    {
        WaitingForDeparture,
        Moving,
        AtStop,
        OnLunch,       // На обеде
        OnShiftChange, // На пересменке
        Finished       // Смена окончена
    }

    public class SimulatedTram : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string RouteNumber { get; set; } = string.Empty;
        public int NumberOnRoute { get; set; }
        public List<StopInfo> RouteStops { get; set; } = new();
        public bool IsAtDepot { get; set; } = true;
        public TrackLocation DepotLocation { get; set; } = new TrackLocation();
        public int CurrentStopIndex { get; set; }
        public int NextStopIndex { get; set; }
        public TramStatus Status { get; set; }
        public TimeSpan NextEventTime { get; set; }
        public TimeSpan ServiceEndTime { get; set; }
        public List<TrackLocation> TrackGeometry { get; set; } = new();
        public StopInfo CurrentStop => RouteStops[CurrentStopIndex];
        public StopInfo? DestinationStop =>
            Status == TramStatus.Moving ? RouteStops[NextStopIndex] : null;

        // --- СЧЕТЧИКИ И ФЛАГИ ---
        private int _circlesDoneBeforeLunch;
        private int _circlesDoneAfterLunch;
        public bool IsLunchCompleted { get; set; } = false;
        public int CurrentShift { get; set; } = 1; // 1 или 2

        // --- НАСТРОЙКИ МАРШРУТА (передаются при создании) ---
        public int CirclesBeforeLunch { get; set; } = 2;
        public int CirclesAfterLunch { get; set; } = 3;

        public int CirclesPerShift =>
            CirclesBeforeLunch + CirclesAfterLunch;
        public TimeSpan LunchTime { get; set; } = TimeSpan.FromMinutes(20);
        public TimeSpan ShiftChangeTime { get; set; } = TimeSpan.FromMinutes(15);
        public TimeSpan BackToWorkTime { get; set; } // Время возвращения на линию
        public string ShiftText => CurrentShift == 1 ? "1-я" : "2-я";
        public TimeSpan TravelTime { get; set; } = TimeSpan.FromMinutes(2);
        public TimeSpan StopDuration { get; set; } = TimeSpan.FromSeconds(20);

        public string StatusText => Status switch
        {
            TramStatus.WaitingForDeparture => "Ожидание",
            TramStatus.Moving => "В пути",
            TramStatus.AtStop => "На остановке",
            TramStatus.OnLunch => "Обед",
            TramStatus.OnShiftChange => "Пересменка",
            TramStatus.Finished => "Смена окончена",
            _ => "Неизвестно"
        };

        public SimulatedTram(
            int id,
            string routeNumber,
            int numberOnRoute,
            List<StopInfo> routeStops,
            List<TrackLocation> trackGeometry,
            TimeSpan departureTime)
                {
                    Id = id;
                    RouteNumber = routeNumber;
                    NumberOnRoute = numberOnRoute;
                    RouteStops = routeStops;
                    TrackGeometry = trackGeometry;

                    CurrentStopIndex = 0;
                    NextStopIndex = 0;
                    Status = TramStatus.WaitingForDeparture;
                    NextEventTime = departureTime;
                }
        public event PropertyChangedEventHandler? PropertyChanged;

        public int CirclesDoneBeforeLunch
        {
            get => _circlesDoneBeforeLunch;
            set
            {
                if (_circlesDoneBeforeLunch == value)
                    return;

                _circlesDoneBeforeLunch = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CirclesDone));
            }
        }

        public int CirclesDoneAfterLunch
        {
            get => _circlesDoneAfterLunch;
            set
            {
                if (_circlesDoneAfterLunch == value)
                    return;

                _circlesDoneAfterLunch = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CirclesDone));
            }
        }

        public int CirclesDone =>
            CirclesDoneBeforeLunch + CirclesDoneAfterLunch;

        private void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
        public TrackLocation GetCurrentLocation(TimeSpan currentTime)
        {
            if (TrackGeometry.Count == 0)
                throw new InvalidOperationException("Геометрия маршрута не задана.");

            if (IsAtDepot)
                return DepotLocation;

            if (Status != TramStatus.Moving)
            {
                var stop = TrackGeometry[CurrentStopIndex];

                return new TrackLocation
                {
                    TrackId = stop.TrackId,
                    Position = stop.Position,
                    X = stop.X + GetDirectionOffset(stop.TrackId),
                    Y = stop.Y
                };
            }

            var from = TrackGeometry[CurrentStopIndex];
            var to = TrackGeometry[NextStopIndex];

            if (TravelTime <= TimeSpan.Zero)
                return to;

            TimeSpan movementStartTime = NextEventTime - TravelTime;

            double progress =
                (currentTime - movementStartTime).TotalMilliseconds /
                TravelTime.TotalMilliseconds;

            progress = Math.Clamp(progress, 0, 1);

            // Учитываем смещение каждой из двух точек отдельно.
            // Благодаря этому маркер не смещается при переходе
            // между основным путём и ответвлением.
            double fromX = from.X + GetDirectionOffset(from.TrackId);
            double toX = to.X + GetDirectionOffset(to.TrackId);

            return new TrackLocation
            {
                TrackId = progress < 1 ? from.TrackId : to.TrackId,
                Position = from.Position + (to.Position - from.Position) * progress,
                X = fromX + (toX - fromX) * progress,
                Y = from.Y + (to.Y - from.Y) * progress
            };
        }

        private static double GetDirectionOffset(string trackId)
        {
            return trackId switch
            {
                "main-to-plant" => 12,
                "main-to-olympic" => -12,
                _ => 0
            };
        }
    }
}