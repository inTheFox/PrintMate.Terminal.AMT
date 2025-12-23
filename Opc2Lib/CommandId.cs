public enum CommandId
{
    // COM section Axes
    Com_Axes_Doser,
    Com_Axes_RecoaterJogLeft,
    Com_Axes_RecoaterJogRight,
    Com_Axes_PlatformJogUp,
    Com_Axes_PlatformStepUp,
    Com_Axes_PlatformJogDown,
    Com_Axes_PlatformStepDown,
    Com_Axes_Stop,
    Com_Axes_RecoaterToLeftPos,
    Com_Axes_RecoaterToRightPos,
    Com_Axes_RecoaterToLoadPos,
    Com_Axes_DoserRef,
    Com_Axes_RecoaterRef,
    Com_Axes_PlatformRef,
    Com_Axes_PlatformRefREL,
    Com_Axes_PlatformPosZero,

    // COM section Process Chamber
    Com_PChamber_LightPB,
    Com_PChamber_Light,
    Com_PChamber_CameraLock,
    Com_PChamber_VExhaustCamera,
    Com_PChamber_VExhaustCameraRegulated,
    Com_PChamber_PlatformHeater,

    // COM section Powder
    Com_Powder_DoserVibro,

    // COM section GasFilter
    Com_GasFilter_VGasToCamera,
    Com_GasFilter_VGasFromCamera,
    Com_GasFilter_ResetInertCons,
    Com_GasFilter_Blower,
    Com_GasFilter_Inert,
    Com_GasFilter_Module_PowerChiller,
    Com_GasFilter_Module_VSupplyDischargeTank,
    Com_GasFilter_Module_VInletInert,
    Com_GasFilter_Module_VExhaust,
    Com_GasFilter_Module_VDischargeFilter1,
    Com_GasFilter_Module_VDischargeFilter2,
    Com_GasFilter_Module_VDischargeFilter3,
    Com_GasFilter_Module_VDischargeFilter4,

    // COM section Laser
    Com_Laser_Emission,
    Com_Laser_Reset,
    Com_Laser_ScannersCooling,
    Com_Laser1_PowerChiller,
    Com_Laser2_PowerChiller,

    // COM section Common
    Com_Layer,
    Com_Recoat,
    Com_LaserSystem,
    Com_GasSystem,
    Com_PenultimateLayer,
    Com_HeartBit,

    // TRIG section
    Trig_Axes_RecoaterRef,
    Trig_Layer,
    Trig_Recoat,
    Trig_AllSystemReady,
    Trig_SensorKeyLocked,
    Trig_PowerSwitch,
    Trig_RKF,

    // AM section Powder
    AM_Powder_Level1,
    AM_Powder_Level2,

    // AM section Process Chamber
    AM_PChamber_Pressure,
    AM_PChamber_Oxygen,
    AM_PChamber_Temperature,
    AM_PChamber_HeaterTemperature1,
    AM_PChamber_HeaterTemperature2,
    AM_PChamber_InertInletPressure,

    // AM section Gas and Filters
    AM_GasFilter_FilterOxygen,
    AM_GasFilter_PressureFilter,
    AM_GasFilter_GasFlow,
    AM_GasFilter_InertInletPressure,
    AM_GasFilter_InertConsumption,
    AM_GasFilter_InertCurrentConsumption,
    AM_GasFilter_BlowerTemperature,
    AM_GasFilter_VenturiTemperature,
    AM_GasFilter_Module_DischargeTankPressure,
    AM_GasFilter_Module_Pressure,
    AM_GasFilter_Module_RH,
    AM_GasFilter_Module_Temperature,
    AM_GasFilter_Module_Oxygen,

    // AM section Axes
    AM_Axes_RecoaterABSPosition,
    AM_Axes_PlatformRELPosition,

    // DISP section
    DISP_PositionRecoater,
    DISP_PositionPlatform,

    // DM section Process Chamber
    DM_PChamber_ChamberDoorLS,
    DM_PChamber_ExhaustValve,
    DM_PChamber_ExhaustValveRegulated,
    DM_PChamber_GloveBoxLS,

    // DM section Gas Filters
    DM_GasFilter_FilterPresentLS,
    DM_GasFilter_VToCameraOpenedLS,
    DM_GasFilter_VToCameraClosedLS,
    DM_GasFilter_VFromCameraOpenedLS,
    DM_GasFilter_VFromCameraClosedLS,
    DM_GasFilter_Module_ChillerOK,
    DM_GasFilter_Module_LidClosedLS,
    DM_GasFilter_Module_DumpBucketPresentLS,
    DM_GasFilter_Module_DumpBucketLidClosedLS,
    DM_GasFilter_AirInputPressure,

    // DM section Laser
    DM_Laser1_ChillerOK,
    DM_Laser1_PowerStateLaser,
    DM_Laser1_EmissionStateLaser,
    DM_Laser2_ChillerOK,
    DM_Laser2_PowerStateLaser,
    DM_Laser2_EmissionStateLaser,

    // DM section Axes
    DM_Axes_RecoaterLeftLS,
    DM_Axes_RecoaterRightLS,
    DM_Axes_PlatformTopLS,
    DM_Axes_PlatformBottomLS,

    // DM section Powder
    DM_Powder_LSRight,
    DM_Powder_LSLeft,
    DM_Powder_PowderDispose_DoorLeftLS,
    DM_Powder_PowderDispose_DoorRightLS,
    DM_Powder_PowderDispose_DoserLeftLS,
    DM_Powder_PowderDispose_DoserRightLS,

    // SET section
    Set_GasFilter_BlowerSpeed,
    Set_Axes_PlatformStep,
    Set_Axes_PlatformSpeed,
    Set_Axes_RecoaterSpeed,
    Set_Axes_DoserSpeed,
    Set_Axes_DoserCounts,
    Set_PChamber_PlatformHeatingTemperature,

    // Alarm section PChamber
    Alarm_PChamber_Unlocked,
    Alarm_PChamber_Pressure,
    Alarm_PChamber_HighOxygen,
    Alarm_PChamber_Temperature,
    Alarm_PChamber_InertInletPressure,

    // Alarm section Powder
    Alarm_Powder_Empty,

    // Alarm section GasFilter
    Alarm_GasFilter_HighOxygen,
    Alarm_GasFilter_FilterClogg,
    Alarm_GasFilter_InertPressure,
    Alarm_GasFilter_HighBlowerTemp,
    Alarm_GasFilter_InertFlowSensor,
    Alarm_GasFilter_Module_FilterDoorLS,
    Alarm_GasFilter_Module_SupplyGasDoorLeftLS,
    Alarm_GasFilter_Module_SupplyGasDoorRightLS,
    Alarm_GasFilter_Module_SupplyGasHighOxygen,

    // Error section PChamber
    Err_PChamber_Pressure,
    Err_PChamber_HighOxygen,
    Err_PChamber_Temperature,
    Err_PChamber_TemperatureSensor,
    Err_PChamber_PlatformHeatingTemperatureSensor1,
    Err_PChamber_PlatformHeatingTemperatureSensor2,
    Err_PChamber_PressureSensor,
    Err_PChamber_OxygenSensor,
    Err_PChamber_InertInletPressure,
    Err_PChamber_InertInletPressureSensor,

    // Error section Axes
    Err_Axes_Platform,
    Err_Axes_Recoater,
    Err_Axes_Doser,
    Err_Axes_ErrPLCModuleExtension,
    Err_Axes_RKF,

    // Error section Laser
    Err_Laser1_Chiller,
    Err_Laser1,
    Err_Laser2_Chiller,
    Err_Laser2,

    // Error section Common
    Err_EmergencyStop,

    // Error section GasFilter
    Err_GasFilter_BlowerFC,
    Err_GasFilter_FilterPresent,
    Err_GasFilter_PressureSensor,
    Err_GasFilter_OxygenSensor,
    Err_GasFilter_FlowSensor,
    Err_GasFilter_FilterClogg,
    Err_GasFilter_HighOxygen,
    Err_GasFilter_InertPressure,
    Err_GasFilter_HighBlowerTemp,
    Err_GasFilter_InertPressureSensor,
    Err_GasFilter_FilterClosed,
    Err_GasFilter_VToCameraTimeout,
    Err_GasFilter_VFromCameraTimeout,
    Err_GasFilter_TempBlowerMotorSensor,
    Err_GasFilter_TempVenturiSensor,
    Err_GasFilter_NotSealed,
    Err_GasFilter_InputAirPressure,
    Err_GasFilter_Module_NotSealed,
    Err_GasFilter_Module_DischargeTankPressureSensor,
    Err_GasFilter_Module_DumpBucketNotSealed,
    Err_GasFilter_Module_Chiller,
    Err_GasFilter_Module_LidClose,
    Err_GasFilter_Module_SupplyGasPressureSensor,
    Err_GasFilter_Module_SupplyGasRHSensor,
    Err_GasFilter_Module_SupplyGasTemperatureSensor,
    Err_GasFilter_Module_SupplyGasHighOxygen,
    Err_GasFilter_Module_OxygenSensor,

    // Error section Powder
    Err_Powder_PowderLevel1Sensor,
    Err_Powder_PowderLevel2Sensor,
    Err_Powder_DoserStuck,
    Err_Powder_Empty,
    Err_Powder_PowderDispose_FullDoor,
    Err_Powder_PowderDispose_FullDoser,
    Err_Powder_RecoaterRef,

    // SetControlParameter section Axes
    SCP_Axes_RecoaterFrontOnExpose,
    SCP_Axes_JogSpeedRecoater,
    SCP_Axes_SetRecoaterDistRight,
    SCP_Axes_SetRecoaterDistDoser,
    SCP_Axes_PlatformBacklashDOWN,
    SCP_Axes_PlatformCorrectionDOWN,

    // SetControlParameter section Powder
    SCP_Powder_SetPowderLevelAlarm,

    // SetControlParameter section GasFilter
    SCP_GasFilter_AlarmTempBlower,
    SCP_GasFilter_ErrTempBlower,
    SCP_GasFilter_BlowerMaxFlow,
    SCP_GasFilter_AlarmGasFlow,
    SCP_GasFilter_ErrGasFlow,
    SCP_GasFilter_SetPointO2,
    SCP_GasFilter_StartPointO2,
    SCP_GasFilter_InertFillChamber,
    SCP_GasFilter_AlarmInertPressure,
    SCP_GasFilter_ErrInertPressure,
    SCP_GasFilter_AlarmO2,
    SCP_GasFilter_ErrO2,
    SCP_GasFilter_ErrFilterClosed,
    SCP_GasFilter_ErrPresNotSealed,
    SCP_GasFilter_Module_ErrPresNotSealed,
    SCP_GasFilter_BlowerPID_Kp,
    SCP_GasFilter_BlowerPID_Kd,
    SCP_GasFilter_BlowerPID_Ki,

    // SetControlParameter section PChamber
    SCP_PChamber_AlarmTemperature,
    SCP_PChamber_ErrTemperature,
    SCP_PChamber_AlarmPressure,
    SCP_PChamber_ErrPressure,
    SCP_PChamber_SetExhaustPressure,
    SCP_PChamber_SetInertFillPressure,
    SCP_PChamber_SetInertInletPressureAlarm,
    SCP_PChamber_SetInertInletPressureErr,

    // SetControlParameter section Common
    SCP_SetLayerThickness,
    SCP_GUIState
}