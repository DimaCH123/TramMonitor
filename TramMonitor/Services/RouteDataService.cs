using System.Collections.ObjectModel;
using System.Collections.Generic;
using TramMonitor.Models;

namespace TramMonitor.Services
{
    public class RouteDataService
    {
        public ObservableCollection<RouteInfo> Routes { get; }

        public RouteDataService()
        {
            Routes = new ObservableCollection<RouteInfo>
            {
                // Маршрут № 2
                new RouteInfo
                {
                    RouteNumber = "2",
                    TramCount = 0,
                    IntervalMinutes = 12,
                    MovementType = RouteMovementType.OneWay,
                    IsEnabledInSimulation = false,
                    NextRouteNumber = "4",
                    StartTime = new TimeSpan(5, 26, 0),
                    EndTime = new TimeSpan(6, 22, 0),
                    Stops = new List<StopInfo>
                    {
                        new StopInfo { Order = 1, Name = "Олимпийская" },
                        new StopInfo { Order = 2, Name = "23-й микрорайон" },
                        new StopInfo { Order = 3, Name = "Юбилейная" },
                        new StopInfo { Order = 4, Name = "Кинотеатр Победа" },
                        new StopInfo { Order = 5, Name = "Архангельская" },
                        new StopInfo { Order = 6, Name = "Фабрика Красный ткач" },
                        new StopInfo { Order = 7, Name = "Первомайская" },
                        new StopInfo { Order = 8, Name = "Набережная" },
                        new StopInfo { Order = 9, Name = "Красноармейская площадь" },
                        new StopInfo { Order = 10, Name = "Военный ордена Жукова университет радиоэлектроники" },
                        new StopInfo { Order = 11, Name = "Вокзал" },
                        new StopInfo { Order = 12, Name = "Заря Свободы" },
                        new StopInfo { Order = 13, Name = "Верещагина" },
                        new StopInfo { Order = 14, Name = "Сталеваров" },
                        new StopInfo { Order = 15, Name = "Университет" },
                        new StopInfo { Order = 16, Name = "Комарова" },
                        new StopInfo { Order = 17, Name = "Гагарина" },
                        new StopInfo { Order = 18, Name = "Центральное заводоуправление" },
                        new StopInfo { Order = 19, Name = "Заводская" },
                        new StopInfo { Order = 20, Name = "Доменная" },
                        new StopInfo { Order = 21, Name = "Водородная" },
                        new StopInfo { Order = 22, Name = "Кислородная" },
                        new StopInfo { Order = 23, Name = "Аглофабрика № 3" }
                    }
                },

                // Маршрут № 4
                new RouteInfo
                {
                    RouteNumber = "4",
                    TramCount = 16,
                    IntervalMinutes = 8,
                    MovementType = RouteMovementType.OutAndBack,
                    IsEnabledInSimulation = true,

                    StartTime = new TimeSpan(4, 49, 0),

                    // 2 круга до обеда, 3 после.
                    CirclesBeforeLunch = 2,
                    CirclesAfterLunch = 3,

                    TravelTimeMinutes = 2,
                    StopDurationSeconds = 20,

                    LunchMinutes = 20,
                    ShiftChangeMinutes = 15,
                    Stops = new List<StopInfo>
                    {
                        new StopInfo { Order = 1, Name = "Олимпийская" },
                        new StopInfo { Order = 2, Name = "23-й микрорайон" },
                        new StopInfo { Order = 3, Name = "Юбилейная" },
                        new StopInfo { Order = 4, Name = "Кинотеатр Победа" },
                        new StopInfo { Order = 5, Name = "Архангельская" },
                        new StopInfo { Order = 6, Name = "Фабрика Красный ткач" },
                        new StopInfo { Order = 7, Name = "Первомайская" },
                        new StopInfo { Order = 8, Name = "Набережная" },
                        new StopInfo { Order = 9, Name = "Красноармейская площадь" },
                        new StopInfo { Order = 10, Name = "Максима Горького" },
                        new StopInfo { Order = 11, Name = "Верещагина" },
                        new StopInfo { Order = 12, Name = "Сталеваров" },
                        new StopInfo { Order = 13, Name = "Университет" },
                        new StopInfo { Order = 14, Name = "Комарова" },
                        new StopInfo { Order = 15, Name = "Гагарина" },
                        new StopInfo { Order = 16, Name = "Центральное заводоуправление" },
                        new StopInfo { Order = 17, Name = "Заводская" },
                        new StopInfo { Order = 18, Name = "Доменная" },
                        new StopInfo { Order = 19, Name = "Водородная" },
                        new StopInfo { Order = 20, Name = "Кислородная" },
                        new StopInfo { Order = 21, Name = "Аглофабрика № 3" }
                    }
                },

                // Маршрут № 4а
                new RouteInfo
                {
                    RouteNumber = "4а",
                    TramCount = 0,
                    IntervalMinutes = 12,
                    MovementType = RouteMovementType.Circular,
                    IsEnabledInSimulation = false,
                    StartTime = new TimeSpan(5, 12, 0),
                    EndTime = new TimeSpan(6, 22, 0),
                    Stops = new List<StopInfo>
                    {
                        new StopInfo { Order = 1, Name = "Олимпийская" },
                        new StopInfo { Order = 2, Name = "23-й микрорайон" },
                        new StopInfo { Order = 3, Name = "Юбилейная" },
                        new StopInfo { Order = 4, Name = "Кинотеатр Победа" },
                        new StopInfo { Order = 5, Name = "Архангельская" },
                        new StopInfo { Order = 6, Name = "Фабрика Красный ткач" },
                        new StopInfo { Order = 7, Name = "Первомайская" },
                        new StopInfo { Order = 8, Name = "Набережная" },
                        new StopInfo { Order = 9, Name = "Красноармейская площадь" },
                        new StopInfo { Order = 10, Name = "Максима Горького" },
                        new StopInfo { Order = 11, Name = "Верещагина" },
                        new StopInfo { Order = 12, Name = "Сталеваров" },
                        new StopInfo { Order = 13, Name = "Университет" },
                        new StopInfo { Order = 14, Name = "Комарова" },
                        new StopInfo { Order = 15, Name = "Гагарина" },
                        new StopInfo { Order = 16, Name = "Центральное заводоуправление" },
                        new StopInfo { Order = 17, Name = "Заводская" },
                        new StopInfo { Order = 18, Name = "Доменная" },

                        new StopInfo { Order = 19, Name = "Заводская" },
                        new StopInfo { Order = 20, Name = "Центральное заводоуправление" },
                        new StopInfo { Order = 21, Name = "Гагарина" },
                        new StopInfo { Order = 22, Name = "Комарова" },
                        new StopInfo { Order = 23, Name = "Университет" },
                        new StopInfo { Order = 24, Name = "Сталеваров" },
                        new StopInfo { Order = 25, Name = "Верещагина" },
                        new StopInfo { Order = 26, Name = "Максима Горького" },
                        new StopInfo { Order = 27, Name = "Красноармейская площадь" },
                        new StopInfo { Order = 28, Name = "Набережная" },
                        new StopInfo { Order = 29, Name = "Первомайская" },
                        new StopInfo { Order = 30, Name = "Фабрика Красный ткач" },
                        new StopInfo { Order = 31, Name = "Архангельская" },
                        new StopInfo { Order = 32, Name = "Кинотеатр Победа" },
                        new StopInfo { Order = 33, Name = "Юбилейная" },
                        new StopInfo { Order = 34, Name = "23-й микрорайон" },
                        new StopInfo { Order = 35, Name = "Олимпийская" }
                    }
                },

                // Маршрут № 8
                new RouteInfo
                {
                    RouteNumber = "8",
                    TramCount = 8,
                    IntervalMinutes = 18,
                    MovementType = RouteMovementType.Circular,
                    IsEnabledInSimulation = true,

                    StartTime = new TimeSpan(4, 47, 0),

                    // 4 круга до обеда, 5 после.
                    CirclesBeforeLunch = 4,
                    CirclesAfterLunch = 5,

                    TravelTimeMinutes = 1.9898989899,
                    StopDurationSeconds = 20,

                    LunchMinutes = 20,
                    ShiftChangeMinutes = 15,
                    Stops = new List<StopInfo>
                    {
                        new StopInfo { Order = 1, Name = "Олимпийская" },
                        new StopInfo { Order = 2, Name = "23-й микрорайон" },
                        new StopInfo { Order = 3, Name = "Юбилейная" },
                        new StopInfo { Order = 4, Name = "Кинотеатр Победа" },
                        new StopInfo { Order = 5, Name = "Архангельская" },
                        new StopInfo { Order = 6, Name = "Фабрика Красный ткач" },
                        new StopInfo { Order = 7, Name = "Первомайская" },
                        new StopInfo { Order = 8, Name = "Набережная" },
                        new StopInfo { Order = 9, Name = "Красноармейская площадь" },
                        new StopInfo { Order = 10, Name = "Военный ордена Жукова университет радиоэлектроники" },
                        new StopInfo { Order = 11, Name = "Вокзал" },
                        new StopInfo { Order = 12, Name = "Заря Свободы" },
                        new StopInfo { Order = 13, Name = "Верещагина" },
                        new StopInfo { Order = 14, Name = "Максима Горького" },
                        new StopInfo { Order = 15, Name = "Красноармейская площадь" },
                        new StopInfo { Order = 16, Name = "Набережная" },
                        new StopInfo { Order = 17, Name = "Первомайская" },
                        new StopInfo { Order = 18, Name = "Фабрика Красный ткач" },
                        new StopInfo { Order = 19, Name = "Архангельская" },
                        new StopInfo { Order = 20, Name = "Кинотеатр Победа" },
                        new StopInfo { Order = 21, Name = "Юбилейная" },
                        new StopInfo { Order = 22, Name = "23-й микрорайон" },
                        new StopInfo { Order = 23, Name = "Олимпийская" }
                    }
                }
            };
        }
    }
}