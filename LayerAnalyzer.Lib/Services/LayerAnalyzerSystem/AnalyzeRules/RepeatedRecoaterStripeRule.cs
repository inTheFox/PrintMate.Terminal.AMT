using System.Drawing;
using Emgu.CV;
using Emgu.CV.Util;
using LayerAnalyzer.Lib.Models.Defects;
using Range = Emgu.CV.Structure.Range;

namespace LayerAnalyzer.Lib.Services.LayerAnalyzerSystem.AnalyzeRules;

/// <summary>
/// Правило обнаружения повторяющихся полос от ракеля
/// </summary>
public class RepeatedRecoaterStripeRule : IAnalyzeRule
{
    private readonly int _observeLayersCount;
    private readonly int _countLayerWithDefect;
    private readonly double[] _contourHeight = new double[2];

    public RepeatedRecoaterStripeRule(int observeLayersCount, int countLayerWithDefect)
    {
        _observeLayersCount = observeLayersCount;
        _countLayerWithDefect = countLayerWithDefect;
    }

    public List<Defect> GetDefects(List<Dictionary<DefectType, VectorOfVectorOfPoint>> classifierList)
    {
        if (classifierList.Count < _observeLayersCount)
        {
            return new List<Defect>();
        }

        FoundPatternHeight(classifierList);

        List<Range> lineRangesUnion = new();
        List<int> countLineAppears = new();

        // Обрабатываем последние observeLayersCount слоёв
        for (int i = classifierList.Count - 1; i >= classifierList.Count - _observeLayersCount; i--)
        {
            VectorOfVectorOfPoint curRakelContourList = classifierList[i][DefectType.RakelLine];
            List<Range> lastLineRanges = new();

            for (int j = 0; j < curRakelContourList.Size; j++)
            {
                using VectorOfPoint curRakelContour = curRakelContourList[j];
                lastLineRanges.Add(GetRangeFromMat(curRakelContour));
            }

            UpdateRanges(lastLineRanges, lineRangesUnion, countLineAppears);
        }

        return GetFilteredDefectList(lineRangesUnion, countLineAppears);
    }

    public int GetNecessaryCountLayerForCache()
    {
        return _observeLayersCount;
    }

    private bool IsIntersect(Range range1, Range range2)
    {
        // Проверяем пересечение: диапазоны пересекаются если start1 <= end2 && start2 <= end1
        return range1.Start <= range2.End && range2.Start <= range1.End;
    }

    private Range GetRangeFromMat(VectorOfPoint mat)
    {
        Rectangle rect = CvInvoke.BoundingRectangle(mat);
        return new Range(rect.X, rect.X + rect.Width - 1);
    }

    private void UpdateRanges(List<Range> lastLineRanges, List<Range> lineRangesUnion, List<int> countLineAppears)
    {
        SortRanges(lastLineRanges, lineRangesUnion, countLineAppears);
        UnionMultiIntersection(lastLineRanges, lineRangesUnion, countLineAppears);

        foreach (Range lastLineRange in lastLineRanges)
        {
            bool isIntersect = false;

            for (int unionLineIndex = 0; unionLineIndex < lineRangesUnion.Count; unionLineIndex++)
            {
                if (IsIntersect(lastLineRange, lineRangesUnion[unionLineIndex]))
                {
                    int min = Math.Min(lineRangesUnion[unionLineIndex].Start, lastLineRange.Start);
                    int max = Math.Max(lineRangesUnion[unionLineIndex].End, lastLineRange.End);

                    lineRangesUnion[unionLineIndex] = new Range(min, max);
                    countLineAppears[unionLineIndex] += 1;
                    isIntersect = true;

                    break;
                }
            }

            if (!isIntersect)
            {
                lineRangesUnion.Add(lastLineRange);
                countLineAppears.Add(1);
            }
        }
    }

    private void SortRanges(List<Range> lastLineRanges, List<Range> lineRangesUnion, List<int> countLineAppears)
    {
        lastLineRanges.Sort((r1, r2) => r1.Start.CompareTo(r2.Start));

        bool needIteration = true;
        while (needIteration)
        {
            needIteration = false;
            for (int i = 1; i < lineRangesUnion.Count; i++)
            {
                if (lineRangesUnion[i].Start < lineRangesUnion[i - 1].Start)
                {
                    // Swap ranges
                    (lineRangesUnion[i], lineRangesUnion[i - 1]) = (lineRangesUnion[i - 1], lineRangesUnion[i]);

                    // Swap counts
                    (countLineAppears[i], countLineAppears[i - 1]) = (countLineAppears[i - 1], countLineAppears[i]);

                    needIteration = true;
                }
            }
        }
    }

