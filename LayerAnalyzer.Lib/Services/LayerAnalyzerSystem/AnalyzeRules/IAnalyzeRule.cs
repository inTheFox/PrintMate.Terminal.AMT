using Emgu.CV.Util;
using LayerAnalyzer.Lib.Models.Defects;

namespace LayerAnalyzer.Lib.Services.LayerAnalyzerSystem.AnalyzeRules;

/// <summary>
/// Интерфейс для правил анализа контуров на нескольких слоях
/// </summary>
public interface IAnalyzeRule
{
    /// <summary>
    /// Получить дефекты из классифицированных контуров на нескольких слоях
    /// </summary>
    /// <param name="classifierList">
    /// Список слоёв, где каждый слой содержит словарь: DefectType -> List контуров
    /// </param>
    /// <returns>Список обнаруженных дефектов</returns>
    List<Defect> GetDefects(List<Dictionary<DefectType, VectorOfVectorOfPoint>> classifierList);

    /// <summary>
    /// Получить необходимое количество слоёв для кэширования
    /// </summary>
    /// <returns>Количество слоёв</returns>
    int GetNecessaryCountLayerForCache();
}
