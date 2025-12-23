using System.Drawing;
using Emgu.CV.Util;
using LayerAnalyzer.Lib.Models.Contours;
using LayerAnalyzer.Lib.Models.Defects;
using LayerAnalyzer.Lib.Services.ContourFiltering;

namespace LayerAnalyzer.Lib.Services.ContourClassification;

/// <summary>
/// Builder для создания ContourClassifier
/// </summary>
public class ContourClassifierBuilder
{
    protected Size? _maskSize;
    protected readonly List<IntersectArea> _intersectAreas = new();
    protected readonly Dictionary<DefectType, VectorOfVectorOfPoint> _classifierContours = new();
    protected readonly Dictionary<DefectType, List<IContourFilter>> _filterRules = new();

    /// <summary>
    /// Добавить контуры для определённого типа дефекта
    /// </summary>
    public ContourClassifierBuilder AddContours(DefectType type, VectorOfVectorOfPoint contours)
    {
        if (!_classifierContours.ContainsKey(type))
        {
            _classifierContours[type] = new VectorOfVectorOfPoint();
        }

        // Копируем контуры
        _classifierContours[type].Push(contours);

        return this;
    }

    /// <summary>
    /// Добавить правило фильтрации для типа дефекта
    /// </summary>
    public ContourClassifierBuilder AddFilterRule(DefectType defectType, IContourFilter filterRule)
    {
        if (!_filterRules.ContainsKey(defectType))
        {
            _filterRules[defectType] = new List<IContourFilter>();
        }

        _filterRules[defectType].Add(filterRule);
        return this;
    }

    /// <summary>
    /// Добавить несколько правил фильтрации для типа дефекта
    /// </summary>
    public ContourClassifierBuilder AddFilterRules(DefectType defectType, List<IContourFilter> filterRules)
    {
        if (!_filterRules.ContainsKey(defectType))
        {
            _filterRules[defectType] = new List<IContourFilter>();
        }

        _filterRules[defectType].AddRange(filterRules);
        return this;
    }

    /// <summary>
    /// Добавить одну область для классификации
    /// </summary>
    public ContourClassifierBuilder AddArea(IntersectArea intersectArea)
    {
        if (_maskSize == null)
        {
            _maskSize = new Size(intersectArea.Mask.Width, intersectArea.Mask.Height);
        }

        if (!IsCorrectMaskSize(new Size(intersectArea.Mask.Width, intersectArea.Mask.Height)))
        {
            throw new ArgumentException($"Mask size mismatch. Expected {_maskSize}, got {intersectArea.Mask.Size}");
        }

        _intersectAreas.Add(intersectArea);
        return this;
    }

    /// <summary>
    /// Добавить список областей для классификации
    /// </summary>
    public ContourClassifierBuilder AddAreas(List<IntersectArea> intersectAreaList)
    {
        if (intersectAreaList == null || intersectAreaList.Count == 0)
        {
            return this;
        }

        if (_maskSize == null)
        {
            var firstArea = intersectAreaList[0];
            _maskSize = new Size(firstArea.Mask.Width, firstArea.Mask.Height);
        }

        foreach (var intersectArea in intersectAreaList)
        {
            var areaSize = new Size(intersectArea.Mask.Width, intersectArea.Mask.Height);
            if (!IsCorrectMaskSize(areaSize))
            {
                throw new ArgumentException($"Mask size mismatch. Expected {_maskSize}, got {areaSize}");
            }

            _intersectAreas.Add(intersectArea);
        }

        return this;
    }

    /// <summary>
    /// Установить размер маски
    /// </summary>
    public ContourClassifierBuilder SetMaskSize(Size maskSize)
    {
        if (_maskSize != null && !_maskSize.Equals(maskSize))
        {
            throw new ArgumentException($"Mask size already set to {_maskSize}, cannot change to {maskSize}");
        }

        _maskSize = maskSize;
        return this;
    }

    /// <summary>
    /// Проверить, что размер маски совпадает
    /// </summary>
    protected bool IsCorrectMaskSize(Size size)
    {
        return _maskSize == null || _maskSize.Equals(size);
    }

    /// <summary>
    /// Построить ContourClassifier
    /// </summary>
    public virtual ContourClassifier Build()
    {
        // Если размер маски не установлен, используем значение по умолчанию
        var maskSize = _maskSize ?? new Size(1280, 1024);

        // Создаём классификатор с собранными параметрами
        return new ContourClassifier(_intersectAreas, _filterRules, maskSize, _classifierContours);
    }
}
