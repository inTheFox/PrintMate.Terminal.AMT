namespace ProjectParserTest.Parsers.Shared.Enums;

/// <summary>
/// Регионы геометрии (типы элементов детали)
/// </summary>
public enum GeometryRegion
{
    /// <summary>Заполнение (infill)</summary>
    Infill,

    /// <summary>Заполнение поддержки</summary>
    SupportFill,

    /// <summary>Контур поддержки</summary>
    Support,

    /// <summary>Контур детали</summary>
    Contour,

    /// <summary>Контур верхней поверхности (upskin)</summary>
    ContourUpskin,

    /// <summary>Контур нижней поверхности (downskin)</summary>
    ContourDownskin,

    /// <summary>Верхняя поверхность (upskin fill)</summary>
    Upskin,

    /// <summary>Нижняя поверхность (downskin fill)</summary>
    Downskin,

    /// <summary>Края детали</summary>
    Edges,

    /// <summary>Пустой регион</summary>
    None,

    /// <summary>Предпросмотр региона верхней поверхности</summary>
    UpskinRegionPreview,

    /// <summary>Предпросмотр региона нижней поверхности</summary>
    DownskinRegionPreview,

    /// <summary>Предпросмотр региона заполнения</summary>
    InfillRegionPreview
}