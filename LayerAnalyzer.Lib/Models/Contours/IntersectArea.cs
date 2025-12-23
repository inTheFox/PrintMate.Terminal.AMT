using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using LayerAnalyzer.Lib.Models.Defects;
using System.Drawing;
using LayerAnalyzer.Lib.Services.Utils.CommonOcvService;

namespace LayerAnalyzer.Lib.Models.Contours;

/// <summary>
/// Область для классификации контуров по пересечению
/// </summary>
public class IntersectArea : IDisposable
{
    private Mat? _mask;
    private bool _disposed;

    /// <summary>
    /// Тип дефекта для этой области
    /// </summary>
    public DefectType DefectType { get; set; }

    /// <summary>
    /// Маска области (бинарное изображение)
    /// </summary>
    public Mat Mask
    {
        get => _mask ?? throw new InvalidOperationException("Mask not initialized");
        set
        {
            _mask?.Dispose();
            _mask = value;
        }
    }

    /// <summary>
    /// Конструктор, который создаёт маску на основе цветной маски и выбранного цвета
    /// </summary>
    public IntersectArea(Mat colorMask, MCvScalar selectedColor, DefectType defectType)
    {
        // Создаём бинарную маску: пиксели, равные selectedColor, становятся 255, остальные 0
        var mask = new Mat();
        CvInvoke.InRange(colorMask, new ScalarArray(selectedColor), new ScalarArray(selectedColor), mask);
        DefectType = defectType;
        _mask = mask;
    }

    /// <summary>
    /// Создать IntersectArea из контуров
    /// </summary>
    public static IntersectArea FromContour(VectorOfVectorOfPoint contours, Size maskSize, DefectType defectType)
    {
        var mask = new Mat(maskSize, DepthType.Cv8U, 1);
        mask.SetTo(new MCvScalar(0));

        // Рисуем все контуры белым цветом
        var contoursF = MatConverterService.VectorOfPointToVectorOfPointF(contours);
        DrawProcess.DrawFoundContoursPoly(mask, contoursF, new MCvScalar(255), -1);
        // Создаём новый IntersectArea, передавая маску и цвет 255
        return new IntersectArea(mask, new MCvScalar(255), defectType);
    }

    /// <summary>
    /// Вычесть другую область из текущей
    /// </summary>
    public void ExcludeArea(IntersectArea excludedArea)
    {
        if (_mask == null)
            throw new InvalidOperationException("Mask not initialized");

        // Получаем маску другой области
        using var excludedMask = excludedArea.GetMaskClone();

        // Выполняем вычитание
        CvInvoke.Subtract(_mask, excludedMask, _mask);
    }

    /// <summary>
    /// Получить клон маски
    /// </summary>
    public Mat GetMaskClone()
    {
        return Mask.Clone();
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _mask?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}