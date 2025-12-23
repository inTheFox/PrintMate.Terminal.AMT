namespace LayerAnalyzer.Lib.Models.Defects;

/// <summary>
/// Действия, которые должны быть выполнены при обнаружении дефекта
/// </summary>
public enum DefectAction
{
    /// <summary>
    /// Информационное сообщение (только уведомление)
    /// </summary>
    InfoMessage,

    /// <summary>
    /// Пауза процесса печати
    /// </summary>
    Pause,

    /// <summary>
    /// Пропустить деталь (продолжить без детали)
    /// </summary>
    SkipDetail
}
