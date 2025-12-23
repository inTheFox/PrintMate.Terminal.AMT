using System.Drawing;
using System.Drawing.Drawing2D;
using Emgu.CV.Util;
using LayerAnalyzer.Lib.Services.Utils.CommonOcvService;

namespace LayerAnalyzer.Lib.Models.Defects;

/// <summary>
/// Представляет обнаруженный дефект в порошковом слое
/// </summary>
public class Defect
{
    /// <summary>
    /// Тип дефекта
    /// </summary>
    public DefectType Type { get; set; }

    /// <summary>
    /// Действие при обнаружении дефекта
    /// </summary>
    public DefectAction Action { get; set; }

    /// <summary>
    /// Уровень критичности дефекта
    /// </summary>
    public DefectLevel Level { get; set; }

    /// <summary>
    /// Номер слоя, на котором обнаружен дефект
    /// </summary>
    public int LayerNumber { get; set; }

    /// <summary>
    /// Контур дефекта (VectorOfPoint в микронах относительно центра)
    /// </summary>
    public VectorOfPoint? ContourMicrons { get; set; }
    public GraphicsPath ContourMicron { get; }
    /// <summary>
    /// Контур дефекта (список точек в микронах относительно центра)
    /// Legacy - для обратной совместимости
    /// </summary>
    public List<Point> Contour { get; set; } = new();

    /// <summary>
    /// Площадь дефекта в квадратных микронах
    /// </summary>
    public double AreaMicrons { get; set; }

    /// <summary>
    /// Центр дефекта в микронах
    /// </summary>
    public Point CenterMicrons { get; set; }

    /// <summary>
    /// Дополнительная информация о дефекте
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Временная метка обнаружения
    /// </summary>
    public DateTime DetectedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Конструктор с основными параметрами (как в Java)
    /// </summary>
    public Defect(DefectType type, DefectAction action, DefectLevel level, VectorOfPoint contourMicrons, VectorOfPointF contourMm)
    {
        Type = type;
        Action = action;
        Level = level;
        ContourMicrons = contourMicrons;
        ContourMicron = MatConverterService.ConvertMat2Path(contourMm);
    }

    /// <summary>
    /// Конструктор по умолчанию
    /// </summary>
    public Defect()
    {
    }

    public override string ToString()
    {
        return $"{Type} ({Level} - {Action}): площадь {CalculateArea(ContourMicron)} мм^2";
    }
    /// <summary>
    /// Вычисляет площадь GraphicsPath, аппроксимируя его как полигон.
    /// </summary>
    /// <param name="path">Входной GraphicsPath.</param>
    /// <returns>Площадь в квадратных пикселях.</returns>
    public static float CalculateArea(GraphicsPath path)
    {
        if (path.PointCount < 3)
        {
            // Путь не замкнут или не является полигоном, площадь 0
            return 0.0f;
        }

        // Создаём копию пути, чтобы не изменять оригинал
        using var clonedPath = (GraphicsPath)path.Clone();

        // Преобразуем кривые в линии (аппроксимируем)
        // flatness - это максимальное отклонение от истинной кривой. Меньше значение - точнее, но больше точек.
        clonedPath.Flatten(new Matrix(), 0.25f); // flatness = 0.25f - часто используемое значение

        // Получаем точки после Flatten
        PointF[] points = clonedPath.PathPoints;

        if (points.Length < 3)
        {
            return 0.0f;
        }

        // Вычисляем площадь с помощью формулы шнуровки
        float area = 0.0f;
        int j = points.Length - 1; // Индекс последней точки

        for (int i = 0; i < points.Length; i++)
        {
            area += (points[j].X + points[i].X) * (points[j].Y - points[i].Y);
            j = i; // j - это предыдущая точка относительно i
        }

        // Берём абсолютное значение и делим на 2
        return Math.Abs(area) / 2.0f;
    }
}
