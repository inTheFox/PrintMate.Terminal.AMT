using Emgu.CV;
using Emgu.CV.Util;

namespace LayerAnalyzer.Lib.Services.ContourDetection;

/// <summary>
/// Интерфейс для обнаружения контуров на изображении
/// </summary>
public interface IContourDetector
{
    /// <summary>
    /// Получить контуры с изображения
    /// </summary>
    /// <param name="image">Входное изображение</param>
    /// <returns>Список обнаруженных контуров</returns>
    VectorOfVectorOfPoint GetContours(Mat image);
}
