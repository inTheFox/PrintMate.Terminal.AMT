namespace LayerAnalyzer.Lib.Models;

/// <summary>
/// Тип захвата изображения
/// </summary>
public enum CaptureType
{
    /// <summary>
    /// После нанесения порошкового слоя (recoating)
    /// </summary>
    AfterRecoating,

    /// <summary>
    /// После лазерного воздействия (exposure)
    /// </summary>
    AfterExposure
}
