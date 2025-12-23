using Emgu.CV.Util;
using LayerAnalyzer.Lib.Models.Defects;

namespace LayerAnalyzer.Lib.Services.ContourClassification;

/// <summary>
/// Интерфейс для классификации контуров по типам дефектов
/// </summary>
public interface IContourClassifier
{
    /// <summary>
    /// Классифицировать контуры
    /// </summary>
    /// <param name="contours">Входные контуры</param>
    /// <returns>Словарь контуров, сгруппированных по типам дефектов</returns>
    Dictionary<DefectType, VectorOfVectorOfPoint> Classify(VectorOfVectorOfPoint contours);

    /// <summary>
    /// Применить фильтры к классифицированным контурам
    /// </summary>
    void ApplyFilters();

    /// <summary>
    /// Получить классифицированные контуры
    /// </summary>
    Dictionary<DefectType, VectorOfVectorOfPoint> GetClassifiedContours();
}
