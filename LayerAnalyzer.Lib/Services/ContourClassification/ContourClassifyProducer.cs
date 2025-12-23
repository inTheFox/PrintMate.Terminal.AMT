using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using LayerAnalyzer.Lib.Models.Calibration;
using LayerAnalyzer.Lib.Models.Contours;
using LayerAnalyzer.Lib.Models.Defects;
using LayerAnalyzer.Lib.Services.ContourCache;
using LayerAnalyzer.Lib.Services.ContourDetection;
using LayerAnalyzer.Lib.Services.ContourFiltering;
using LayerAnalyzer.Lib.Services.Utils.CommonOcvService;
using Microsoft.VisualBasic;

namespace LayerAnalyzer.Lib.Services.ContourClassification;

/// <summary>
/// Асинхронный производитель классифицированных контуров.
/// Управляет обнаружением, классификацией и кэшированием контуров для анализа слоёв.
/// </summary>
public class ContourClassifyProducer : IDisposable
{
    private readonly Mat _roiMask;
    private readonly SizeF _frameSizeMm;
    private readonly Size _frameSizePx;
    private readonly IContourCache _cache;
    private readonly DirectoryInfo? _imageDirectory;
    private int _lastLayer = 0;

    // Асинхронные задачи для обнаружения контуров
    private Task<VectorOfVectorOfPoint>? _detectorTaskAR;  // After Recoating
    private Task<VectorOfVectorOfPoint>? _detectorTaskAE;  // After Exposure
    private Task<VectorOfVectorOfPoint>? _detectorTaskRD;  // Rakel Defects
    private Task? _classifierContoursTask;

    // Синхронизация
    private readonly SemaphoreSlim _semaphoreAE = new(0, 2);
    private readonly SemaphoreSlim _semaphoreAR = new(0, 2);
    private readonly object _lock = new();

    // Токен для отмены
    private CancellationTokenSource? _cancellationTokenSource;

    public ContourClassifyProducer(
        CalibrationSettings calibrationSettings,
        IContourCache cache,
        DirectoryInfo? undistortedImageDirectory,
        string roiMaskPath)
    {
        _roiMask = calibrationSettings.GetRoiMask(roiMaskPath);
        _frameSizeMm = calibrationSettings.FrameSizeMm;
        _frameSizePx = calibrationSettings.FrameSizePx;
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _imageDirectory = undistortedImageDirectory;
    }

    /// <summary>
    /// Устанавливает начальный слой
    /// </summary>
    public void SetStartLayer(int startLayer)
    {
        _cache.Clear();
        _cache.SetCurLayer(startLayer);
    }

