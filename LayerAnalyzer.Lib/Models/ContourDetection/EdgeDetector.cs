using Emgu.CV.XImgproc;

namespace LayerAnalyzer.Lib.Models.ContourDetection;

/// <summary>
/// Singleton для StructuredEdgeDetection
/// Требует файл model.yml для работы
/// </summary>
public class EdgeDetector
{
    private static readonly Lazy<EdgeDetector> _instance = new Lazy<EdgeDetector>(() => new EdgeDetector());
    private StructuredEdgeDetection? _structEdgeDetector;

    private EdgeDetector()
    {
        // Пытаемся загрузить model.yml
        TryLoadModel();
    }

    public static EdgeDetector Instance => _instance.Value;

    public StructuredEdgeDetection? GetStructEdgeDetector() => _structEdgeDetector;

    public bool IsModelLoaded => _structEdgeDetector != null;

    private void TryLoadModel()
    {
        try
        {
            // Ищем model.yml в нескольких возможных расположениях
            var possiblePaths = new[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model.yml"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "model", "model.yml"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "newmodel", "model.yml"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "LayerAnalyzer", "model.yml"),
                "model.yml"
            };

            string? modelPath = null;
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    modelPath = path;
                    break;
                }
            }

            if (modelPath != null)
            {
                _structEdgeDetector = new StructuredEdgeDetection(modelPath, new RFFeatureGetter());
            }
            else
            {
                // Model не найдена - будем использовать альтернативные методы (Canny, Sobel)
                Console.WriteLine("Warning: model.yml not found. StructuredEdgeDetection will not be available.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading StructuredEdgeDetection model: {ex.Message}");
            _structEdgeDetector = null;
        }
    }

    /// <summary>
    /// Загрузить модель из конкретного пути
    /// </summary>
    public bool LoadModel(string modelPath)
    {
        try
        {
            _structEdgeDetector?.Dispose();
            _structEdgeDetector = new StructuredEdgeDetection(modelPath, new RFFeatureGetter());
            return true;
        }
        catch
        {
            _structEdgeDetector = null;
            return false;
        }
    }
}
