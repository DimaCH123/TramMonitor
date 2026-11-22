    using System;
    using System.Collections.Generic;
    using System.Text;

    namespace TramMonitor.Models
    {
        public enum RouteMovementType
        {
            OneWay,
            OutAndBack,
            Circular
        }

        public class RouteInfo
        {
            public string RouteNumber { get; set; } = string.Empty;

            public int TramCount { get; set; }

            public int IntervalMinutes { get; set; }

            public RouteMovementType MovementType { get; set; }

            public bool IsEnabledInSimulation { get; set; }

            public string? NextRouteNumber { get; set; }

            public List<StopInfo> Stops { get; set; } = new();

            // Время движения между остановками, в минутах.
            public double TravelTimeMinutes { get; set; } = 2.0;

            public int StopDurationSeconds { get; set; } = 20;

            // Круги за одну смену.
            public int CirclesBeforeLunch { get; set; } = 2;
            public int CirclesAfterLunch { get; set; } = 3;

            public int ShiftChangeMinutes { get; set; } = 15;
            public int LunchMinutes { get; set; } = 20;

            public TimeSpan StartTime { get; set; }
            public TimeSpan EndTime { get; set; }

            // Общее количество кругов за одну смену.
            public int CirclesPerShift =>
                CirclesBeforeLunch + CirclesAfterLunch;

            public List<StopInfo> GetFullRouteStops()
            {
                switch (MovementType)
                {
                    case RouteMovementType.OneWay:
                    case RouteMovementType.Circular:
                        return new List<StopInfo>(Stops);

                    case RouteMovementType.OutAndBack:
                        var fullRoute = new List<StopInfo>(Stops);

                        for (int i = Stops.Count - 1; i >= 0; i--)
                        {
                            fullRoute.Add(Stops[i]);
                        }

                        return fullRoute;

                    default:
                        return new List<StopInfo>(Stops);
                }
            }
        }

    }
