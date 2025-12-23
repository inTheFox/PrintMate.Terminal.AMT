using System.Drawing;
using Emgu.CV;
using Emgu.CV.Util;

namespace LayerAnalyzer.Lib.Services.ContourFiltering;

/// <summary>
/// Фильтр контуров по реальному физическому размеру
/// </summary>
public class ContourFilterByRealSize : IContourFilter
{
    private readonly double _areaMm2InPixel;
    private readonly double _minContourAreaMm;

    public ContourFilterByRealSize(Size imgSizePx, Size imgSizeMm, double minContourAreaMm)
    {
        double ratioX = (double)imgSizeMm.Width / imgSizePx.Width;
        double ratioY = (double)imgSizeMm.Height / imgSizePx.Height;

        if (Math.Abs(ratioX - ratioY) > 0.051)
        {
            throw new ArgumentException($"incorrect size matrix: {ratioX} {ratioY}");
        }

        _areaMm2InPixel = ratioX * ratioY;
        _minContourAreaMm = minContourAreaMm;
    }

    public void FilterContours(VectorOfVectorOfPoint contours)
    {
        var filteredContours = new VectorOfVectorOfPoint();

        // Фильтруем контуры по минимальной площади
        for (int i = 0; i < contours.Size; i++)
        {
            var contour = new VectorOfPoint(contours[i].ToArray());
            double areaMm2 = CvInvoke.ContourArea(contour) * _areaMm2InPixel;

            if (areaMm2 > _minContourAreaMm)
            {
                var points = contour.ToArray();
                filteredContours.Push(new VectorOfPoint(points));
            }
        }

        // Очищаем и заполняем исходный VectorOfVectorOfPoint
        contours.Clear();
        contours.Push(filteredContours);
    }
}
