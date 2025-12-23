using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LayerAnalyzer.Lib.Models.Calibration;
using LayerAnalyzer.Lib.Services.Utils;
using PrintMate.Terminal.ConfigurationSystem.Core;

namespace PrintMate.Terminal.AppConfiguration
{
    //TODO: добавить потом в configureParameters
    public class LayerAnalyzerSettings : ConfigurationModelBase
    {
        // Пути для маски и калибровочного файла
        public string RoiMaskPath = @"CalibrateData\RoiMask.png";
        public string CalibrationSettingsPath = @"CalibrateData\CalibrateData.json";

        // Использование правил
        public bool IsRepeatedRecoaterStripe = true;
        public bool IsPartDelamination = true;
        public bool IsLackOfPowder = true;
        public bool IsPlatformAnomaly = true;


        // Настройки правил для анализатора слоёв
        public byte ObserveLayersCount = 5; // N последних слоёв для хранения
        public byte CountLayerWithDefectRakel = 3; // M дефектов подряд на N слоёв
        public ushort MinAreaMm2PartDelamination = 100; // обнаруживать дефекты "отслоение детали" не меньше этой площади
        public ushort MinAreaMm2PlatformAnomaly = 400; // обнаруживать дефекты "посторонний объект на слое" не меньше этой площади
        public byte PercentAreaLackOfPowder = 1; // недостаток порошка, если платформа засыпана меньше, чем на (100-percentArea)
        public string LayerContoursFolder = null; // ОПЦИОНАЛЬНО: хранение контуров доступно в оперативной памяти
    }
}