    /// <summary>
    /// Регистрирует изображение после экспозиции (лазерного воздействия)
    /// </summary>
    public bool RegisterImageAfterExposure(Mat undistortImage, int numLayer)
    {
        lock (_lock)
        {
            if (_detectorTaskAE != null)
            {
                return false;
            }

            // Создаём токен, если ещё не создан (например, после Refresh)
            if (_cancellationTokenSource == null || _cancellationTokenSource.Token.IsCancellationRequested)
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }

            _detectorTaskAE = Task.Run(() =>
            {
                var detector = new ContourDetectorAfterExposure();
                return detector.GetContours(undistortImage.Clone());
            }, _cancellationTokenSource.Token); // Передаём токен в Task.Run (хотя детектор сам по себе может его не использовать)

            if (_classifierContoursTask == null)
            {
                _classifierContoursTask = Task.Run(async () => await SaveInCacheContoursProcessAsync(_cancellationTokenSource.Token));
            }

            _semaphoreAE.Release();

            if (_imageDirectory != null)
            {
                string filename = Path.Combine(_imageDirectory.FullName, $"{numLayer}_AE.png");
                CvInvoke.Imwrite(filename, undistortImage);
                _lastLayer = numLayer;
            }

            return true;
        }
    }

    /// <summary>
    /// Регистрирует изображение после нанесения порошка (recoating)
    /// </summary>
    public bool RegisterImageAfterRecoating(Mat undistortImage, int numLayer)
    {
        lock (_lock)
        {
            if (_detectorTaskAR != null || _detectorTaskRD != null)
            {
                return false;
            }

            // Создаём токен, если ещё не создан (например, после Refresh)
            if (_cancellationTokenSource == null || _cancellationTokenSource.Token.IsCancellationRequested)
            {
                _cancellationTokenSource = new CancellationTokenSource();
            }

            _detectorTaskAR = Task.Run(() =>
            {
                var detector = new ContourDetectorAfterRecoating();
                return detector.GetContours(undistortImage);
            }, _cancellationTokenSource.Token);

            _detectorTaskRD = Task.Run(() =>
            {
                var detector = new RakelDefectDetector();
                return detector.GetContours(undistortImage);
            }, _cancellationTokenSource.Token);

            if (_classifierContoursTask == null)
            {
                _classifierContoursTask = Task.Run(async () => await SaveInCacheContoursProcessAsync(_cancellationTokenSource.Token));
            }

            _semaphoreAR.Release();

            if (_imageDirectory != null)
            {
                string filename = Path.Combine(_imageDirectory.FullName, $"{numLayer}_AR.png");
                CvInvoke.Imwrite(filename, undistortImage);
                _lastLayer = numLayer;
            }

            return true;
        }
    }

    /// <summary>
    /// Основной процесс сохранения классифицированных контуров в кэш
    /// </summary>
    private async Task SaveInCacheContoursProcessAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Ждём изображение после экспозиции
            await _semaphoreAE.WaitAsync(cancellationToken);
            var contourAE = await _detectorTaskAE!;

            // Создаём области пересечения для классификации
            IntersectArea intersectPlatformArea = new(
                _roiMask.Clone(),
                new MCvScalar(0,255,0),
                DefectType.OnPlatform);

            // Получаем контуры деталей (внутри платформы)
            var detailContours = ContourClassifierUtils.GetContoursInArea(
                intersectPlatformArea,
                contourAE);

            IntersectArea intersectDetailArea = IntersectArea.FromContour(
                detailContours,
                _roiMask.Size,
                DefectType.OnDetail);

            intersectPlatformArea.ExcludeArea(intersectDetailArea);

            // Ждём изображение после нанесения порошка
            await _semaphoreAR.WaitAsync(cancellationToken);
            var contourAR = await _detectorTaskAR!;
            var contourRD = await _detectorTaskRD!;


            // Создаём все области для классификации
            var intersectAreas = new List<IntersectArea>
            {
                intersectDetailArea,
                new(_roiMask.Clone(), new MCvScalar(255,0,0), DefectType.OnPlatformContour),
                intersectPlatformArea,
                new(_roiMask, new MCvScalar(0,0,255), DefectType.OnOuterPlatform)
            };
            // Создаём фильтры
            var contourFilterByRealSize1 = new ContourFilterByRealSize(
                new Size((int)_frameSizePx.Width, (int)_frameSizePx.Height),
                new Size((int)_frameSizeMm.Width, (int)_frameSizeMm.Height),
                16.0);
            var contourFilterByRealSize2 = new ContourFilterByRealSize(
                new Size((int)_frameSizePx.Width, (int)_frameSizePx.Height),
                new Size((int)_frameSizeMm.Width, (int)_frameSizeMm.Height),
                1.0);
            var contourFilterRakelLineInDetail = new ContourFilterRakelLineInDetail(intersectDetailArea.Mask);

            var builder = new ContourClassifierBuilder();

            // Создаём классификатор контуров
            var contourClassifier = builder
                .AddAreas(intersectAreas)
                .AddContours(DefectType.RakelLine, contourRD)
                .AddFilterRule(DefectType.OnPlatform, contourFilterByRealSize1)
                .AddFilterRule(DefectType.OnOuterPlatform, contourFilterByRealSize1)
                .AddFilterRule(DefectType.OnPlatformContour, contourFilterByRealSize2)
                .AddFilterRule(DefectType.OnDetail, contourFilterByRealSize2)
                .AddFilterRule(DefectType.RakelLine, contourFilterRakelLineInDetail)
                .Build();

            // Классифицируем и фильтруем контуры
            contourClassifier.Classify(contourAR);
            contourClassifier.ApplyFilters();

            // Сохраняем изображение с контурами (опционально)
            try
            {
                if (_imageDirectory != null)
                {
                    string arImagePath = Path.Combine(_imageDirectory.FullName, $"{_lastLayer}_AR.png");
                    if (File.Exists(arImagePath))
                    {
                        using Mat imgAR = CvInvoke.Imread(arImagePath, ImreadModes.AnyColor);
                        DrawContours(imgAR, contourClassifier.GetClassifiedContours());

                        string contourImagePath = Path.Combine(_imageDirectory.FullName, $"{_lastLayer}_contour.png");
                        CvInvoke.Imwrite(contourImagePath, imgAR);
                    }
                }
            }
            catch (Exception)
            {
                // Игнорируем ошибки сохранения изображений
            }

            // Конвертируем координаты из пикселей в микроны и сохраняем в кэш
            //contourClassifier.ConvertContourPxToCordsMicron(new SizeF((float)_frameSizeMm.Width, (float)_frameSizeMm.Height)/*, this._mm2PerPx2 или другой способ передачи масштаба если нужно */);
            _cache.Add(contourClassifier.GetClassifiedContours());

            // Принудительная сборка мусора для освобождения памяти OpenCV
            GC.Collect();
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("SaveInCacheContoursProcess was cancelled.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SaveInCacheContoursProcess: {ex.Message}");
            throw; // Пробрасываем, чтобы внешний код мог обработать ошибку
        }
    }

    /// <summary>
    /// Отрисовывает контуры на изображении
    /// </summary>
    private void DrawContours(Mat dst, Dictionary<DefectType, VectorOfVectorOfPoint> classifierContoursPx)
    {
        foreach (var kvp in classifierContoursPx)
        {
            MCvScalar color = GetColorForDefectType(kvp.Key);
            // Конвертируем VectorOfVectorOfPoint в VectorOfVectorOfPointF для отрисовки
            var contoursF = MatConverterService.VectorOfPointToVectorOfPointF(kvp.Value);
            DrawProcess.DrawFoundContoursPoly(dst, contoursF, color, 1);
            contoursF.Dispose();
        }
    }

    /// <summary>
    /// Получает цвет для типа дефекта
    /// </summary>
    private static MCvScalar GetColorForDefectType(DefectType defectType)
    {
        return defectType switch
        {
            // BGR
            DefectType.OnPlatform => new MCvScalar(0, 0, 255),          // Красный
            DefectType.OnDetail => new MCvScalar(255, 0, 0),             // Синий
            DefectType.OnPlatformContour => new MCvScalar(0, 255, 0),    // Зелёный
            DefectType.OnOuterPlatform => new MCvScalar(255, 255, 0),    // Голубой
            DefectType.RakelLine => new MCvScalar(0, 255, 255),          // Жёлтый
            _ => new MCvScalar(255, 0, 255)                              // Розовый
        };
    }

    /// <summary>
    /// Получает кэшированные классифицированные контуры
    /// </summary>
    public async Task<List<Dictionary<DefectType, VectorOfVectorOfPoint>>> GetCachingClassifyContoursAsync()
    {
        if (!IsFullData() || _classifierContoursTask == null)
        {
            throw new InvalidOperationException("Missing data for classify contours");
        }

        await _classifierContoursTask;
        return _cache.Get();
    }

    /// <summary>
    /// Синхронная версия получения кэшированных контуров (для совместимости)
    /// </summary>
    public List<Dictionary<DefectType, VectorOfVectorOfPoint>> GetCachingClassifyContours()
    {
        return GetCachingClassifyContoursAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Проверяет, все ли данные для классификации получены
    /// </summary>
    public bool IsFullData()
    {
        return _detectorTaskAE != null && _detectorTaskRD != null && _detectorTaskAR != null;
    }

    /// <summary>
    /// Проверяет, завершена ли обработка
    /// </summary>
    public bool IsDone()
    {
        if (!IsFullData())
            return false;

        // Обратите внимание: IsCompleted вернёт true и для отменённых/ошибочных задач
        return _detectorTaskAE!.IsCompleted &&
               _detectorTaskAR!.IsCompleted &&
               _detectorTaskRD!.IsCompleted &&
               _classifierContoursTask!.IsCompleted;
    }

    /// <summary>
    /// Сбрасывает состояние (отменяет текущие задачи)
    /// </summary>
    public void Refresh()
    {
        // Отменяем все текущие задачи через токен
        _cancellationTokenSource?.Cancel();

        // Сбрасываем ссылки
        _classifierContoursTask = null;
        _detectorTaskAE = null;
        _detectorTaskAR = null;
        _detectorTaskRD = null;

        // Сбрасываем семафоры, чтобы разблокировать ожидающие задачи
        if (_semaphoreAE.CurrentCount == 0)
        {
            try { _semaphoreAE.Release(); } catch { }
        }
        if (_semaphoreAR.CurrentCount == 0)
        {
            try { _semaphoreAR.Release(); } catch { }
        }

        // Создаём новый CTS для следующего цикла работы
        _cancellationTokenSource = new CancellationTokenSource();
    }

    /// <summary>
    /// Освобождает ресурсы
    /// </summary>
    public void Dispose()
    {
        _semaphoreAE?.Dispose();
        _semaphoreAR?.Dispose();
        _cancellationTokenSource?.Dispose();
        GC.SuppressFinalize(this);
    }
}