using HandyControl.Tools.Extension;
using PrintMate.Terminal.Events;
using PrintMate.Terminal.ViewModels;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc2Lib;
using PrintMate.Terminal.AppConfiguration;

namespace PrintMate.Terminal.Services
{
    public class MonitoringManager
    {
        public const string Saved = "saved";
        public const string GasAndFilters = "gas_and_filters";
        public const string Laser = "laser";
        public const string Ports = "ports";
        public const string Powder = "powder";
        public const string WorkCamera = "work_camera";

        private Dictionary<string, MonitoringGroup> _groups { get; set; } = new Dictionary<string, MonitoringGroup>();

        private readonly IEventAggregator _eventAggregator;

        public MonitoringManager(IEventAggregator eventAggregator)
        {
            _eventAggregator = eventAggregator;
            InitGroups();
        }

        private void InitGroups()
        {
            // Избранное
            _groups.Add(Saved, new MonitoringGroup(Saved,
                "Избранное",
                "/images/gauges_favorites_64.png"
            ));

            _groups[Saved] = Bootstrapper.Configuration.Get<FavoritMonitoringSettings>().Favorites;


            // Газ и фильтры
            _groups.Add(GasAndFilters, new MonitoringGroup(GasAndFilters,
                "Газ и фильтры",
                "/images/gas_filters_system_64.png"
            ));
            _groups[GasAndFilters].Commands.Add(OpcCommands.AM_GasFilter_FilterOxygen);
            _groups[GasAndFilters].Commands.Add(OpcCommands.AM_GasFilter_PressureFilter);
            _groups[GasAndFilters].Commands.Add(OpcCommands.AM_GasFilter_GasFlow);
            _groups[GasAndFilters].Commands.Add(OpcCommands.AM_GasFilter_InertInletPressure);
            _groups[GasAndFilters].Commands.Add(OpcCommands.AM_GasFilter_InertConsumption);
            _groups[GasAndFilters].Commands.Add(OpcCommands.AM_GasFilter_InertCurrentConsumption);
            _groups[GasAndFilters].Commands.Add(OpcCommands.AM_GasFilter_BlowerTemperature);
            _groups[GasAndFilters].Commands.Add(OpcCommands.AM_GasFilter_VenturiTemperature);
            _groups[GasAndFilters].Commands.Add(OpcCommands.AM_GasFilter_Module_DischargeTankPressure);
            _groups[GasAndFilters].Commands.Add(OpcCommands.AM_GasFilter_Module_Pressure);
            _groups[GasAndFilters].Commands.Add(OpcCommands.AM_GasFilter_Module_RH);
            _groups[GasAndFilters].Commands.Add(OpcCommands.AM_GasFilter_Module_Temperature);
            _groups[GasAndFilters].Commands.Add(OpcCommands.AM_GasFilter_Module_Oxygen);
            _groups[GasAndFilters].Commands.Add(OpcCommands.DM_GasFilter_FilterPresentLS);
            _groups[GasAndFilters].Commands.Add(OpcCommands.DM_GasFilter_VToCameraOpenedLS);
            _groups[GasAndFilters].Commands.Add(OpcCommands.DM_GasFilter_VToCameraClosedLS);
            _groups[GasAndFilters].Commands.Add(OpcCommands.DM_GasFilter_VFromCameraOpenedLS);
            _groups[GasAndFilters].Commands.Add(OpcCommands.DM_GasFilter_VFromCameraClosedLS);
            _groups[GasAndFilters].Commands.Add(OpcCommands.DM_GasFilter_Module_ChillerOK);
            _groups[GasAndFilters].Commands.Add(OpcCommands.DM_GasFilter_Module_LidClosedLS);
            _groups[GasAndFilters].Commands.Add(OpcCommands.DM_GasFilter_Module_DumpBucketPresentLS);
            _groups[GasAndFilters].Commands.Add(OpcCommands.DM_GasFilter_Module_DumpBucketLidClosedLS);
            _groups[GasAndFilters].Commands.Add(OpcCommands.DM_GasFilter_AirInputPressure);

            // Лазер
            _groups.Add(Laser, new MonitoringGroup(Laser,
                "Лазер",
                "/images/laser_64.png"
            ));
            _groups[Laser].Commands.Add(OpcCommands.DM_Laser1_ChillerOK);
            _groups[Laser].Commands.Add(OpcCommands.DM_Laser1_PowerStateLaser);
            _groups[Laser].Commands.Add(OpcCommands.DM_Laser1_EmissionStateLaser);
            _groups[Laser].Commands.Add(OpcCommands.DM_Laser2_ChillerOK);
            _groups[Laser].Commands.Add(OpcCommands.DM_Laser2_PowerStateLaser);
            _groups[Laser].Commands.Add(OpcCommands.DM_Laser2_EmissionStateLaser);

            // Привода
            _groups.Add(Ports, new MonitoringGroup(Ports,
                "Привода",
                "/images/axes_64.png"
            ));
            _groups[Ports].Commands.Add(OpcCommands.DM_Axes_RecoaterLeftLS);
            _groups[Ports].Commands.Add(OpcCommands.DM_Axes_RecoaterRightLS);
            _groups[Ports].Commands.Add(OpcCommands.DM_Axes_PlatformTopLS);
            _groups[Ports].Commands.Add(OpcCommands.DM_Axes_PlatformBottomLS);

            // Порошок
            _groups.Add(Powder, new MonitoringGroup(Powder,
                "Порошок",
                "/images/powder_64.png"
            ));
            _groups[Powder].Commands.Add(OpcCommands.DM_Powder_LSRight);
            _groups[Powder].Commands.Add(OpcCommands.DM_Powder_LSLeft);
            _groups[Powder].Commands.Add(OpcCommands.DM_Powder_PowderDispose_DoorLeftLS);
            _groups[Powder].Commands.Add(OpcCommands.DM_Powder_PowderDispose_DoorRightLS);
            _groups[Powder].Commands.Add(OpcCommands.DM_Powder_PowderDispose_DoserLeftLS);
            _groups[Powder].Commands.Add(OpcCommands.DM_Powder_PowderDispose_DoserRightLS);
        }

        public void AddCommandToFavourites(CommandInfo command)
        {
            _groups[Saved].Commands.AddIfNotExists(command);
            _eventAggregator.GetEvent<OnCommandAddToFavouritesEvent>().Publish(command);
            Bootstrapper.Configuration.Get<FavoritMonitoringSettings>().Favorites = _groups[Saved];
            Bootstrapper.Configuration.SaveNow();
        }
        public void RemoveCommandFromFavourites(CommandInfo command)
        {
            _groups[Saved].Commands.Remove(command);
            _eventAggregator.GetEvent<OnCommandRemoveFromFavouritesEvent>().Publish(command);

            Bootstrapper.Configuration.Get<FavoritMonitoringSettings>().Favorites = _groups[Saved];
            Bootstrapper.Configuration.SaveNow();
        }

        public bool IsCommandInFavourites(CommandInfo command) =>
            _groups[Saved].Commands.FirstOrDefault(p => p == command) != null;

        public List<MonitoringGroup> GetGroupsList() => _groups.Values.ToList();
        public Dictionary<string, MonitoringGroup> GetGroups() => _groups;

    }

}