    private void UnionMultiIntersection(List<Range> lastLineRanges, List<Range> lineRangesUnion, List<int> countLineAppears)
    {
        // Объединяем множественные пересечения в lastLineRanges
        foreach (Range rangeUnion in lineRangesUnion)
        {
            int startIntersectIndex = -1;
            int endIntersectIndex = -1;

            for (int i = 0; i < lastLineRanges.Count; i++)
            {
                if (startIntersectIndex == -1)
                {
                    if (rangeUnion.Start < lastLineRanges[i].End)
                    {
                        startIntersectIndex = i;
                    }
                }
                else
                {
                    endIntersectIndex = i;
                    if (rangeUnion.End < lastLineRanges[i].Start)
                    {
                        endIntersectIndex = i - 1;
                        break;
                    }
                }
            }

            if (startIntersectIndex != -1 && endIntersectIndex != -1 && startIntersectIndex != endIntersectIndex)
            {
                Range unionRange = new(lastLineRanges[startIntersectIndex].Start, lastLineRanges[endIntersectIndex].End);
                for (int j = endIntersectIndex; j > startIntersectIndex; j--)
                {
                    lastLineRanges.RemoveAt(j);
                }
                lastLineRanges[startIntersectIndex] = unionRange;
            }
        }

        // Объединяем множественные пересечения в lineRangesUnion
        foreach (Range lastLineRange in lastLineRanges)
        {
            int startIntersectIndex = -1;
            int endIntersectIndex = -1;

            for (int i = 0; i < lineRangesUnion.Count; i++)
            {
                if (startIntersectIndex == -1)
                {
                    if (lastLineRange.Start < lineRangesUnion[i].End)
                    {
                        startIntersectIndex = i;
                    }
                }
                else
                {
                    endIntersectIndex = i;
                    if (lastLineRange.End < lineRangesUnion[i].Start)
                    {
                        endIntersectIndex = i - 1;
                        break;
                    }
                }
            }

            if (startIntersectIndex != -1 && endIntersectIndex != -1 && startIntersectIndex != endIntersectIndex)
            {
                Range unionRange = new(lineRangesUnion[startIntersectIndex].Start, lineRangesUnion[endIntersectIndex].End);
                int maxCountAppears = 0;

                for (int j = startIntersectIndex; j <= endIntersectIndex; j++)
                {
                    maxCountAppears = Math.Max(countLineAppears[j], maxCountAppears);
                }

                for (int j = endIntersectIndex; j > startIntersectIndex; j--)
                {
                    lineRangesUnion.RemoveAt(j);
                    countLineAppears.RemoveAt(j);
                }

                lineRangesUnion[startIntersectIndex] = unionRange;
                countLineAppears[startIntersectIndex] = maxCountAppears;
            }
        }
    }

    private List<Defect> GetFilteredDefectList(
        List<Range> lineRangesUnion,
        List<int> countLineAppears)
    {
        List<Defect> defects = new();

        for (int i = 0; i < lineRangesUnion.Count; i++)
        {
            if (countLineAppears[i] >= _countLayerWithDefect)
            {
                //var contourMicrons = rakelContoursInMicrons[i];
                int maxX = lineRangesUnion[i].End;
                int minX = lineRangesUnion[i].Start;

                PointF[] pointsContour = new PointF[]
                {
                    new(minX, (float)_contourHeight[0]),
                    new(maxX, (float)_contourHeight[0]),
                    new(maxX, (float)_contourHeight[1]),
                    new(minX, (float)_contourHeight[1])
                };

                // Конвертируем в Point для VectorOfPoint
                var points = pointsContour.Select(p => new Point((int)p.X, (int)p.Y)).ToArray();

                defects.Add(new Defect(
                    DefectType.RakelLine,
                    DefectAction.SkipDetail,
                    DefectLevel.Error,
                    new VectorOfPoint(points),
                    new VectorOfPointF(pointsContour)));
            }
        }

        return defects;
    }

    private void FoundPatternHeight(List<Dictionary<DefectType, VectorOfVectorOfPoint>> classifierList)
    {
        for (int i = classifierList.Count - 1; i >= classifierList.Count - _observeLayersCount; i--)
        {
            VectorOfVectorOfPoint curRakelContourList = classifierList[i][DefectType.RakelLine];

            for (int j = 0; j < curRakelContourList.Size; j++)
            {
                using VectorOfPoint mat = curRakelContourList[j];
                _contourHeight[0] = double.MaxValue;
                _contourHeight[1] = double.MinValue;

                Point[] points = mat.ToArray();
                foreach (Point point in points)
                {
                    _contourHeight[0] = Math.Min(_contourHeight[0], point.Y);
                    _contourHeight[1] = Math.Max(_contourHeight[1], point.Y);
                }

                if (_contourHeight[0] <= _contourHeight[1])
                {
                    return;
                }
            }
        }
    }
}
