using PrintMate.Terminal.ConfigurationSystem.Core;
using PrintMate.Terminal.Services;
using PrintMate.Terminal.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PrintMate.Terminal.AppConfiguration
{
    public class FavoritMonitoringSettings : ConfigurationModelBase
    {
        public MonitoringGroup Favorites = new MonitoringGroup(MonitoringManager.Saved,
            "Избранное",
            "/images/gauges_favorites_64.png"
        );
    }
}
