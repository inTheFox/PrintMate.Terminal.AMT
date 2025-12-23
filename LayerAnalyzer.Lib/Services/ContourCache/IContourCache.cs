using Emgu.CV.Util;
using LayerAnalyzer.Lib.Models.Defects;

namespace LayerAnalyzer.Lib.Services.ContourCache;

/// <summary>
/// Интерфейс для кэширования классифицированных контуров
/// </summary>
public interface IContourCache
{
    /// <summary>
    /// Установить текущий слой
    /// </summary>
    void SetCurLayer(int curLayer);

    /// <summary>
    /// Получить номер текущего слоя
    /// </summary>
    int GetCurLayer();

    /// <summary>
    /// Получить контуры текущего слоя
    /// </summary>
    List<Dictionary<DefectType, VectorOfVectorOfPoint>> Get();

    /// <summary>
    /// Получить контуры слоёв от numLayer-size+1 до numLayer
    /// </summary>
    List<Dictionary<DefectType, VectorOfVectorOfPoint>> GetLayers(int numLayer, int size);

    /// <summary>
    /// Получить контуры слоёв в диапазоне [start, end)
    /// </summary>
    List<Dictionary<DefectType, VectorOfVectorOfPoint>> GetRange(int start, int end);

    /// <summary>
    /// Проверить, есть ли слой в кэше
    /// </summary>
    bool HasInCache(int curLayer);

    /// <summary>
    /// Добавить контуры слоя в кэш
    /// </summary>
    void Add(Dictionary<DefectType, VectorOfVectorOfPoint> contours);

    /// <summary>
    /// Очистить кэш
    /// </summary>
    void Clear();
}
