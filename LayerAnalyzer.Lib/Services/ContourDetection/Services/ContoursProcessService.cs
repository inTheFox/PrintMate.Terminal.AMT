using Emgu.CV;
using Emgu.CV.Util;

namespace LayerAnalyzer.Lib.Services.ContourDetection.Services;

/// <summary>
/// Сервис для обработки контуров (сортировка, аппроксимация)
/// </summary>
public static class ContoursProcessService
{
    /// <summary>
    /// Получить площади контуров
    /// </summary>
    public static List<double> GetContoursArea(VectorOfVectorOfPoint contours)
    {
        var contoursArea = new List<double>();
        for (int i = 0; i < contours.Size; i++)
        {
            var contour = new VectorOfPoint(contours[i].ToArray());
            contoursArea.Add(CvInvoke.ContourArea(contour));
        }
        return contoursArea;
    }

    /// <summary>
    /// Аппроксимировать контуры полигонами
    /// </summary>
    public static VectorOfVectorOfPoint GetApproxContoursPoly(VectorOfVectorOfPoint contours, double approxEpsilon)
    {
        var contoursPoly = new VectorOfVectorOfPoint();

        for (int i = 0; i < contours.Size; i++)
        {
            var currentContour = contours[i];
            if (currentContour.Size == 0)
            {
                continue;
            }

            using var approxContourPoly = new VectorOfPoint(currentContour.ToArray());

            CvInvoke.ApproxPolyDP(approxContourPoly, approxContourPoly, approxEpsilon, true);
            contoursPoly.Push(approxContourPoly);
        }

        return contoursPoly;
    }

    /// <summary>
    /// Сортировать контуры по площади (от большего к меньшему)
    /// </summary>
    public static void SortContoursByArea(VectorOfVectorOfPoint contours)
    {
        if (contours.Size <= 1) return;

        // Конвертируем в List для сортировки
        var contoursList = new List<VectorOfPoint>();
        for (int i = 0; i < contours.Size; i++)
        {
            // Создаём копию контура, чтобы избежать проблем с памятью
            var contourCopy = new VectorOfPoint(contours[i].ToArray());
            contoursList.Add(contourCopy);
        }

        // Сортируем по убыванию площади
        contoursList.Sort((a, b) =>
        {
            double areaA = CvInvoke.ContourArea(a);
            double areaB = CvInvoke.ContourArea(b);
            return areaB.CompareTo(areaA); // По убыванию
        });

        // Очищаем и заполняем VectorOfVectorOfPoint
        contours.Clear();
        foreach (var contour in contoursList)
        {
            contours.Push(contour);
        }
    }
}
