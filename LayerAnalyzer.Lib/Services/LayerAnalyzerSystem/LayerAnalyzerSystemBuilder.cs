using Emgu.CV.Dnn;
using LayerAnalyzer.Lib.Services.LayerAnalyzerSystem.AnalyzeRules;
using LayerAnalyzer.Lib.Models.Calibration;
using LayerAnalyzer.Lib.Services.Utils;

namespace LayerAnalyzer.Lib.Services.LayerAnalyzerSystem;

/// <summary>
/// Базовый Builder для конфигурации LayerAnalyzerSystem
/// </summary>
public abstract class LayerAnalyzerSystemBuilder
{
    //TODO: я не знаю как убрать быдлокод
    public static double Mm2PerPx2;
    public static CalibrationSettings CalibrationSettings;

    protected int CurBufferSize = 3;
    protected readonly List<IAnalyzeRule> AnalyzeRules = new();
    protected DirectoryInfo? ImageDirectory;
    protected DirectoryInfo? ContourDirectory;
    protected string RoiMaskPath;


    public LayerAnalyzerSystemBuilder SetRoiMaskPath(string roiMaskPath)
    {
        if (string.IsNullOrEmpty(roiMaskPath))
            throw new ArgumentNullException(nameof(roiMaskPath));
        RoiMaskPath = roiMaskPath;
        return this;
    }

    /// <summary>
    /// Устанавливает калибровку
    /// </summary>
    /// <param name="filename"></param>
    /// <returns></returns>
    public LayerAnalyzerSystemBuilder SetCalibrationSettingsFile(string filename)
    {
        if (!File.Exists(filename))
        {
            throw new FileNotFoundException($"Calibration settings file not found: {filename}");
        }

        CalibrationSettings = new CalibrationSettingsLoader().Load(filename);
        var areaPx2 = CalibrationSettings.FrameSizePx.Height * CalibrationSettings.FrameSizePx.Width;
        var areaMm2 = CalibrationSettings.FrameSizeMm.Height * CalibrationSettings.FrameSizeMm.Width;
        Mm2PerPx2 = areaMm2 / areaPx2;

        return this;
    }

    /// <summary>
    /// Добавляет правило анализа
    /// </summary>
    public LayerAnalyzerSystemBuilder AddAnalyzeRule(IAnalyzeRule analyzeRule)
    {
        if (CalibrationSettings == null)
        {
            throw new ArgumentNullException($"сначала внедрите калибровочный файл {nameof(CalibrationSettings)}");
        }
        if (analyzeRule == null)
        {
            throw new ArgumentNullException(nameof(analyzeRule));
        }

        // Увеличиваем размер буфера, если правило требует больше слоёв
        int necessaryCount = analyzeRule.GetNecessaryCountLayerForCache();
        if (necessaryCount > CurBufferSize)
        {
            CurBufferSize = necessaryCount;
        }

        AnalyzeRules.Add(analyzeRule);
        return this;
    }

    /// <summary>
    /// Устанавливает директорию для сохранения контуров
    /// </summary>
    public LayerAnalyzerSystemBuilder SaveContourInDirectory(string directoryPath)
    {
        DirectoryInfo directory = new(directoryPath);

        if (!directory.Exists)
        {
            directory.Create();
        }

        ContourDirectory = directory;
        return this;
    }

    /// <summary>
    /// Устанавливает директорию для сохранения изображений
    /// </summary>
    public LayerAnalyzerSystemBuilder SaveImageInDirectory(string directoryPath)
    {
        DirectoryInfo directory = new(directoryPath);

        if (!directory.Exists)
        {
            directory.Create();
        }

        ImageDirectory = directory;
        return this;
    }

    /// <summary>
    /// Создаёт экземпляр LayerAnalyzerSystem
    /// </summary>
    public abstract LayerAnalyzerSystemService Build();
}
