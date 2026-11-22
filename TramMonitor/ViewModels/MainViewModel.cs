
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using TramMonitor.Services;
using TramMonitor.Simulation;

namespace TramMonitor.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        public SimulationEngine Simulation { get; }

        public ObservableCollection<NavigationItem> NavigationItems { get; }

        [ObservableProperty]
        private ObservableObject? _currentViewModel;

        [ObservableProperty]
        private NavigationItem? _selectedNavigationItem;

        public MainViewModel()
        {
            var routeDataService = new RouteDataService();

            Simulation = new SimulationEngine(routeDataService);

            var modelingViewModel =
                new MonitoringViewModel(Simulation, routeDataService);

            var analyticsViewModel =
                new AnalyticsViewModel(Simulation, routeDataService);

            var routesViewModel =
                new RoutesViewModel(routeDataService);

            NavigationItems = new ObservableCollection<NavigationItem>
            {
                new NavigationItem
                {
                    Title = "Моделирование",
                    ViewModel = modelingViewModel
                },

                new NavigationItem
                {
                    Title = "Отслеживание",
                    ViewModel = new TrackingViewModel()
                },

                new NavigationItem
                {
                    Title = "Аналитика",
                    ViewModel = analyticsViewModel
                },

                new NavigationItem
                {
                    Title = "Расписания и маршруты",
                    ViewModel = routesViewModel
                }
            };

            SelectedNavigationItem = NavigationItems[0];
        }

        partial void OnSelectedNavigationItemChanged(
            NavigationItem? value)
        {
            CurrentViewModel = value?.ViewModel;
        }
    }

    public class NavigationItem
    {
        public string Title { get; set; } = string.Empty;

        public ObservableObject ViewModel { get; set; } = null!;
    }
}
