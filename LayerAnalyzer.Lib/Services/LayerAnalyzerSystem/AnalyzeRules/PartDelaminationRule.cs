using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using LayerAnalyzer.Lib.Models.Defects;
using LayerAnalyzer.Lib.Services.Utils.CommonOcvService;

namespace LayerAnalyzer.Lib.Services.LayerAnalyzerSystem.AnalyzeRules;

/// <summary>
/// Правило обнаружения отслаивания детали
/// </summary>
public class PartDelaminationRule : IAnalyzeRule
{
    private readonly int _observeLayersCount;
    private readonly double _minContourAreaMm2; // Порог для дефекта мм²
    private readonly double _mm2PerPx2; // Масштабный коэффициент
    private readonly Size _frameSizePx;

    public PartDelaminationRule(int observeLayersCount, double minContourAreaMm2, Size frameSizePx, double mm2PerPx2)
    {
        _observeLayersCount = observeLayersCount;
        _minContourAreaMm2 = minContourAreaMm2;
        _frameSizePx = frameSizePx;
        _mm2PerPx2 = mm2PerPx2;
    }

    public PartDelaminationRule(int observeLayersCount, double minContourAreaMm2)
        : this(observeLayersCount, minContourAreaMm2, LayerAnalyzerSystemBuilder.CalibrationSettings.FrameSizePx, LayerAnalyzerSystemBuilder.Mm2PerPx2)
    {
    }

    public List<Defect> GetDefects(List<Dictionary<DefectType, VectorOfVectorOfPoint>> classifierList)
    {
        if (classifierList.Count < _observeLayersCount)
        {
            return new List<Defect>();
        }

        List<Defect> defects = new();

        using var pattern = new Mat(_frameSizePx, DepthType.Cv8U, 1);
        pattern.SetTo(new MCvScalar(0));

        // Накапливаем контуры деталей на последних observeLayersCount слоях
        for (int i = classifierList.Count - 1; i >= classifierList.Count - _observeLayersCount; i--)
        {
            VectorOfVectorOfPoint curDetailContourList = classifierList[i][DefectType.OnDetail];

            for (int j = 0; j < curDetailContourList.Size; j++)
            {
                using VectorOfPoint contour = curDetailContourList[j];
                Point[] points = contour.ToArray();

                // Конвертируем в VectorOfVectorOfPointF для рисования
                using VectorOfVectorOfPointF contourVector = new();
                // Преобразуем VectorOfPoint в VectorOfPointF
                var pointsF = points.Select(p => new PointF(p.X, p.Y)).ToArray();
                contourVector.Push(new VectorOfPointF(pointsF));

                // Рисуем заполненный контур на паттерне
                DrawProcess.DrawFoundContoursPoly(pattern, contourVector, new MCvScalar(255), -1);
            }
        }

        using Mat roiMask = new Mat(_frameSizePx, DepthType.Cv8U, 1);
        roiMask.SetTo(new MCvScalar(255)); // Всё белое

        // Применяем маску к паттерну
        CvInvoke.BitwiseAnd(pattern, roiMask, pattern);

        // Находим контуры в результирующем паттерне
        using Mat hierarchy = new();
        using VectorOfVectorOfPoint contours = new();
        CvInvoke.FindContours(pattern, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxNone);

        // Обрабатываем найденные контуры
        for (int i = 0; i < contours.Size; i++)
        {
            using VectorOfPoint contour = contours[i];
            double areaPx2 = CvInvoke.ContourArea(contour); // Площадь в пикселях²

            // Переводим площадь в мм²
            double realAreaMm2 = areaPx2 * _mm2PerPx2;

            if (realAreaMm2 > _minContourAreaMm2) // Сравниваем с порогом в мм²
            {
                Point[] points = contour.ToArray();
                PointF[] pointsF = new PointF[points.Length];

                // Создаём VectorOfPoint и VectorOfPointF напрямую из points и pointsF.
                for (int k = 0; k < points.Length; k++)
                {
                    pointsF[k] = new PointF(points[k].X, points[k].Y);
                }

                VectorOfPoint convertedMat = new(points);
                VectorOfPointF convertedMatF = new(pointsF);

                defects.Add(new Defect(
                    DefectType.OnDetail,
                    DefectAction.Pause,
                    DefectLevel.Error,
                    convertedMat,
                    convertedMatF));
            }
        }

        return defects;
    }

    public int GetNecessaryCountLayerForCache()
    {
        return _observeLayersCount;
    }
}