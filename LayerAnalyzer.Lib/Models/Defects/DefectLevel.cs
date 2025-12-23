namespace LayerAnalyzer.Lib.Models.Defects;

/// <summary>
/// Уровень критичности дефекта
/// </summary>
public enum DefectLevel
{
    /// <summary>
    /// Информация (низкая критичность)
    /// </summary>
    Info,

    /// <summary>
    /// Предупреждение (средняя критичность)
    /// </summary>
    Warning,

    /// <summary>
    /// Ошибка (высокая критичность)
    /// </summary>
    Error
}
