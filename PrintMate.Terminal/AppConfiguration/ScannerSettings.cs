using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using PrintMate.Terminal.ConfigurationSystem.Core;
using Hans.NET.Models;

namespace PrintMate.Terminal.AppConfiguration
{
    public class ScannerSettings : ConfigurationModelBase
    {
        public List<ScanatorConfiguration> Scanners = new()
        {
            // Scanner 1 - From JSON configuration
            new ScanatorConfiguration
            {
                CardInfo = new CardInfo
                {
                    IpAddress = "172.18.34.227",
                    SeqIndex = 0,
                },
                ProcessVariablesMap = new ProcessVariablesMap
                {
                    NonDepends = new List<ProcessVariables>
                    {
                        new ProcessVariables
                        {
                            MarkSpeed = 1000,
                            JumpSpeed = 25000,
                            PolygonDelay = 170,
                            JumpDelay = 40000,
                            MarkDelay = 1200,
                            LaserOnDelay = 110.0,
                            LaserOffDelay = 120.0,
                            LaserOnDelayForSkyWriting = 130.0,
                            LaserOffDelayForSkyWriting = 140.0,
                            CurBeamDiameterMicron = 65.0,
                            CurPower = 50.0,
                            JumpMaxLengthLimitMm = 400.0,
                            MinJumpDelay = 400,
                            Swenable = true,
                            Umax = 0.1
                        }
                    },
                    MarkSpeed = new List<ProcessVariables>
                    {
                        new ProcessVariables
                        {
                            MarkSpeed = 800,
                            JumpSpeed = 25000,
                            PolygonDelay = 385,
                            JumpDelay = 40000,
                            MarkDelay = 470,
                            LaserOnDelay = 420.0,
                            LaserOffDelay = 490.0,
                            LaserOnDelayForSkyWriting = 600.0,
                            LaserOffDelayForSkyWriting = 730.0,
                            CurBeamDiameterMicron = 65.0,
                            CurPower = 50.0,
                            JumpMaxLengthLimitMm = 400.0,
                            MinJumpDelay = 400,
                            Swenable = true,
                            Umax = 0.1
                        },
                        new ProcessVariables
                        {
                            MarkSpeed = 1250,
                            JumpSpeed = 25000,
                            PolygonDelay = 465,
                            JumpDelay = 40000,
                            MarkDelay = 496,
                            LaserOnDelay = 375.0,
                            LaserOffDelay = 500.0,
                            LaserOnDelayForSkyWriting = 615.0,
                            LaserOffDelayForSkyWriting = 725.0,
                            CurBeamDiameterMicron = 65.0,
                            CurPower = 50.0,
                            JumpMaxLengthLimitMm = 400.0,
                            MinJumpDelay = 400,
                            Swenable = true,
                            Umax = 0.1
                        },
                        new ProcessVariables
                        {
                            MarkSpeed = 2000,
                            JumpSpeed = 25000,
                            PolygonDelay = 600,
                            JumpDelay = 40000,
                            MarkDelay = 540,
                            LaserOnDelay = 330.0,
                            LaserOffDelay = 530.0,
                            LaserOnDelayForSkyWriting = 630.0,
                            LaserOffDelayForSkyWriting = 720.0,
                            CurBeamDiameterMicron = 65.0,
                            CurPower = 50.0,
                            JumpMaxLengthLimitMm = 400.0,
                            MinJumpDelay = 400,
                            Swenable = true,
                            Umax = 0.1
                        }
                    }
                },
                ScannerConfig = new ScannerConfig
                {
                    FieldSizeX = 400.0f,
                    FieldSizeY = 400.0f,
                    ProtocolCode = 1,
                    CoordinateTypeCode = 5,
                    OffsetX = -1,
                    OffsetY = 94,
                    OffsetZ = -0.001f,
                    ScaleX = 1.0f,
                    ScaleY = 1.0f,
                    ScaleZ = 1.0f,
                    RotateAngle = 0.0f
                },
                BeamConfig = new BeamConfig
                {
                    MinBeamDiameterMicron = 50,
                    WavelengthNano = 1070.0,
                    RayleighLengthMicron = 1921.0,
                    M2 = 1.593,
                    FocalLengthMm = 538.46,
                    ActualPowerOffsetValue = new List<float> { 64.7f, 71.0f, 102.5f, 136.4f, 208.5f, 265.6f }
                },
                LaserPowerConfig = new LaserPowerConfig
                {
                    MaxPower = 500.0f,
                    ActualPowerCorrectionValue = new List<float> { 0.0f, 67.0f, 176.0f, 281.0f, 382.0f, 475.0f },
                    PowerOffsetKFactor = -0.6839859f,
                    PowerOffsetCFactor = 51.298943f
                },
                FunctionSwitcherConfig = new FunctionSwitcherConfig
                {
                    EnablePowerOffset = false,
                    EnablePowerCorrection = true,
                    EnableZCorrection = true,
                    EnableDiameterChange = true,
                    EnableDynamicChangeVariables = true,
                    LimitVariablesMinPoint = true,
                    LimitVariablesMaxPoint = true,
                    EnableVariableJumpDelay = true
                },
                ThirdAxisConfig = new ThirdAxisConfig
                {
                    Bfactor = 0.013944261,
                    Cfactor = -7.870427,
                    Afactor = 0.0,
                    BaseFocal = 538.46f
                }
            },
            // Scanner 2 - From JSON configuration
            new ScanatorConfiguration
            {
                CardInfo = new CardInfo
                {
                    IpAddress = "172.18.34.228",
                    SeqIndex = 1,
                },
                ProcessVariablesMap = new ProcessVariablesMap
                {
                    NonDepends = new List<ProcessVariables>
                    {
                        new ProcessVariables
                        {
                            MarkSpeed = 1000,
                            JumpSpeed = 25000,
                            PolygonDelay = 170,
                            JumpDelay = 35000,
                            MarkDelay = 1200,
                            LaserOnDelay = 110.0,
                            LaserOffDelay = 120.0,
                            LaserOnDelayForSkyWriting = 130.0,
                            LaserOffDelayForSkyWriting = 140.0,
                            CurBeamDiameterMicron = 67.0,
                            CurPower = 50.0,
                            JumpMaxLengthLimitMm = 300.0,
                            MinJumpDelay = 400,
                            Swenable = true,
                            Umax = 0.1
                        }
                    },
                    MarkSpeed = new List<ProcessVariables>
                    {
                        new ProcessVariables
                        {
                            MarkSpeed = 800,
                            JumpSpeed = 25000,
                            PolygonDelay = 385,
                            JumpDelay = 35000,
                            MarkDelay = 470,
                            LaserOnDelay = 420.0,
                            LaserOffDelay = 490.0,
                            LaserOnDelayForSkyWriting = 560.0,
                            LaserOffDelayForSkyWriting = 700.0,
                            CurBeamDiameterMicron = 67.0,
                            CurPower = 50.0,
                            JumpMaxLengthLimitMm = 300.0,
                            MinJumpDelay = 400,
                            Swenable = true,
                            Umax = 0.1
                        },
                        new ProcessVariables
                        {
                            MarkSpeed = 1250,
                            JumpSpeed = 25000,
                            PolygonDelay = 465,
                            JumpDelay = 35000,
                            MarkDelay = 496,
                            LaserOnDelay = 375.0,
                            LaserOffDelay = 500.0,
                            LaserOnDelayForSkyWriting = 565.0,
                            LaserOffDelayForSkyWriting = 690.0,
                            CurBeamDiameterMicron = 67.0,
                            CurPower = 50.0,
                            JumpMaxLengthLimitMm = 300.0,
                            MinJumpDelay = 400,
                            Swenable = true,
                            Umax = 0.1
                        },
                        new ProcessVariables
                        {
                            MarkSpeed = 2000,
                            JumpSpeed = 25000,
                            PolygonDelay = 600,
                            JumpDelay = 35000,
                            MarkDelay = 540,
                            LaserOnDelay = 345.0,
                            LaserOffDelay = 510.0,
                            LaserOnDelayForSkyWriting = 570.0,
                            LaserOffDelayForSkyWriting = 685.0,
                            CurBeamDiameterMicron = 67.0,
                            CurPower = 50.0,
                            JumpMaxLengthLimitMm = 300.0,
                            MinJumpDelay = 400,
                            Swenable = true,
                            Umax = 0.1
                        }
                    }
                },
                ScannerConfig = new ScannerConfig
                {
                    FieldSizeX = 400.0f,
                    FieldSizeY = 400.0f,
                    ProtocolCode = 1,
                    CoordinateTypeCode = 5,
                    OffsetX = 1,
                    OffsetY = -94,
                    OffsetZ = 0.102f,
                    ScaleX = 1.0f,
                    ScaleY = 1.0f,
                    ScaleZ = 1.0f,
                    RotateAngle = 0.0f
                },
                BeamConfig = new BeamConfig
                {
                    MinBeamDiameterMicron = 50,
                    WavelengthNano = 1070.0,
                    RayleighLengthMicron = 1863.0,
                    M2 = 1.553,
                    FocalLengthMm = 538.46,
                    ActualPowerOffsetValue = new List<float> { 64.7f, 71.0f, 102.5f, 136.4f, 208.5f, 265.6f }
                },
                LaserPowerConfig = new LaserPowerConfig
                {
                    MaxPower = 500.0f,
                    ActualPowerCorrectionValue = new List<float> { 0.0f, 69.0f, 177.0f, 282.0f, 385.0f, 475.0f },
                    PowerOffsetKFactor = -1.0362141f,
                    PowerOffsetCFactor = 77.71606f
                },
                FunctionSwitcherConfig = new FunctionSwitcherConfig
                {
                    EnablePowerOffset = false,
                    EnablePowerCorrection = true,
                    EnableZCorrection = true,
                    EnableDiameterChange = true,
                    EnableDynamicChangeVariables = true,
                    LimitVariablesMinPoint = true,
                    LimitVariablesMaxPoint = true,
                    EnableVariableJumpDelay = true
                },
                ThirdAxisConfig = new ThirdAxisConfig
                {
                    Bfactor = 0.0139135085,
                    Cfactor = -7.477868,
                    Afactor = 0.0,
                    BaseFocal = 538.46f
                }
            }
        };

        public ScanatorConfiguration? GetConfigurationByAddress(string address)
        {
            return Scanners.FirstOrDefault(p => p.CardInfo.IpAddress == address);
        }

        public ScanatorConfiguration? GetConfigurationByFixedIndex(int index)
        {
            return Scanners.FirstOrDefault(p => p.CardInfo.SeqIndex == index);
        }

    }

}
