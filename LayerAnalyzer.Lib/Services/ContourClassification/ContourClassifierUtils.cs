using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using LayerAnalyzer.Lib.Models.Contours;

namespace LayerAnalyzer.Lib.Services.ContourClassification;

/// <summary>
/// Утилиты для классификации контуров
/// </summary>
public static class ContourClassifierUtils
{
    /// <summary>
    /// Получить контуры, которые находятся в заданной области
    /// (более 50% контура пересекается с областью)
    /// </summary>
    public static VectorOfVectorOfPoint GetContoursInArea(IntersectArea area, VectorOfVectorOfPoint contours)
    {
        var contoursInArea = new VectorOfVectorOfPoint();
        using var neededAreaMask = area.GetMaskClone();

        using var foundMatrix = new Mat(neededAreaMask.Size, DepthType.Cv8U, 1);

        for (int i = 0; i < contours.Size; i++)
        {
            using var contour = contours[i];

            // Очищаем матрицу
            foundMatrix.SetTo(new MCvScalar(0));

            // Рисуем контур
            var tempContours = new VectorOfVectorOfPoint(new VectorOfPoint(contour.ToArray()));
            CvInvoke.DrawContours(foundMatrix, tempContours, -1, new MCvScalar(255), -1);

            // Вычисляем площадь контура
            double contourAreaPx = CvInvoke.ContourArea(contour);

            // Побитовое И с маской области
            CvInvoke.BitwiseAnd(foundMatrix, neededAreaMask, foundMatrix);
            int intersectPixels = CvInvoke.CountNonZero(foundMatrix);

            // Если больше половины контура в области - добавляем
            if (intersectPixels > contourAreaPx / 2.0)
            {
                contoursInArea.Push(new VectorOfPoint(contour.ToArray()));
            }
        }

        return contoursInArea;
    }
}
