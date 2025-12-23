using LayerAnalyzer.Lib.Services.LayerAnalyzerSystem;
using PrintMate.Terminal.AppConfiguration;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Documents;
using Emgu.CV;
using LayerAnalyzer.Lib.Models;
using LayerAnalyzer.Lib.Models.Defects;
using LayerAnalyzer.Lib.Services.LayerAnalyzerSystem.AnalyzeRules;
using PrintMate.Terminal.ConfigurationSystem.Core;

namespace PrintMate.Terminal.Services
{
    public class LayerAnalyzerService
    {
        private readonly LayerAnalyzerSystemService _layerAnalyzerSystem;
        private readonly ConfigurationManager _configurationManager;
        public List<Defect> Defects { get; set; } = [];

        public LayerAnalyzerService(ConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
            var settings = _configurationManager.Get<LayerAnalyzerSettings>();


            if (string.IsNullOrEmpty(settings.CalibrationSettingsPath) ||
                string.IsNullOrEmpty(settings.RoiMaskPath))
            {
                throw new ArgumentNullException();
            }

            // Создание LayerAnalyzerSystem
            var builder = new LayerAnalyzerSystemService.Builder();

            // Устанавливаем калибровочный файл и маску
            builder
                .SetCalibrationSettingsFile(settings.CalibrationSettingsPath)
                .SetRoiMaskPath(settings.RoiMaskPath);

            // Добавляем необходимые правила для анализа
            if (settings.IsRepeatedRecoaterStripe)
                builder.AddAnalyzeRule(new RepeatedRecoaterStripeRule(settings.ObserveLayersCount,
                    settings.CountLayerWithDefectRakel));
            if (settings.IsPartDelamination)
                builder.AddAnalyzeRule(new PartDelaminationRule(settings.ObserveLayersCount,
                    settings.MinAreaMm2PartDelamination));
            if (settings.IsLackOfPowder)
                builder.AddAnalyzeRule(new LackOfPowderRule(settings.ObserveLayersCount,
                    settings.PercentAreaLackOfPowder));
            if (settings.IsPlatformAnomaly)
                builder.AddAnalyzeRule(new PlatformAnomalyRule(settings.ObserveLayersCount,
                    settings.MinAreaMm2PlatformAnomaly));

            // Настройка директорий для сохранения (опционально)
            if (!string.IsNullOrEmpty(settings.LayerContoursFolder))
            {
                var outputImageDir = Path.Combine(settings.LayerContoursFolder, "images");
                var outputContourDir = Path.Combine(settings.LayerContoursFolder, "contours");

                builder.SaveImageInDirectory(outputImageDir)
                    .SaveContourInDirectory(outputContourDir);
            }

            _layerAnalyzerSystem = builder.Build();
        }

        /// <summary>
        /// Установка начального слоя
        /// </summary>
        public void SetStartLayer(int startLayer)
        {
            // Обычно startLayer = 1
            // 
            _layerAnalyzerSystem.SetStartLayer(startLayer);
        }

        /// <summary>
        /// Регистрация эталонного слоя (фон)
        /// </summary>
        public void RegisterReferenceCapture(Mat referenceMat)
        {
            _layerAnalyzerSystem.RegisterCapture(referenceMat, CaptureType.AfterRecoating);
        }

        /// <summary>
        /// Регистрация изображения после лазера
        /// </summary>
        public void RegisterCaptureAfterExposure(Mat capture)
        {
            _layerAnalyzerSystem.RegisterCapture(capture, CaptureType.AfterExposure);
            _layerAnalyzerSystem.NewLayer();
        }

        /// <summary>
        /// Регистрация изображения после рекоутинга
        /// </summary>
        public void RegisterCaptureAfterRecoating(Mat capture)
        {
            _layerAnalyzerSystem.RegisterCapture(capture, CaptureType.AfterRecoating);
            Defects.AddRange(_layerAnalyzerSystem.WaitAnalyze());
        }

        /// <summary>
        /// Добавить контур для пропуска
        /// </summary>
        /// <param name="skipContour"></param>
        public void AddSkipContour(GraphicsPath skipContour)
        {
            _layerAnalyzerSystem.AddSkipContour(skipContour);
        }

        /// <summary>
        /// Получить статус вычислений
        /// </summary>
        /// <returns></returns>
        public ComputeStatus GetComputingStatus()
        {
            return _layerAnalyzerSystem.GetComputingStatus();
        }

        /// <summary>
        /// Получить список правил для анализа
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<IAnalyzeRule> GetAnalyzeRules()
        {
            return _layerAnalyzerSystem.GetAnalyzeRules();
        }
    }
}
