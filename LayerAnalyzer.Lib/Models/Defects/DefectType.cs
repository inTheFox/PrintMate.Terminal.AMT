namespace LayerAnalyzer.Lib.Models.Defects;

/// <summary>
/// Типы дефектов, обнаруживаемых в порошковом слое
/// </summary>
public enum DefectType
{
    /// <summary>
    /// Дефект на детали
    /// </summary>
    OnDetail,

    /// <summary>
    /// Дефект на платформе
    /// </summary>
    OnPlatform,

    /// <summary>
    /// Дефект на контуре платформы
    /// </summary>
    OnPlatformContour,

    /// <summary>
    /// Дефект вне платформы
    /// </summary>
    OnOuterPlatform,

    /// <summary>
    /// Линия ракеля (лезвия для разравнивания порошка)
    /// </summary>
    RakelLine,

    /// <summary>
    /// Неинтересный дефект (для фильтрации)
    /// </summary>
    NotInterest
}
