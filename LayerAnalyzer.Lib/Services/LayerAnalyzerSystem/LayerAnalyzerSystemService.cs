using Emgu.CV;
using LayerAnalyzer.Lib.Models;
using LayerAnalyzer.Lib.Models.Defects;
using LayerAnalyzer.Lib.Services.ContourCache;
using LayerAnalyzer.Lib.Services.ContourClassification;
using System.Drawing.Drawing2D;
using LayerAnalyzer.Lib.Models.Calibration;
using LayerAnalyzer.Lib.Services.LayerAnalyzerSystem.AnalyzeRules;

namespace LayerAnalyzer.Lib.Services.LayerAnalyzerSystem;

/// <summary>
/// Главная система анализа слоёв - оркестратор всех компонентов
/// </summary>
public class LayerAnalyzerSystemService : IDisposable
{
    private readonly ContourClassifyProducer _contourClassifyProducer;
    private readonly LayerAnalyzer _layerAnalyzer = new();
    private readonly CalibrationSettings _calibrationSettings;

    private int _countRegisteredCapture = 0;
    private int _curLayer = 0;
    private bool _isStartLayer = true;

    private LayerAnalyzerSystemService(
        CalibrationSettings calibrationSettings,
        List<IAnalyzeRule> analyzeRules,
        IContourCache cache,
        DirectoryInfo? imageDirectory,
        string roiMaskPath)
    {
        _calibrationSettings = calibrationSettings;
        _contourClassifyProducer = new ContourClassifyProducer(calibrationSettings, cache, imageDirectory, roiMaskPath);


        foreach (var analyzeRule in analyzeRules)
        {
            _layerAnalyzer.AddAnalyzeRule(analyzeRule);
        }
    }

    /// <summary>
    /// Переходит на новый слой
    /// </summary>
    public void NewLayer()
    {
        if (!_isStartLayer)
        {
            _curLayer++;
        }
        _isStartLayer = false;
    }

    /// <summary>
    /// Сбрасывает состояние системы
    /// </summary>
    public void Refresh()
    {
        _contourClassifyProducer.Refresh();
        _countRegisteredCapture = 0;
    }

    /// <summary>
    /// Получает статус вычислений
    /// </summary>
    public ComputeStatus GetComputingStatus()
    {
        if (!_contourClassifyProducer.IsFullData())
        {
            return ComputeStatus.MissingData;
        }

        if (_contourClassifyProducer.IsDone())
        {
            return ComputeStatus.Done;
        }

        return ComputeStatus.Running;
    }

    /// <summary>
    /// Регистрирует снимок (с автоматической коррекцией дисторсии)
    /// </summary>
    public Mat? RegisterCapture(Mat capture, CaptureType type)
    {
        if (capture == null)
        {
            return null;
        }

        // TODO: убрать после тестов (убрано)
        //Mat undistortedCapture = capture.Clone(); // для тестов (картинки из paint)
        Mat undistortedCapture = _calibrationSettings.ApplyToFrame(capture);

        bool isRegisterSuccess = false;
        _isStartLayer = false;
        
        // Если уже зарегистрировано 2 снимка - сбрасываем
        if (_countRegisteredCapture >= 2)
        {
            Refresh();
        }

        // Первый снимок должен быть AFTER_EXPOSURE
        if (_countRegisteredCapture == 0 && type != CaptureType.AfterExposure)
        {
            Refresh();
            return undistortedCapture;
        }

        // Регистрируем снимок в зависимости от типа
        if (type == CaptureType.AfterRecoating)
        {
            isRegisterSuccess = _contourClassifyProducer.RegisterImageAfterRecoating(undistortedCapture, _curLayer);
        }
        else if (type == CaptureType.AfterExposure)
        {
            isRegisterSuccess = _contourClassifyProducer.RegisterImageAfterExposure(undistortedCapture, _curLayer);
        }

        if (isRegisterSuccess)
        {
            _countRegisteredCapture++;
            return undistortedCapture;
        }

        undistortedCapture.Dispose();
        return null;
    }

    /// <summary>
    /// Устанавливает начальный слой
    /// </summary>
    public void SetStartLayer(int startLayer)
    {
        _isStartLayer = true;
        _curLayer = startLayer;

        _contourClassifyProducer.SetStartLayer(startLayer + 1);
        _contourClassifyProducer.Refresh();
    }

    /// <summary>
    /// Ожидает завершения анализа и возвращает дефекты
    /// </summary>
    public List<Defect> WaitAnalyze()
    {
        if (_countRegisteredCapture == 2)
        {
            var classifiedContours = _contourClassifyProducer.GetCachingClassifyContours();
            return _layerAnalyzer.Analyze(classifiedContours);
        }

        Refresh();
        return new List<Defect>();
    }

    /// <summary>
    /// Добавляет контур для пропуска (игнорирования)
    /// </summary>
    public void AddSkipContour(GraphicsPath skipContour)
    {
        _layerAnalyzer.AddSkipContour(skipContour);
    }

    /// <summary>
    /// Получение списка правил для анализа
    /// </summary>
    public IReadOnlyList<IAnalyzeRule> GetAnalyzeRules()
    {
        return _layerAnalyzer.GetAnalyzeRules();
    }

    /// <summary>
    /// Освобождает ресурсы
    /// </summary>
    public void Dispose()
    {
        _contourClassifyProducer?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Builder для создания LayerAnalyzerSystem
    /// </summary>
    public class Builder : LayerAnalyzerSystemBuilder
    {
        public override LayerAnalyzerSystemService Build()
        {
            if (CalibrationSettings == null)
            {
                throw new InvalidOperationException("Calibration settings is not set");
            }

            if (string.IsNullOrEmpty(RoiMaskPath))
            {
                throw new InvalidOperationException("ROI mask is not set");
            }

            // Создаём кэш контуров
            IContourCache cache;
            if (ContourDirectory != null)
            {
                cache = new ContourCache.ContourCache(CurBufferSize, ContourDirectory.FullName);
            }
            else
            {
                cache = new CacheInRam(CurBufferSize);
            }

            // Создаём и возвращаем LayerAnalyzerSystemService с новыми параметрами
            return new LayerAnalyzerSystemService(
                CalibrationSettings,
                AnalyzeRules,
                cache,
                ImageDirectory,
                RoiMaskPath);
        }
    }
}
