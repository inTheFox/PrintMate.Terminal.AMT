using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;

namespace LayerAnalyzer.Lib.Models.ContourDetection;

/// <summary>
/// Информация о вырезанной области контура для обработки
/// </summary>
public class CropInfo
{
    private readonly Rectangle _rect;
    private readonly Mat _img;
    private readonly Mat _contourMask;

    private CropInfo(Mat img, Mat contourMask, Rectangle rect)
    {
        _rect = rect;
        _img = img;
        _contourMask = contourMask;
    }

    /// <summary>
    /// Создать CropInfo из одного контура
    /// </summary>
    public static CropInfo FromContour(Mat src, VectorOfPoint contour)
    {
        // Получаем ограничивающий прямоугольник
        var rect = CvInvoke.BoundingRectangle(contour);

        // Создаём маску контура
        using var patternFull = new Mat(src.Size, DepthType.Cv8U, 1);
        patternFull.SetTo(new MCvScalar(0));

        // Заполняем контур
        using var contoursVector = new VectorOfVectorOfPoint([contour]);
        CvInvoke.FillPoly(patternFull, contoursVector, new MCvScalar(255));

        // Вырезаем ROI
        var contourMask = new Mat(patternFull, rect).Clone();
        var img = new Mat(src, rect).Clone();

        return new CropInfo(img, contourMask, rect);
    }

    /// <summary>
    /// Создать список CropInfo из контуров
    /// </summary>
    public static List<CropInfo> FromContours(Mat src, VectorOfVectorOfPoint contours)
    {
        var cropInfoList = new List<CropInfo>();
        for (int i = 0; i < contours.Size; i++)
        {
            cropInfoList.Add(FromContour(src, contours[i]));
        }
        return cropInfoList;
    }

    /// <summary>
    /// Получить прямоугольник (копия)
    /// </summary>
    public Rectangle GetRect() => _rect;

    /// <summary>
    /// Получить изображение (копия)
    /// </summary>
    public Mat GetImg() => _img.Clone();

    /// <summary>
    /// Получить маску контура (копия)
    /// </summary>
    public Mat GetContourMask() => _contourMask.Clone();

    /// <summary>
    /// Освободить ресурсы
    /// </summary>
    public void Release()
    {
        _img?.Dispose();
        _contourMask?.Dispose();
    }
}
