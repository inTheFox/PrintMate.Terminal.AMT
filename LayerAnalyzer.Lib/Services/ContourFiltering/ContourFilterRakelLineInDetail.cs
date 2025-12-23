using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace LayerAnalyzer.Lib.Services.ContourFiltering;

/// <summary>
/// Фильтр контуров линий ракеля, пересекающихся с деталью
/// Оставляет только те контуры, которые пересекаются с маской детали
/// </summary>
public class ContourFilterRakelLineInDetail : IContourFilter
{
    private readonly Mat _detailContourMask;

    public ContourFilterRakelLineInDetail(Mat detailContourMask)
    {
        _detailContourMask = detailContourMask;
    }

    public void FilterContours(VectorOfVectorOfPoint contours)
    {
        using var contourPattern = new Mat(_detailContourMask.Size, DepthType.Cv8U, 1);
        using var intersectResult = new Mat(_detailContourMask.Size, DepthType.Cv8U, 1);
        var filteredContours = new List<VectorOfPoint>();

        // Проверяем каждый контур на пересечение с маской детали
        for (int i = 0; i < contours.Size; i++)
        {
            var contour = new VectorOfPoint(contours[i].ToArray());
            // Очищаем паттерн
            contourPattern.SetTo(new MCvScalar(0));
            // Рисуем текущий контур на паттерне
            using var contourArray = new VectorOfVectorOfPoint();
            contourArray.Push(contour);
            CvInvoke.DrawContours(contourPattern, contourArray, -1, new MCvScalar(255), (int)LineType.Filled);

            // Проверяем пересечение с маской детали
            CvInvoke.BitwiseAnd(contourPattern, _detailContourMask, intersectResult);
            int intersectPixels = CvInvoke.CountNonZero(intersectResult);

            // Если есть пересечение - сохраняем контур
            if (intersectPixels > 0)
            {
                filteredContours.Add(contour);
            }
        }

        // Очищаем и заполняем исходный VectorOfVectorOfPoint
        contours.Clear();
        foreach (var contour in filteredContours)
        {
            contours.Push(contour);
        }
    }
}
