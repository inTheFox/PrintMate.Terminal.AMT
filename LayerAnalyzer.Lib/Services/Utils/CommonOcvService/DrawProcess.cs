using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace LayerAnalyzer.Lib.Services.Utils.CommonOcvService;

/// <summary>
/// Утилиты для отрисовки контуров на изображениях
/// </summary>
public static class DrawProcess
{
    /// <summary>
    /// Отрисовывает контуры из списка VectorOfPointF на изображении с указанной толщиной
    /// </summary>
    /// <param name="img">Изображение для отрисовки</param>
    /// <param name="contoursPoly">Список контуров (VectorOfPointF)</param>
    /// <param name="color">Цвет контуров</param>
    /// <param name="thickness">Толщина линии (или -1 для заливки)</param>
    public static void DrawFoundContoursPoly(Mat img, VectorOfVectorOfPointF contoursPoly, MCvScalar color, int thickness)
    {
        // Конвертируем VectorOfPointF в VectorOfPoint
        using VectorOfVectorOfPoint contours = MatConverterService.VectorOfPointFToVectorOfPoint(contoursPoly);
        CvInvoke.DrawContours(img, contours, -1, color, thickness, LineType.AntiAlias);
    }
}
