
using System;
using System.Collections.Generic;
using TramMonitor.Models;

namespace TramMonitor.Services
{
    public class TrackLocation
    {
        public string TrackId { get; init; } = string.Empty;
        public double Position { get; init; }
        public double X { get; init; }
        public double Y { get; init; }
    }

    public class TrackGeometryService
    {
        public const double UnitsPerMeter = 0.2;
        public const double SafeDistanceMeters = 30;
        public const double SafeDistance = SafeDistanceMeters * UnitsPerMeter;

        private readonly Dictionary<string, List<TrackLocation>> _routes = new();

        public TrackGeometryService()
        {
            BuildRoute4();
            BuildRoute4a();
            BuildRoute8();
            BuildRoute2();
        }

        public TrackLocation GetStopLocation(string routeNumber, int stopIndex)
        {
            if (!_routes.TryGetValue(routeNumber, out var route))
                throw new ArgumentException(
                    $"Геометрия маршрута {routeNumber} не задана.");

            if (stopIndex < 0 || stopIndex >= route.Count)
                throw new ArgumentOutOfRangeException(nameof(stopIndex));

            return route[stopIndex];
        }

        public IReadOnlyList<TrackLocation> GetRouteGeometry(string routeNumber)
        {
            if (!_routes.TryGetValue(routeNumber, out var route))
                throw new ArgumentException(
                    $"Геометрия маршрута {routeNumber} не задана.");

            return route;
        }

        private static TrackLocation MainLocation(
            string trackId, double y, bool towardPlant)
        {
            return new TrackLocation
            {
                TrackId = trackId,
                Position = towardPlant ? 2240 - y : y - 80,
                X = 500,
                Y = y
            };
        }

        private void BuildRoute4()
        {
            double[] stopY =
            {
                2240, 2166, 2040, 1960, 1880,
                1760, 1680, 1460, 1360, 1240,
                1180, 1100, 1020, 900, 840,
                680, 580, 500, 380, 280, 80
            };

            var geometry = new List<TrackLocation>();

            foreach (double y in stopY)
                geometry.Add(MainLocation("main-to-plant", y, true));

            // Обратное направление. Повтор конечной остановки сохраняется.
            for (int i = stopY.Length - 1; i >= 0; i--)
                geometry.Add(MainLocation("main-to-olympic", stopY[i], false));

            _routes["4"] = geometry;
        }

        private void BuildRoute4a()
        {
            double[] stopY =
            {
        2240, 2166, 2040, 1960, 1880,
        1760, 1680, 1460, 1360, 1240,
        1180, 1100, 1020, 900, 840,
        680, 580, 500
    };

            var geometry = new List<TrackLocation>();

            // Олимпийская -> Доменная.
            foreach (double y in stopY)
                geometry.Add(MainLocation("main-to-plant", y, true));

            // Доменная -> Олимпийская.
            // Обратный путь начинается с Заводской.
            for (int i = stopY.Length - 2; i >= 0; i--)
                geometry.Add(MainLocation("main-to-olympic", stopY[i], false));

            _routes["4а"] = geometry;
        }

        private void BuildRoute8()
        {
            var geometry = new List<TrackLocation>();

            double[] outboundY =
            {
                2240, 2166, 2040, 1960, 1880,
                1760, 1680, 1460, 1360
            };

            foreach (double y in outboundY)
                geometry.Add(MainLocation("main-to-plant", y, true));

            AddBranchPoints(geometry, new[]
            {
                (640d, 1360d),
                (640d, 1240d),
                (620d, 1180d),
                (512d, 1180d),
                (488d, 1240d)
            });

            double[] inboundY =
            {
                1360, 1460, 1680, 1760, 1880,
                1960, 2040, 2166, 2240
            };

            foreach (double y in inboundY)
                geometry.Add(MainLocation("main-to-olympic", y, false));

            _routes["8"] = geometry;
        }

        private void BuildRoute2()
        {
            var geometry = new List<TrackLocation>();

            double[] outboundY =
            {
                2240, 2166, 2040, 1960, 1880,
                1760, 1680, 1460, 1360
            };

            foreach (double y in outboundY)
                geometry.Add(MainLocation("main-to-plant", y, true));

            AddBranchPoints(geometry, new[]
            {
                (640d, 1360d),
                (640d, 1240d),
                (620d, 1180d),
                (500d, 1180d)
            });

            double[] remainingY =
            {
                1100, 1020, 900, 840, 680,
                580, 500, 380, 280, 80
            };

            foreach (double y in remainingY)
                geometry.Add(MainLocation("main-to-plant", y, true));

            _routes["2"] = geometry;
        }
        public TrackLocation DepotLocation { get; } = new TrackLocation
        {
            TrackId = "depot",
            Position = 0,
            X = 500,
            Y = 2280
        };
        private static void AddBranchPoints(
            List<TrackLocation> geometry,
            (double X, double Y)[] points)
        {
            double position = 0;
            double previousX = 500;
            double previousY = 1360;

            foreach (var point in points)
            {
                double dx = point.X - previousX;
                double dy = point.Y - previousY;

                position += Math.Sqrt(dx * dx + dy * dy);

                geometry.Add(new TrackLocation
                {
                    TrackId = "station-branch",
                    Position = position,
                    X = point.X,
                    Y = point.Y
                });

                previousX = point.X;
                previousY = point.Y;
            }
        }
    }
}
