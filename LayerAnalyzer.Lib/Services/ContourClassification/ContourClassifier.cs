using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using LayerAnalyzer.Lib.Models.Contours;
using LayerAnalyzer.Lib.Models.Defects;
using LayerAnalyzer.Lib.Services.ContourFiltering;
using System.Drawing;

namespace LayerAnalyzer.Lib.Services.ContourClassification;

/// <summary>
/// Классификатор контуров на основе пересечения с предопределенными областями
/// </summary>
public class ContourClassifier : IContourClassifier
{
    private readonly List<IntersectArea> _intersectAreas;
    private readonly Dictionary<DefectType, List<IContourFilter>> _filterRules;
    private readonly Size _maskSize;
    private Dictionary<DefectType, VectorOfVectorOfPoint> _classifiedContours = new();

    public ContourClassifier()
        : this(null, null, null, null)
    {
    }

    public ContourClassifier(
        List<IntersectArea>? intersectAreas,
        Dictionary<DefectType, List<IContourFilter>>? filterRules = null,
        Size? maskSize = null,
        Dictionary<DefectType, VectorOfVectorOfPoint>? classifierContours = null)
    {
        _maskSize = maskSize ?? new Size(1280, 1024);
        _intersectAreas = intersectAreas ;//?? DefaultIntersectAreasFactory.CreateDefault(_maskSize.Width, _maskSize.Height);
        _filterRules = filterRules ?? new Dictionary<DefectType, List<IContourFilter>>();

        // Если контуры предоставлены (через Builder), используем их
        if (classifierContours != null)
        {
            _classifiedContours = classifierContours;
        }
    }

    public Dictionary<DefectType, VectorOfVectorOfPoint> Classify(VectorOfVectorOfPoint contours)
    {
        // Инициализация словаря для всех типов дефектов
        foreach (var area in _intersectAreas)
        {
            if (!_classifiedContours.ContainsKey(area.DefectType))
            {
                _classifiedContours[area.DefectType] = new VectorOfVectorOfPoint();
            }
        }

        // Классификация каждого контура
        for (int i = 0; i < contours.Size; i++)
        {
            using var contour = contours[i];
            var contourAreaPx = CvInvoke.ContourArea(contour);

            int maxIntersectPixels = 0;
            DefectType? selectedDefectType = null;

            foreach (var intersectArea in _intersectAreas)
            {
                Mat intersectAreaMask = intersectArea.GetMaskClone();
                using var foundMatrix = new Mat(intersectAreaMask.Size, DepthType.Cv8U, 1);
                foundMatrix.SetTo(new MCvScalar(0));


                // Рисуем контур на маске
                var tempContours = new VectorOfVectorOfPoint(new VectorOfPoint(contour.ToArray()));
                CvInvoke.DrawContours(foundMatrix, tempContours, -1, new MCvScalar(255), -1);

                // Побитовое И с маской области
                using var intersected = new Mat();
                CvInvoke.BitwiseAnd(foundMatrix, intersectAreaMask, intersected);
                int intersectPixels = CvInvoke.CountNonZero(intersected);

                // Если больше половины контура пересекается - сразу выбираем этот тип
                if (intersectPixels > contourAreaPx / 2.0)
                {
                    selectedDefectType = intersectArea.DefectType;
                    break;
                }

                // Иначе выбираем тип с максимальным пересечением
                if (intersectPixels > maxIntersectPixels)
                {
                    maxIntersectPixels = intersectPixels;
                    selectedDefectType = intersectArea.DefectType;
                }
            }

            if (selectedDefectType.HasValue)
            {
                _classifiedContours[selectedDefectType.Value].Push(new VectorOfPoint(contour.ToArray()));
            }
        }

        return _classifiedContours;
    }
    public void ApplyFilters()
    {
        foreach (var kvp in _filterRules)
        {
            DefectType defectType = kvp.Key;
            List<IContourFilter> filters = kvp.Value;

            if (_classifiedContours.TryGetValue(defectType, out var contoursToFilter) && contoursToFilter.Size > 0)
            {
                foreach (var filter in filters)
                {
                    // Применяем фильтр к контурам этого типа
                    filter.FilterContours(contoursToFilter);
                }
            }
        }
    }

    public Dictionary<DefectType, VectorOfVectorOfPoint> GetClassifiedContours()
    {
        return _classifiedContours;
    }

    /// <summary>
    /// Преобразует координаты контуров из пикселей в микроны
    /// Формула:
    /// 1. Умножить на ratio (пиксели → мм)
    /// 2. Вычесть frameSizeMm/2 (центр координат в середину)
    /// 3. Умножить на 1000 для X, на -1000 для Y (мм → микроны, инвертировать Y)
    /// </summary>
    /// <param name="frameSizeMm">Размер кадра в миллиметрах</param>
    public void ConvertContourPxToCordsMicron(SizeF frameSizeMm/*, double mm2PerPx2*/) // Добавляем масштабный коэффициент
    {
        // Вычисляем коэффициент: мм на пиксель (предполагаем квадратный пиксель)
        // double ratio = Math.Sqrt(mm2PerPx2); // Если mm2PerPx2 = (мм/пиксель)^2
        // Или, если передаётся размер кадра в мм и пикселях:
        double ratioX = frameSizeMm.Width / _maskSize.Width;
        double ratioY = frameSizeMm.Height / _maskSize.Height;
        // Предполагаем, что пиксель квадратный, или используем средний масштаб, или ratioX, если ширина определяющая
        double ratio = ratioX; // Или (ratioX + ratioY) / 2.0; // Или Math.Sqrt(ratioX * ratioY);

        foreach (var defectType in _classifiedContours.Keys.ToList())
        {
            var contours = _classifiedContours[defectType];
            var newContours = new VectorOfVectorOfPoint();

            for (int i = 0; i < contours.Size; i++)
            {
                var contour = contours[i];
                var points = contour.ToArray();

                // Преобразование для каждой точки
                for (int j = 0; j < points.Length; j++)
                {
                    // Шаг 1: пиксели → мм
                    double xMm = points[j].X * ratio;
                    double yMm = points[j].Y * ratio; // ratioY, если нужно точное соотношение

                    // Шаг 2: центр координат в середину
                    xMm -= frameSizeMm.Width / 2.0;
                    yMm -= frameSizeMm.Height / 2.0;

                    // Шаг 3: мм → микроны (×1000), инвертировать Y (×-1)
                    // Используем double для промежуточных вычислений, чтобы избежать переполнения
                    double xMicrons = xMm * 1000.0;
                    double yMicrons = yMm * -1000.0;

                    // Обрезаем до разумного диапазона, если нужно, и конвертируем в int
                    // Это зависит от максимального размера платформы и ожидаемых значений
                    // Например, если платформа 320 мм, то максимальное значение в мкм ~ 160 * 1000 = 160000
                    xMicrons = Math.Max(int.MinValue, Math.Min(int.MaxValue, xMicrons));
                    yMicrons = Math.Max(int.MinValue, Math.Min(int.MaxValue, yMicrons));

                    points[j] = new Point((int)xMicrons, (int)yMicrons);
                }

                newContours.Push(new VectorOfPoint(points));
            }

            contours.Dispose(); // Освобождаем старый контур
            _classifiedContours[defectType] = newContours; // Заменяем на новый
        }
    }

    /// <summary>
    /// Освобождает ресурсы
    /// </summary>
    public void Release()
    {
        foreach (var contours in _classifiedContours.Values)
        {
            contours?.Dispose();
        }

        _classifiedContours.Clear();
    }
}
