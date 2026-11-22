using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TramMonitor.Models;
using TramMonitor.Services;

namespace TramMonitor.ViewModels
{
    public partial class RoutesViewModel : ObservableObject
    {
        public ObservableCollection<RouteInfo> Routes { get; }

        [ObservableProperty]
        private RouteInfo? _selectedRoute;

        public RoutesViewModel(RouteDataService routeDataService)
        {
            Routes = routeDataService.Routes;
            SelectedRoute = Routes[0];
        }
    }
}