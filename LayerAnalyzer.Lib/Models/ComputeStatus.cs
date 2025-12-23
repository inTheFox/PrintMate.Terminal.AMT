namespace LayerAnalyzer.Lib.Models;

/// <summary>
/// Статус вычислений анализа
/// </summary>
public enum ComputeStatus
{
    /// <summary>
    /// Не хватает данных для анализа
    /// </summary>
    MissingData,

    /// <summary>
    /// Анализ выполняется
    /// </summary>
    Running,

    /// <summary>
    /// Анализ завершён
    /// </summary>
    Done
}
