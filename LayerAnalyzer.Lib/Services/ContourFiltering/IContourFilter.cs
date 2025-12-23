using Emgu.CV.Util;

namespace LayerAnalyzer.Lib.Services.ContourFiltering;

/// <summary>
/// Интерфейс для фильтрации контуров
/// Изменяет список контуров in-place (как в Java реализации)
/// </summary>
public interface IContourFilter
{
    /// <summary>
    /// Отфильтровать контуры (изменяет входной VectorOfVectorOfPoint)
    /// </summary>
    /// <param name="contours">Входные/выходные контуры</param>
    void FilterContours(VectorOfVectorOfPoint contours);
}
