using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using TramMonitor.Models;
using TramMonitor.Services;

namespace TramMonitor.ViewModels
{
    public partial class OverviewViewModel : ObservableObject
    {
        public ObservableCollection<RouteInfo> Routes { get; }
        public int RoutesCount => Routes.Count; 

        public int TotalTrams => Routes.Sum(route => route.TramCount);
        public OverviewViewModel(RouteDataService routeDataService)
        {
            Routes = routeDataService.Routes;
        }
    }
}
