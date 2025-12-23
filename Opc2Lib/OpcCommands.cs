namespace Opc2Lib
{
    public enum ValueCommandType
    {
        Bool,
        Real,
        Unsigned,
        Dint
    }

    public enum CommandType
    {
        Com,
        Trig,
        AM,
        DISP,
        DM,
        SetPoints,
        Alarms,
        Errors,
        SCP
    }

    public class CommandInfo
    {
        public string Command { get; set; } = string.Empty;
        public string RussianName { get; set; } = string.Empty;
        public string EnglishName { get; set; } = string.Empty;
        public ValueCommandType ValueCommandType { get; set; } = ValueCommandType.Bool;
        public string Address { get; set; } = string.Empty;
        public CommandType GroupId { get; set; }
        public string Title => RussianName;

        public CommandInfo()
        {
        }

        public CommandInfo(string cmd, string rg, string eg, ValueCommandType valueCommand, string address, CommandType groupId)
        {
            Command = cmd;
            RussianName = rg;
            EnglishName = eg;
            ValueCommandType = valueCommand;
            Address = address;
            GroupId = groupId;
        }
    }

    public static class OpcCommands
    {
        // COM section Axes
        public static readonly CommandInfo Com_Axes_Doser = new CommandInfo("Com_Axes_Doser", "Дозатор", "Doser", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Axes_RecoaterJogLeft = new CommandInfo("Com_Axes_RecoaterJogLeft", "Рекоутер вперед", "Recoater forward", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Axes_RecoaterJogRight = new CommandInfo("Com_Axes_RecoaterJogRight", "Рекоутер назад", "Recoater backward", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Axes_PlatformJogUp = new CommandInfo("Com_Axes_PlatformJogUp", "Плафторма вверх", "Platform jog up", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Axes_PlatformStepUp = new CommandInfo("Com_Axes_PlatformStepUp", "Плафторма шаг вверх", "Platform step up", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Axes_PlatformJogDown = new CommandInfo("Com_Axes_PlatformJogDown", "Плафторма вниз", "Platform jog down", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Axes_PlatformStepDown = new CommandInfo("Com_Axes_PlatformStepDown", "Плафторма шаг вниз", "Platform step down", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Axes_Stop = new CommandInfo("Com_Axes_Stop", "Привода стоп", "Axes stop", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Axes_RecoaterToLeftPos = new CommandInfo("Com_Axes_RecoaterToLeftPos", "Рекоутер положение спереди", "Recoater front pos", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Axes_RecoaterToRightPos = new CommandInfo("Com_Axes_RecoaterToRightPos", "Рекоутер положение сзади", "Recoater back pos", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Axes_RecoaterToLoadPos = new CommandInfo("Com_Axes_RecoaterToLoadPos", "Рекоутер положение загрузки", "Recoater load pos", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Axes_DoserRef = new CommandInfo("Com_Axes_DoserRef", "Реферирование дозатора", "Doser homing", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Axes_RecoaterRef = new CommandInfo("Com_Axes_RecoaterRef", "Реферирование рекоутера", "Recoater homing", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Axes_PlatformRef = new CommandInfo("Com_Axes_PlatformRef", "Реферирование платформы", "Platform homing", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Axes_PlatformRefREL = new CommandInfo("Com_Axes_PlatformRefREL", "Установка нуля платформы", "Set platform zero", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Axes_PlatformPosZero = new CommandInfo("Com_Axes_PlatformPosZero", "Перемещение платформы в 0", "Move platform to 0", ValueCommandType.Bool, "4", CommandType.Com);

        // COM section Process Chamber
        public static readonly CommandInfo Com_PChamber_LightPB = new CommandInfo("Com_PChamber_LightPB", "Свет системы контроля", "Light for control system", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_PChamber_Light = new CommandInfo("Com_PChamber_Light", "Свет в камере", "Chamber light", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_PChamber_CameraLock = new CommandInfo("Com_PChamber_CameraLock", "Замок камеры", "Chamber lock", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_PChamber_VExhaustCamera = new CommandInfo("Com_PChamber_VExhaustCamera", "Клапан сброса давления камеры", "Chamber exhaust valve", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_PChamber_VExhaustCameraRegulated = new CommandInfo("Com_PChamber_VExhaustCameraRegulated", "Дроссель сброса давления камеры", "Chamber exhaust orifice", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_PChamber_PlatformHeater = new CommandInfo("Com_PChamber_PlatformHeater", "Нагрев платформы", "Platform heater", ValueCommandType.Bool, "4", CommandType.Com);

        // COM section Powder
        public static readonly CommandInfo Com_Powder_DoserVibro = new CommandInfo("Com_Powder_DoserVibro", "Виброочистка дозатора", "Doser vibration clear", ValueCommandType.Bool, "4", CommandType.Com);

        // COM section Gas Filter
        public static readonly CommandInfo Com_GasFilter_VGasToCamera = new CommandInfo("Com_GasFilter_VGasToCamera", "Клапан впускного коллектора", "Chamber input valve", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_GasFilter_VGasFromCamera = new CommandInfo("Com_GasFilter_VGasFromCamera", "Клапан выпускного коллектора", "Chamber output valve", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_GasFilter_ResetInertCons = new CommandInfo("Com_GasFilter_ResetInertCons", "Сброс расхода инертного газа", "Reset inert gas consumption", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_GasFilter_Blower = new CommandInfo("Com_GasFilter_Blower", "Воздуходувка", "Blower", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_GasFilter_Inert = new CommandInfo("Com_GasFilter_Inert", "Напуск инертного газа", "Inert gas", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_GasFilter_Module_PowerChiller = new CommandInfo("Com_GasFilter_Module_PowerChiller", "Чиллер модуля фильтрации", "Filter module chiller", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_GasFilter_Module_VSupplyDischargeTank = new CommandInfo("Com_GasFilter_Module_VSupplyDischargeTank", "Клапан подачи газа в ресивер очистки фильтров", "Filters discharge tank supply valve", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_GasFilter_Module_VInletInert = new CommandInfo("Com_GasFilter_Module_VInletInert", "Клапан подачи инертного газа в модуль фильтрации", "Filter module inert inlet valve", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_GasFilter_Module_VExhaust = new CommandInfo("Com_GasFilter_Module_VExhaust", "Клапан сброса давления модуля фильтрации", "Filter module discharge valve", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_GasFilter_Module_VDischargeFilter1 = new CommandInfo("Com_GasFilter_Module_VDischargeFilter1", "Клапан очистки фильтра 1", "Filter 1 discharge valve", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_GasFilter_Module_VDischargeFilter2 = new CommandInfo("Com_GasFilter_Module_VDischargeFilter2", "Клапан очистки фильтра 2", "Filter 2 discharge valve", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_GasFilter_Module_VDischargeFilter3 = new CommandInfo("Com_GasFilter_Module_VDischargeFilter3", "Клапан очистки фильтра 3", "Filter 3 discharge valve", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_GasFilter_Module_VDischargeFilter4 = new CommandInfo("Com_GasFilter_Module_VDischargeFilter4", "Клапан очистки фильтра 4", "Filter 4 discharge valve", ValueCommandType.Bool, "4", CommandType.Com);

        // COM section Laser
        public static readonly CommandInfo Com_Laser_Emission = new CommandInfo("Com_Laser_Emission", "Излучение", "Emission", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Laser_Reset = new CommandInfo("Com_Laser_Reset", "Сброс ошибок лазера", "Laser error reset", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Laser_ScannersCooling = new CommandInfo("Com_Laser_ScannersCooling", "Воздушное охлаждение сканаторов", "Scanners air cooling", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Laser1_PowerChiller = new CommandInfo("Com_Laser1_PowerChiller", "Чиллер лазера 1", "Laser 1 chiller", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Laser2_PowerChiller = new CommandInfo("Com_Laser2_PowerChiller", "Чиллер лазера 2", "Laser 2 chiller", ValueCommandType.Bool, "4", CommandType.Com);

        // COM section Common
        public static readonly CommandInfo Com_Layer = new CommandInfo("Com_Layer", "Пуск нанесения слоя", "Undefined", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Recoat = new CommandInfo("Com_Recoat", "Нанести порошок", "Undefined", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_LaserSystem = new CommandInfo("Com_LaserSystem", "Лазерная система", "Laser system", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_GasSystem = new CommandInfo("Com_GasSystem", "Газовая система", "Gas and filters system", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_PenultimateLayer = new CommandInfo("Com_PenultimateLayer", "Начинаем печатать предпоследний слой", "Undefined", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_HeartBit = new CommandInfo("Com_HeartBit", "Сигнал наличия связи с ПЛК", "Undefined", ValueCommandType.Bool, "4", CommandType.Com);

        // TRIG section
        public static readonly CommandInfo Trig_Axes_RecoaterRef = new CommandInfo("Trig_Axes_RecoaterRef", "Рекоутер отреферирован", "Recoater is homed", ValueCommandType.Bool, "4", CommandType.Trig);
        public static readonly CommandInfo Trig_Layer = new CommandInfo("Trig_Layer", "Слой нанесен", "Undefined", ValueCommandType.Bool, "4", CommandType.Trig);
        public static readonly CommandInfo Trig_Recoat = new CommandInfo("Trig_Recoat", "Нанесен порошок", "Undefined", ValueCommandType.Bool, "4", CommandType.Trig);
        public static readonly CommandInfo Trig_AllSystemReady = new CommandInfo("Trig_AllSystemReady", "ПЛК готовность к печати", "PLC ready for print", ValueCommandType.Bool, "4", CommandType.Trig);
        public static readonly CommandInfo Trig_SensorKeyLocked = new CommandInfo("Trig_SensorKeyLocked", "Ключ отключения сенсора", "Undefined", ValueCommandType.Bool, "4", CommandType.Trig);
        public static readonly CommandInfo Trig_PowerSwitch = new CommandInfo("Trig_PowerSwitch", "Рубильник питания", "Power switch", ValueCommandType.Bool, "4", CommandType.Trig);
        public static readonly CommandInfo Trig_RKF = new CommandInfo("Trig_RKF", "Реле контроля фаз", "RKF", ValueCommandType.Bool, "4", CommandType.Trig);

        // AM section Powder
        public static readonly CommandInfo AM_Powder_Level1 = new CommandInfo("AM_Powder_Level1", "Уровень порошка 1", "Powder level 1", ValueCommandType.Real, "4", CommandType.AM);
        public static readonly CommandInfo AM_Powder_Level2 = new CommandInfo("AM_Powder_Level2", "Уровень порошка 2", "Powder level 2", ValueCommandType.Real, "4", CommandType.AM);

        // AM section Process Chamber
        public static readonly CommandInfo AM_PChamber_Pressure = new CommandInfo("AM_PChamber_Pressure", "Давление в камере, кПа", "Chamber pressure, kPa", ValueCommandType.Real, "4", CommandType.AM);
        public static readonly CommandInfo AM_PChamber_Oxygen = new CommandInfo("AM_PChamber_Oxygen", "Кислород в камере, %", "Chamber oxygen, %", ValueCommandType.Real, "4", CommandType.AM);
        public static readonly CommandInfo AM_PChamber_Temperature = new CommandInfo("AM_PChamber_Temperature", "Температура в камере, °C", "Chamber temperature, °C", ValueCommandType.Real, "4", CommandType.AM);
        public static readonly CommandInfo AM_PChamber_HeaterTemperature1 = new CommandInfo("AM_PChamber_HeaterTemperature1", "Температура платформы 1, °C", "Platform temperature 1, °C", ValueCommandType.Real, "4", CommandType.AM);
        public static readonly CommandInfo AM_PChamber_HeaterTemperature2 = new CommandInfo("AM_PChamber_HeaterTemperature2", "Температура платформы 2, °C", "Platform temperature 2, °C", ValueCommandType.Real, "4", CommandType.AM);
        public static readonly CommandInfo AM_PChamber_InertInletPressure = new CommandInfo("AM_PChamber_InertInletPressure", "Давление инертного газа, кПа", "Inert inlet pressure, kPa", ValueCommandType.Real, "4", CommandType.AM);

        // AM section Gas and Filters
        public static readonly CommandInfo AM_GasFilter_FilterOxygen = new CommandInfo("AM_GasFilter_FilterOxygen", "Кислород в фильтре, %", "Oxygen in filter, %", ValueCommandType.Real, "4", CommandType.AM);
        public static readonly CommandInfo AM_GasFilter_PressureFilter = new CommandInfo("AM_GasFilter_PressureFilter", "Давление на фильтре, кПа", "Filter pressure, kPa", ValueCommandType.Real, "4", CommandType.AM);
        public static readonly CommandInfo AM_GasFilter_GasFlow = new CommandInfo("AM_GasFilter_GasFlow", "Расход газовой среды, м³/ч", "Process gas flow, m³/h", ValueCommandType.Real, "4", CommandType.AM);
        public static readonly CommandInfo AM_GasFilter_InertInletPressure = new CommandInfo("AM_GasFilter_InertInletPressure", "Давление инертного газа, bar", "Inert gas pressure, bar", ValueCommandType.Real, "4", CommandType.AM);
        public static readonly CommandInfo AM_GasFilter_InertConsumption = new CommandInfo("AM_GasFilter_InertConsumption", "Расход инертного газа, м³", "Inert gas consumption, m³", ValueCommandType.Real, "4", CommandType.AM);
        public static readonly CommandInfo AM_GasFilter_InertCurrentConsumption = new CommandInfo("AM_GasFilter_InertCurrentConsumption", "Мгновенный расход инертного газа, л/м", "Current inert gas flow, l/m", ValueCommandType.Real, "4", CommandType.AM);
        public static readonly CommandInfo AM_GasFilter_BlowerTemperature = new CommandInfo("AM_GasFilter_BlowerTemperature", "Темепература воздуходувки, °C", "Blower temperature, °C", ValueCommandType.Real, "4", CommandType.AM);
        public static readonly CommandInfo AM_GasFilter_VenturiTemperature = new CommandInfo("AM_GasFilter_VenturiTemperature", "Температура датчика расхода, °C", "Consumption sensor temperature, °C", ValueCommandType.Real, "4", CommandType.AM);
        public static readonly CommandInfo AM_GasFilter_Module_DischargeTankPressure = new CommandInfo("AM_GasFilter_Module_DischargeTankPressure", "Давление в ресивере очистки фильтров, кПа", "Filters discharge tank pressure, kPa", ValueCommandType.Real, "4", CommandType.AM);
        public static readonly CommandInfo AM_GasFilter_Module_Pressure = new CommandInfo("AM_GasFilter_Module_Pressure", "Давление воздуха в модуле фильтрации, кПа", "Air pressure in filters module, kPa", ValueCommandType.Real, "4", CommandType.AM);
        public static readonly CommandInfo AM_GasFilter_Module_RH = new CommandInfo("AM_GasFilter_Module_RH", "Уровень влажности газовой среды, %", "Process gas humidity, %", ValueCommandType.Real, "4", CommandType.AM);
        public static readonly CommandInfo AM_GasFilter_Module_Temperature = new CommandInfo("AM_GasFilter_Module_Temperature", "Температура газовой среды, °C", "Process gas temperature, °C", ValueCommandType.Real, "4", CommandType.AM);
        public static readonly CommandInfo AM_GasFilter_Module_Oxygen = new CommandInfo("AM_GasFilter_Module_Oxygen", "Кислород в модуле фильтрации, %", "Oxygen in filters module, %", ValueCommandType.Real, "4", CommandType.AM);

        // AM section Axes
        public static readonly CommandInfo AM_Axes_RecoaterABSPosition = new CommandInfo("AM_Axes_RecoaterABSPosition", "Текущая позиция рекоутера, мм", "Recoater pos, mm", ValueCommandType.Unsigned, "4", CommandType.AM);
        public static readonly CommandInfo AM_Axes_PlatformRELPosition = new CommandInfo("AM_Axes_PlatformRELPosition", "Положение платформы, мкм", "Platform pos, mkm", ValueCommandType.Dint, "4", CommandType.AM);

        // DISP section
        public static readonly CommandInfo DISP_PositionRecoater = new CommandInfo("DISP_PositionRecoater", "Позиция рекоутра", "Recoater position", ValueCommandType.Unsigned, "4", CommandType.DISP);
        public static readonly CommandInfo DISP_PositionPlatform = new CommandInfo("DISP_PositionPlatform", "Позиция платформы", "Platform position", ValueCommandType.Unsigned, "4", CommandType.DISP);

        // DM section Process Chamber
        public static readonly CommandInfo DM_PChamber_ChamberDoorLS = new CommandInfo("DM_PChamber_ChamberDoorLS", "Дверь закрыта", "Door is closed", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_PChamber_ExhaustValve = new CommandInfo("DM_PChamber_ExhaustValve", "Сбросной клапан", "Exhaust valve", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_PChamber_ExhaustValveRegulated = new CommandInfo("DM_PChamber_ExhaustValveRegulated", "Сбросной дроссель", "Exhaust orifice", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_PChamber_GloveBoxLS = new CommandInfo("DM_PChamber_GloveBoxLS", "Перчаточный порт закрыт", "Glovebox is closed", ValueCommandType.Bool, "4", CommandType.DM);

        // DM section Gas Filters
        public static readonly CommandInfo DM_GasFilter_FilterPresentLS = new CommandInfo("DM_GasFilter_FilterPresentLS", "Фильтр установлен", "Filter is installed", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_GasFilter_VToCameraOpenedLS = new CommandInfo("DM_GasFilter_VToCameraOpenedLS", "Впускная задвижка открыта", "Inlet valve opened", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_GasFilter_VToCameraClosedLS = new CommandInfo("DM_GasFilter_VToCameraClosedLS", "Впускная задвижка закрыта", "Inlet valve closed", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_GasFilter_VFromCameraOpenedLS = new CommandInfo("DM_GasFilter_VFromCameraOpenedLS", "Выпускная задвижка открыта", "Outlet valve opened", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_GasFilter_VFromCameraClosedLS = new CommandInfo("DM_GasFilter_VFromCameraClosedLS", "Выпускная задвижка закрыта", "Outlet valve closed", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_GasFilter_Module_ChillerOK = new CommandInfo("DM_GasFilter_Module_ChillerOK", "Чиллер модуля фильтрации состояние", "Filters module chiller state", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_GasFilter_Module_LidClosedLS = new CommandInfo("DM_GasFilter_Module_LidClosedLS", "Крышка модуля фильтрации закрыта", "Filters module lid is closed", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_GasFilter_Module_DumpBucketPresentLS = new CommandInfo("DM_GasFilter_Module_DumpBucketPresentLS", "Сбросная емкость конденсата установлена", "Dump bucket is installed", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_GasFilter_Module_DumpBucketLidClosedLS = new CommandInfo("DM_GasFilter_Module_DumpBucketLidClosedLS", "Крышка сбросной емкости конденсата установлена", "Dump bucket lid is installed", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_GasFilter_AirInputPressure = new CommandInfo("DM_GasFilter_AirInputPressure", "Давление воздуха модуля фильтрации в норме", "Filters module air pressure state", ValueCommandType.Bool, "4", CommandType.DM);

        // DM section Laser
        public static readonly CommandInfo DM_Laser1_ChillerOK = new CommandInfo("DM_Laser1_ChillerOK", "Чиллер 1 состояние", "Chiller 1 state", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_Laser1_PowerStateLaser = new CommandInfo("DM_Laser1_PowerStateLaser", "Лазер 1 питание", "Laser 1 power", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_Laser1_EmissionStateLaser = new CommandInfo("DM_Laser1_EmissionStateLaser", "Лазер 1 излучение", "Laser 1 emission", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_Laser2_ChillerOK = new CommandInfo("DM_Laser2_ChillerOK", "Чиллер 2 состояние", "Chiller 2 state", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_Laser2_PowerStateLaser = new CommandInfo("DM_Laser2_PowerStateLaser", "Лазер 2 питание", "Laser 2 power", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_Laser2_EmissionStateLaser = new CommandInfo("DM_Laser2_EmissionStateLaser", "Лазер 2 излучение", "Laser 2 emission", ValueCommandType.Bool, "4", CommandType.DM);

        // DM section Axes
        public static readonly CommandInfo DM_Axes_RecoaterLeftLS = new CommandInfo("DM_Axes_RecoaterLeftLS", "Рекоутер концевик спереди", "Recoater limit switch front", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_Axes_RecoaterRightLS = new CommandInfo("DM_Axes_RecoaterRightLS", "Рекоутер концевик сзади", "Recoater limit switch back", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_Axes_PlatformTopLS = new CommandInfo("DM_Axes_PlatformTopLS", "Платформа концевик верх", "Platform limit switch top", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_Axes_PlatformBottomLS = new CommandInfo("DM_Axes_PlatformBottomLS", "Платформа концевик низ", "Platform limit switch bottom", ValueCommandType.Bool, "4", CommandType.DM);

        // DM section Powder
        public static readonly CommandInfo DM_Powder_LSRight = new CommandInfo("DM_Powder_LSRight", "Датчик подачи порошка справа", "Fresh powder sensor right", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_Powder_LSLeft = new CommandInfo("DM_Powder_LSLeft", "Датчик подачи порошка слева", "Fresh powder sensor left", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_Powder_PowderDispose_DoorLeftLS = new CommandInfo("DM_Powder_PowderDispose_DoorLeftLS", "Датчик сброса порошка спереди слева", "Overfill powder near left", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_Powder_PowderDispose_DoorRightLS = new CommandInfo("DM_Powder_PowderDispose_DoorRightLS", "Датчик сброса порошка спереди справа", "Overfill powder near right", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_Powder_PowderDispose_DoserLeftLS = new CommandInfo("DM_Powder_PowderDispose_DoserLeftLS", "Датчик сброса порошка сзади слева", "Overfill powder far left", ValueCommandType.Bool, "4", CommandType.DM);
        public static readonly CommandInfo DM_Powder_PowderDispose_DoserRightLS = new CommandInfo("DM_Powder_PowderDispose_DoserRightLS", "Датчик сброса порошка сзади справа", "Overfill powder far right", ValueCommandType.Bool, "4", CommandType.DM);

        // SET section GasFilter
        public static readonly CommandInfo Set_GasFilter_BlowerSpeed = new CommandInfo("Set_GasFilter_BlowerSpeed", "Обдув скорость, %", "Blower speed, %", ValueCommandType.Unsigned, "4", CommandType.SetPoints);

        // SET section Axes
        public static readonly CommandInfo Set_Axes_PlatformStep = new CommandInfo("Set_Axes_PlatformStep", "Шаг платформы, мкм", "Platform step, mkm", ValueCommandType.Dint, "4", CommandType.SetPoints);
        public static readonly CommandInfo Set_Axes_PlatformSpeed = new CommandInfo("Set_Axes_PlatformSpeed", "Платформа скорость, мм/с", "Platform speed, mm/s", ValueCommandType.Unsigned, "4", CommandType.SetPoints);
        public static readonly CommandInfo Set_Axes_RecoaterSpeed = new CommandInfo("Set_Axes_RecoaterSpeed", "Рекоутер скорость, мм/с", "Recoater speed, mm/s", ValueCommandType.Unsigned, "4", CommandType.SetPoints);
        public static readonly CommandInfo Set_Axes_DoserSpeed = new CommandInfo("Set_Axes_DoserSpeed", "Дозатор скорость, об/мин", "Doser speed, rnd/min", ValueCommandType.Unsigned, "4", CommandType.SetPoints);
        public static readonly CommandInfo Set_Axes_DoserCounts = new CommandInfo("Set_Axes_DoserCounts", "Дозатор кол-во порошка, дозы", "Doser counts, dose", ValueCommandType.Unsigned, "4", CommandType.SetPoints);

        // SET section Process Chamber
        public static readonly CommandInfo Set_PChamber_PlatformHeatingTemperature = new CommandInfo("Set_PChamber_PlatformHeatingTemperature", "Температура нагрева платформы, °C", "Platform heating temperature, °C", ValueCommandType.Real, "4", CommandType.SetPoints);

        // Alarm section PChamber
        public static readonly CommandInfo Alarm_PChamber_Unlocked = new CommandInfo("Alarm_PChamber_Unlocked", "Дверь камеры не закрыта", "Process chamber door unlocked", ValueCommandType.Bool, "4", CommandType.Alarms);
        public static readonly CommandInfo Alarm_PChamber_Pressure = new CommandInfo("Alarm_PChamber_Pressure", "Высокое давление в камере", "High process chamber pressure", ValueCommandType.Bool, "4", CommandType.Alarms);
        public static readonly CommandInfo Alarm_PChamber_HighOxygen = new CommandInfo("Alarm_PChamber_HighOxygen", "Высокое содержание кислорода в камере", "High oxygen in process chamber", ValueCommandType.Bool, "4", CommandType.Alarms);
        public static readonly CommandInfo Alarm_PChamber_Temperature = new CommandInfo("Alarm_PChamber_Temperature", "Высокая температура в камере", "High temperature in process chamber", ValueCommandType.Bool, "4", CommandType.Alarms);
        public static readonly CommandInfo Alarm_PChamber_InertInletPressure = new CommandInfo("Alarm_PChamber_InertInletPressure", "Низкое давление инертного газа в камере", "Low inert gas pressure in process chamber", ValueCommandType.Bool, "4", CommandType.Alarms);

        // Alarm section Powder
        public static readonly CommandInfo Alarm_Powder_Empty = new CommandInfo("Alarm_Powder_Empty", "Низкий уровень порошка в подающем бункере", "Low powder level in powder feeder", ValueCommandType.Bool, "4", CommandType.Alarms);

        // Alarm section GasFilter
        public static readonly CommandInfo Alarm_GasFilter_HighOxygen = new CommandInfo("Alarm_GasFilter_HighOxygen", "Высокое содержание кислорода в фильтре", "High oxygen in filter", ValueCommandType.Bool, "4", CommandType.Alarms);
        public static readonly CommandInfo Alarm_GasFilter_FilterClogg = new CommandInfo("Alarm_GasFilter_FilterClogg", "Фильтр загрезнен", "Filter is clogged", ValueCommandType.Bool, "4", CommandType.Alarms);
        public static readonly CommandInfo Alarm_GasFilter_InertPressure = new CommandInfo("Alarm_GasFilter_InertPressure", "Низкое давление инертного газа", "Inert gas low pressure", ValueCommandType.Bool, "4", CommandType.Alarms);
        public static readonly CommandInfo Alarm_GasFilter_HighBlowerTemp = new CommandInfo("Alarm_GasFilter_HighBlowerTemp", "Высокая темепратура воздуходувки", "High blower temperature", ValueCommandType.Bool, "4", CommandType.Alarms);
        public static readonly CommandInfo Alarm_GasFilter_InertFlowSensor = new CommandInfo("Alarm_GasFilter_InertFlowSensor", "Неисправность датчика потока инертного газа", "Inert gas flow sensor alarm", ValueCommandType.Bool, "4", CommandType.Alarms);
        public static readonly CommandInfo Alarm_GasFilter_Module_FilterDoorLS = new CommandInfo("Alarm_GasFilter_Module_FilterDoorLS", "Открыта дверь модуля фильтрации", "Filters module door is opened", ValueCommandType.Bool, "4", CommandType.Alarms);
        public static readonly CommandInfo Alarm_GasFilter_Module_SupplyGasDoorLeftLS = new CommandInfo("Alarm_GasFilter_Module_SupplyGasDoorLeftLS", "Открыта левая створка двери модуля фильтрации", "Filters module left door is opened", ValueCommandType.Bool, "4", CommandType.Alarms);
        public static readonly CommandInfo Alarm_GasFilter_Module_SupplyGasDoorRightLS = new CommandInfo("Alarm_GasFilter_Module_SupplyGasDoorRightLS", "Открыта правая створка двери модуля фильтрации", "Filters module right door is opened", ValueCommandType.Bool, "4", CommandType.Alarms);
        public static readonly CommandInfo Alarm_GasFilter_Module_SupplyGasHighOxygen = new CommandInfo("Alarm_GasFilter_Module_SupplyGasHighOxygen", "Высокое содержание кислорода в модуле фильтрации", "High oxygen in filters module", ValueCommandType.Bool, "4", CommandType.Alarms);

        // Error section PChamber
        public static readonly CommandInfo Err_PChamber_Pressure = new CommandInfo("Err_PChamber_Pressure", "Высокое давление в камере", "High process chamber pressure", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_PChamber_OxygenSensor = new CommandInfo("Err_PChamber_OxygenSensor", "Неисправность датчика кислорода в камере", "Process chamber oxygen sensor error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_PChamber_PressureSensor = new CommandInfo("Err_PChamber_PressureSensor", "Неисправность датчика давления в камере", "Process chamber pressure sensor error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_PChamber_HighOxygen = new CommandInfo("Err_PChamber_HighOxygen", "Высокое содержание кислорода в камере", "High oxygen in process chamber", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_PChamber_Temperature = new CommandInfo("Err_PChamber_Temperature", "Высокая температура в камере", "High temperature in process chamber", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_PChamber_TemperatureSensor = new CommandInfo("Err_PChamber_TemperatureSensor", "Неисправность датчика температуры в камере", "Process chamber temperature sensor error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_PChamber_PlatformHeatingTemperatureSensor1 = new CommandInfo("Err_PChamber_PlatformHeatingTemperatureSensor1", "Неисправность датчика 1 температуры нагревателя в камере", "Process chamber heater temperature sensor 1 error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_PChamber_PlatformHeatingTemperatureSensor2 = new CommandInfo("Err_PChamber_PlatformHeatingTemperatureSensor2", "Неисправность датчика 2 температуры нагревателя в камере", "Process chamber heater temperature sensor 2 error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_PChamber_InertInletPressure = new CommandInfo("Err_PChamber_InertInletPressure", "Низкое входное давление инертного газа", "Inert gas inlet pressure is low", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_PChamber_InertInletPressureSensor = new CommandInfo("Err_PChamber_InertInletPressureSensor", "Неисправность датчика входного давления инертного газа", "Inert gas inlet pressure sensor error", ValueCommandType.Bool, "4", CommandType.Errors);

        // Error section Axes
        public static readonly CommandInfo Err_Axes_Platform = new CommandInfo("Err_Axes_Platform", "Ошибка привода платформы", "Platform axis error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_Axes_Recoater = new CommandInfo("Err_Axes_Recoater", "Ошибка привода рекоутера", "Recoater axis error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_Axes_Doser = new CommandInfo("Err_Axes_Doser", "Ошибка привода дозатора", "Doser axis error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_Axes_ErrPLCModuleExtension = new CommandInfo("Err_Axes_ErrPLCModuleExtension", "Ошибка модуля ПЛК", "PLC module error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_Axes_RKF = new CommandInfo("Err_Axes_RKF", "Ошибка фазировки", "Input power phase error", ValueCommandType.Bool, "4", CommandType.Errors);

        // Error section Laser
        public static readonly CommandInfo Err_Laser1_Chiller = new CommandInfo("Err_Laser1_Chiller", "Ошибка чиллера 1", "Chiller error 1", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_Laser1 = new CommandInfo("Err_Laser1", "Ошибка лазера 1", "Laser source error 1", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_Laser2_Chiller = new CommandInfo("Err_Laser2_Chiller", "Ошибка чиллера 2", "Chiller error 2", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_Laser2 = new CommandInfo("Err_Laser2", "Ошибка лазера 2", "Laser source error 2", ValueCommandType.Bool, "4", CommandType.Errors);

        // Error
        public static readonly CommandInfo Err_EmergencyStop = new CommandInfo("Err_EmergencyStop", "Нажата кнопка аварийной остановки", "Emergency stop button is pressed", ValueCommandType.Bool, "4", CommandType.Errors);

        // Error section GasFilter
        public static readonly CommandInfo Err_GasFilter_BlowerFC = new CommandInfo("Err_GasFilter_BlowerFC", "Ошибка ПЧ воздуходвки", "Blower FC error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_FilterPresent = new CommandInfo("Err_GasFilter_FilterPresent", "Фильтр не установлен", "Filter is not installed", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_PressureSensor = new CommandInfo("Err_GasFilter_PressureSensor", "Неисправность датчика давления в фильтре", "Filter pressure sensor error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_OxygenSensor = new CommandInfo("Err_GasFilter_OxygenSensor", "Неисправность датчика кислорода в фильтре", "Filter oxygen sensor error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_FlowSensor = new CommandInfo("Err_GasFilter_FlowSensor", "Неисправность датчика скорости потока в фильтре", "Filter flow velocity sensor error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_FilterClogg = new CommandInfo("Err_GasFilter_FilterClogg", "Фильтр загрезнен", "Filter is clogged", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_HighOxygen = new CommandInfo("Err_GasFilter_HighOxygen", "Высокое содержание кислорода в фильтре", "High oxygen in filter", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_InertPressure = new CommandInfo("Err_GasFilter_InertPressure", "Низкое давление инертного газа", "Inert gas low pressure", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_HighBlowerTemp = new CommandInfo("Err_GasFilter_HighBlowerTemp", "Высокая темепратура воздуходувки", "High blower temperature", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_InertPressureSensor = new CommandInfo("Err_GasFilter_InertPressureSensor", "Неисправность датчика давления инертного газа", "Inert gas pressure sensor error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_FilterClosed = new CommandInfo("Err_GasFilter_FilterClosed", "Задвижки фильтра закрыты", "Filter valves are closed", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_VToCameraTimeout = new CommandInfo("Err_GasFilter_VToCameraTimeout", "Превышено время операции впускной задвижки", "Inlet valve operation timeout", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_VFromCameraTimeout = new CommandInfo("Err_GasFilter_VFromCameraTimeout", "Превышено время операции выпускной задвижки", "Outlet valve operation timeout", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_TempBlowerMotorSensor = new CommandInfo("Err_GasFilter_TempBlowerMotorSensor", "Неисправность датчика температуры воздуходвки", "Blower temperature sensor error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_TempVenturiSensor = new CommandInfo("Err_GasFilter_TempVenturiSensor", "Неисправность датчика температуры газовой системы", "Gas system temperature sensor error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_NotSealed = new CommandInfo("Err_GasFilter_NotSealed", "Газовая система не герметична", "Gas system is not sealed", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_InputAirPressure = new CommandInfo("Err_GasFilter_InputAirPressure", "Отсутсвует давление сжатого воздуха", "Compressed air inlet pressure is low", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_Module_NotSealed = new CommandInfo("Err_GasFilter_Module_NotSealed", "Модуль фильтрации не герметичен", "Filters module is not sealed", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_Module_DischargeTankPressureSensor = new CommandInfo("Err_GasFilter_Module_DischargeTankPressureSensor", "Неисправность датчика давления в ресивере очистки фильтров", "Filters discharge tank pressure sensor error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_Module_DumpBucketNotSealed = new CommandInfo("Err_GasFilter_Module_DumpBucketNotSealed", "Сбросная емкость конденсата не герметичена", "Dump bucket is not sealed", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_Module_Chiller = new CommandInfo("Err_GasFilter_Module_Chiller", "Ошибка чилера модуля фильтрации", "Filters module chiller error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_Module_LidClose = new CommandInfo("Err_GasFilter_Module_LidClose", "Не закрыта крышка модуля фильтрации", "Filters module is not sealed", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_Module_SupplyGasPressureSensor = new CommandInfo("Err_GasFilter_Module_SupplyGasPressureSensor", "Ошибка датчика давления в модуле фильтрации", "Filters module pressure sensor error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_Module_SupplyGasRHSensor = new CommandInfo("Err_GasFilter_Module_SupplyGasRHSensor", "Ошибка датчика влажности в модуле фильтрации", "Filters module humidity sensor error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_Module_SupplyGasTemperatureSensor = new CommandInfo("Err_GasFilter_Module_SupplyGasTemperatureSensor", "Ошибка датчика температуры в модуле фильтрации", "Filters module temperature sensor error", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_Module_SupplyGasHighOxygen = new CommandInfo("Err_GasFilter_Module_SupplyGasHighOxygen", "Высокий уровень кислорода в модуле фильтрации", "Hight oxygen level in filters module", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_GasFilter_Module_OxygenSensor = new CommandInfo("Err_GasFilter_Module_OxygenSensor", "Ошибка датчика кислорода в модуле фильтрации", "Filters module oxygen sensor error", ValueCommandType.Bool, "4", CommandType.Errors);

        // Error section Powder
        public static readonly CommandInfo Err_Powder_PowderLevel1Sensor = new CommandInfo("Err_Powder_PowderLevel1Sensor", "Неисправность датчика уровня порошка в подающем бункере 1", "Fresh powder level sensor error 1", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_Powder_PowderLevel2Sensor = new CommandInfo("Err_Powder_PowderLevel2Sensor", "Неисправность датчика уровня порошка в подающем бункере 2", "Fresh powder level sensor error 2", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_Powder_DoserStuck = new CommandInfo("Err_Powder_DoserStuck", "Движение дозатора заблокировано", "Doser movement is blocked", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_Powder_Empty = new CommandInfo("Err_Powder_Empty", "Подающий бункер для порошка пуст", "Powder feeder is empty", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_Powder_PowderDispose_FullDoor = new CommandInfo("Err_Powder_PowderDispose_FullDoor", "Бункер для сброса порошка спереди переполнен", "Overfill near powder disposer is full", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_Powder_PowderDispose_FullDoser = new CommandInfo("Err_Powder_PowderDispose_FullDoser", "Бункер для сброса порошка сзади переполнен", "Overfill far powder disposer is full", ValueCommandType.Bool, "4", CommandType.Errors);
        public static readonly CommandInfo Err_Powder_RecoaterRef = new CommandInfo("Err_Powder_RecoaterRef", "Рекоутер не отреферирован", "Recoater is not homed", ValueCommandType.Bool, "4", CommandType.Errors);

        // SetControlParameter section Axes
        public static readonly CommandInfo SCP_Axes_RecoaterFrontOnExpose = new CommandInfo("SCP_Axes_RecoaterFrontOnExpose", "Односторонее нанесение порошка", "Single side powder recoat", ValueCommandType.Bool, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_Axes_JogSpeedRecoater = new CommandInfo("SCP_Axes_JogSpeedRecoater", "Рекоутер скорость JOG, мм/с", "Recoater JOG speed, mm/s", ValueCommandType.Unsigned, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_Axes_SetRecoaterDistRight = new CommandInfo("SCP_Axes_SetRecoaterDistRight", "Рекоутер правое конечное положение, мм", "Recoater right end pos, mm", ValueCommandType.Unsigned, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_Axes_SetRecoaterDistDoser = new CommandInfo("SCP_Axes_SetRecoaterDistDoser", "Рекоутер положение под дозатором, мм", "Recoater under doser pos, mm", ValueCommandType.Unsigned, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_Axes_PlatformBacklashDOWN = new CommandInfo("SCP_Axes_PlatformBacklashDOWN", "Компенсация зазора ШВП вниз, мкм", "Platform backlash compensation down, mkm", ValueCommandType.Unsigned, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_Axes_PlatformCorrectionDOWN = new CommandInfo("SCP_Axes_PlatformCorrectionDOWN", "Компенсация погрешности ШВП вниз, мкм", "Platform error compensation down, mkm", ValueCommandType.Unsigned, "4", CommandType.SCP);

        // SetControlParameter section Powder
        public static readonly CommandInfo SCP_Powder_SetPowderLevelAlarm = new CommandInfo("SCP_Powder_SetPowderLevelAlarm", "Предупреждение низкий уровень порошка, %", "Powder reserve, %", ValueCommandType.Unsigned, "4", CommandType.SCP);

        // SetControlParameter section GasFilter
        public static readonly CommandInfo SCP_GasFilter_AlarmTempBlower = new CommandInfo("SCP_GasFilter_AlarmTempBlower", "Предупреждение температуры воздуходувки, °C", "Alarm high blower temperature, °C", ValueCommandType.Unsigned, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_GasFilter_ErrTempBlower = new CommandInfo("SCP_GasFilter_ErrTempBlower", "Ошибка температуры воздуходувки, °C", "Error high blower temperature, °C", ValueCommandType.Unsigned, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_GasFilter_BlowerMaxFlow = new CommandInfo("SCP_GasFilter_BlowerMaxFlow", "Максимальная скорость потока воздуходувки, м/с", "Maximum blower flow velocity, m/s", ValueCommandType.Unsigned, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_GasFilter_AlarmGasFlow = new CommandInfo("SCP_GasFilter_AlarmGasFlow", "Предупреждение заполнен фильтр, м/с", "Warning dirty filter, m/s", ValueCommandType.Unsigned, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_GasFilter_ErrGasFlow = new CommandInfo("SCP_GasFilter_ErrGasFlow", "Ошибка заполнен фильтр, м/с", "Error dirty filter, m/s", ValueCommandType.Unsigned, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_GasFilter_SetPointO2 = new CommandInfo("SCP_GasFilter_SetPointO2", "Целевой уровень кислорода, %", "Target oxygen level, %", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_GasFilter_StartPointO2 = new CommandInfo("SCP_GasFilter_StartPointO2", "Стартовый уровень кислорода, %", "Start oxygen level, %", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_GasFilter_InertFillChamber = new CommandInfo("SCP_GasFilter_InertFillChamber", "Значение кислорода при продувке камеры, %", "Process chamber inert blow oxygen, %", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_GasFilter_AlarmInertPressure = new CommandInfo("SCP_GasFilter_AlarmInertPressure", "Предупреждение низкое давление инертного газа, bar", "Alarm inert gas input pressure, bar", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_GasFilter_ErrInertPressure = new CommandInfo("SCP_GasFilter_ErrInertPressure", "Ошибка низкое давление инертного газа, bar", "Error inert gas input pressure, bar", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_GasFilter_AlarmO2 = new CommandInfo("SCP_GasFilter_AlarmO2", "Предупреждение выскоий кислород, %", "Warning high oxygen, %", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_GasFilter_ErrO2 = new CommandInfo("SCP_GasFilter_ErrO2", "Ошибка выскоий кислород, %", "Error high oxygen, %", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_GasFilter_ErrFilterClosed = new CommandInfo("SCP_GasFilter_ErrFilterClosed", "Ошибка фильтр закрыт, кПа", "Error filter is closed, kPa", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_GasFilter_ErrPresNotSealed = new CommandInfo("SCP_GasFilter_ErrPresNotSealed", "Минимльное давление теста герметичности, кПа", "Minimum pressure for seal test, kPa", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_GasFilter_Module_ErrPresNotSealed = new CommandInfo("SCP_GasFilter_Module_ErrPresNotSealed", "Минимльное давление теста герметичности модуля фильтрации, кПа", "Minimum pressure for seal test of filters module, kPa", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_GasFilter_BlowerPID_Kp = new CommandInfo("SCP_GasFilter_BlowerPID_Kp", "ПИД воздуходувки Kp", "Blower PID Kp", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_GasFilter_BlowerPID_Kd = new CommandInfo("SCP_GasFilter_BlowerPID_Kd", "ПИД воздуходувки Kd", "Blower PID Kd", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_GasFilter_BlowerPID_Ki = new CommandInfo("SCP_GasFilter_BlowerPID_Ki", "ПИД воздуходувки Ki", "Blower PID Ki", ValueCommandType.Real, "4", CommandType.SCP);

        // SetControlParameter section Process Chamber
        public static readonly CommandInfo SCP_PChamber_AlarmTemperature = new CommandInfo("SCP_PChamber_AlarmTemperature", "Предупреждение высокая темература в камере, кПа", "Warning process chamber high temperature, kPa", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_PChamber_ErrTemperature = new CommandInfo("SCP_PChamber_ErrTemperature", "Ошибка высокая температура в камере, кПа", "Error process chamber high temperature, kPa", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_PChamber_AlarmPressure = new CommandInfo("SCP_PChamber_AlarmPressure", "Предупреждение высокое давление в камере, кПа", "Warning process chamber high pressure, kPa", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_PChamber_ErrPressure = new CommandInfo("SCP_PChamber_ErrPressure", "Ошибка высокое давление в камере, кПа", "Error process chamber high pressure, kPa", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_PChamber_SetExhaustPressure = new CommandInfo("SCP_PChamber_SetExhaustPressure", "Открытие клапана сброса для компенсации избыточного давления, кПа", "Blow off valve for high chamber pressure, kPa", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_PChamber_SetInertFillPressure = new CommandInfo("SCP_PChamber_SetInertFillPressure", "Подача аргона для компенсации низкого давления, кПа", "Inert fill for low chamber pressure, kPa", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_PChamber_SetInertInletPressureErr = new CommandInfo("SCP_PChamber_SetInertInletPressureErr", "Ошибка низкое давление инертного газа в камере, кПа", "Error process chamber low inert gas pressure, kPa", ValueCommandType.Real, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_PChamber_SetInertInletPressureAlarm = new CommandInfo("SCP_PChamber_SetInertInletPressureAlarm", "Предупреждение низкое давление инертного газа в камере, кПа", "Warning process chamber low inert gas pressure, kPa", ValueCommandType.Real, "4", CommandType.SCP);

        // SetControlParameter
        public static readonly CommandInfo SCP_SetLayerThickness = new CommandInfo("SCP_SetLayerThickness", "Толщина слоя, мкм", "Layer thickness, mkm", ValueCommandType.Unsigned, "4", CommandType.SCP);
        public static readonly CommandInfo SCP_GUIState = new CommandInfo("SCP_GUIState", "Состояние GUI печати (0-нет печати, 1-пауза, 2-печать)", "GUI print state", ValueCommandType.Unsigned, "4", CommandType.SCP);


        public static readonly CommandInfo Com_Axes_RecoaterJogExtremeLeft = new CommandInfo("Com_Axes_RecoaterJogExtremeLeft", "Движение рекоутра в крайнее левое положение", "Движение рекоутра в крайнее левое положение", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Axes_RecoaterJogExtremeRight = new CommandInfo("Com_Axes_RecoaterJogExtremeRight", "Движение рекоутра в крайнее правое положение", "Движение рекоутра в крайнее левое положение", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Com_Axes_PlatformJogExtremeDown = new CommandInfo("Com_Axes_PlatformJogExtremeDown", "Движение стола в нижнее положение", "Движение рекоутра в крайнее левое положение", ValueCommandType.Bool, "4", CommandType.Com);

        public static readonly CommandInfo Print_Dosing = new CommandInfo("Print_Dosing", "Дозирование во время печати", "Дозирование во время печати", ValueCommandType.Bool, "4", CommandType.Com);
        public static readonly CommandInfo Print_Layer = new CommandInfo("Print_Layer", "Перемещение рекоутера во время печати", "Перемещение рекоутера во время печати", ValueCommandType.Bool, "4", CommandType.Com);

        public static readonly CommandInfo dosing1Completed = new CommandInfo("dosing1Completed", "Дозирование первого бункера завершено", "Дозирование первого бункера завершено", ValueCommandType.Bool, "4", CommandType.Trig);
        public static readonly CommandInfo dosing2Completed = new CommandInfo("dosing2Completed", "Дозирование второго бункера завершено", "Дозирование второго бункера завершено", ValueCommandType.Bool, "4", CommandType.Trig);

        public static readonly CommandInfo layerToDoorCompleted = new CommandInfo("layerToDoorCompleted", "Слой нанесен к двери", "Слой нанесен к двери", ValueCommandType.Bool, "4", CommandType.Trig);
        public static readonly CommandInfo layerToDoserCompleted = new CommandInfo("layerToDoserCompleted", "Слой нанесен к дозатору", "Слой нанесен к дозатору", ValueCommandType.Bool, "4", CommandType.Trig);
        
        public static readonly CommandInfo dosingSuccess = new CommandInfo("dosingSuccess", "Общий флаг успешного завершения дозирования", "Общий флаг успешного завершения дозирования", ValueCommandType.Bool, "4", CommandType.Trig);
        public static readonly CommandInfo layerSuccess = new CommandInfo("layerSuccess", "Общий флаг успешного перемещения рекоутера", "Общий флаг успешного перемещения рекоутера", ValueCommandType.Bool, "4", CommandType.Trig);

        // Готовности систем
        public static readonly CommandInfo readySystemLaserForPrint = new CommandInfo("readySystemLaserForPrint", "Готовность лазерной системы", "Готовность лазерной системы", ValueCommandType.Bool, "4", CommandType.Trig);
        public static readonly CommandInfo readySystemGasForPrint = new CommandInfo("readySystemGasForPrint", "Готовность газовой системы", "Готовность газовой системы", ValueCommandType.Bool, "4", CommandType.Trig);
        public static readonly CommandInfo readySistemHeatingTable = new CommandInfo("readySistemHeatingTable", "Готовность нагрева стола", "Готовность нагрева стола", ValueCommandType.Bool, "4", CommandType.Trig);
        public static readonly CommandInfo notErrorsBlockingPrinting = new CommandInfo("notErrorsBlockingPrinting", "Отсутствие ошибок блокирующих печать", "Отсутствие ошибок блокирующих печать", ValueCommandType.Bool, "4", CommandType.Trig);
        public static readonly CommandInfo Trig_Axes_DoserRef = new CommandInfo("Trig_Axes_DoserRef", "Рекоутер отреферирован", "Рекоутер отреферирован", ValueCommandType.Bool, "4", CommandType.Trig);
        public static readonly CommandInfo dispRealPosition = new CommandInfo("dispRealPosition", "Положение стола", "Положение стола", ValueCommandType.Dint, "4", CommandType.AM);
        public static readonly CommandInfo WorkAllowed = new CommandInfo("WorkAllowed", "Разрешение работы рекоутера при печати, если стол не выше отметки 31100", "Разрешение работы рекоутера при печати, если стол не выше отметки 31100", ValueCommandType.Bool, "4", CommandType.Trig);


        public static readonly CommandInfo RecouterInDoorState = new CommandInfo("RecouterInDoorState", "Разрешение работы рекоутера при печати, если стол не выше отметки 31100", "Разрешение работы рекоутера при печати, если стол не выше отметки 31100", ValueCommandType.Bool, "4", CommandType.Trig);
        public static readonly CommandInfo RecouterInHomeState = new CommandInfo("RecouterInHomeState", "Разрешение работы рекоутера при печати, если стол не выше отметки 31100", "Разрешение работы рекоутера при печати, если стол не выше отметки 31100", ValueCommandType.Bool, "4", CommandType.Trig);
        public static readonly CommandInfo IsMarking = new CommandInfo("IsMarking", "Разрешение работы рекоутера при печати, если стол не выше отметки 31100", "Разрешение работы рекоутера при печати, если стол не выше отметки 31100", ValueCommandType.Bool, "4", CommandType.Trig);
        public static readonly CommandInfo Print_PlatformDown = new CommandInfo("Print_PlatformDown", "Разрешение работы рекоутера при печати, если стол не выше отметки 31100", "Разрешение работы рекоутера при печати, если стол не выше отметки 31100", ValueCommandType.Bool, "4", CommandType.Trig);
        public static readonly CommandInfo platformDownCompleted = new CommandInfo("platformDownCompleted", "Разрешение работы рекоутера при печати, если стол не выше отметки 31100", "Разрешение работы рекоутера при печати, если стол не выше отметки 31100", ValueCommandType.Bool, "4", CommandType.Trig);

        public static List<CommandInfo> GetList()
        {
            return typeof(OpcCommands)
                .GetFields()
                .Where(p => p.FieldType == typeof(CommandInfo))
                .Select(p => (CommandInfo)p.GetValue(null)).ToList();
        }
    }
}


