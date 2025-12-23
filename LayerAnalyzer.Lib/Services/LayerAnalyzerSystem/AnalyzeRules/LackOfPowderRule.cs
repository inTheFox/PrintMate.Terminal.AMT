using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using LayerAnalyzer.Lib.Models.Defects;
using LayerAnalyzer.Lib.Services.Utils.CommonOcvService;
using System.Drawing;

namespace LayerAnalyzer.Lib.Services.LayerAnalyzerSystem.AnalyzeRules
{
    public class LackOfPowderRule : IAnalyzeRule
    {
        private readonly int _observeLayersCount;
        private readonly double _totalAreaMm2; // Рабочая область в мм²
        private readonly double _areaFactor; // Процент (например, 0.1 для 10%)
        private readonly double _mm2PerPx2; // Масштабный коэффициент
        private readonly Size _frameSizePx;


        public LackOfPowderRule(int observeLayersCount, int percentArea, double mm2PerPx2, Size frameSizePx, SizeF frameSizeMm)
        {
            _areaFactor = percentArea / 100.0;
            _observeLayersCount = observeLayersCount;
            _mm2PerPx2 = mm2PerPx2; // Передаём масштаб
            _frameSizePx = frameSizePx;

            _totalAreaMm2 = frameSizeMm.Width * frameSizeMm.Height; ; // Вычисляем в мм²
        }
        public LackOfPowderRule(int observeLayersCount, int percentArea)
            : this(observeLayersCount, percentArea, LayerAnalyzerSystemBuilder.Mm2PerPx2,
                LayerAnalyzerSystemBuilder.CalibrationSettings.FrameSizePx, LayerAnalyzerSystemBuilder.CalibrationSettings.FrameSizeMm)
        {
        }

        public List<Defect> GetDefects(List<Dictionary<DefectType, VectorOfVectorOfPoint>> classifierList)
        {
            if (classifierList.Count < _observeLayersCount)
            {
                return new List<Defect>();
            }

            List<Defect> defects = new List<Defect>();

            using (var pattern = new Mat(_frameSizePx, DepthType.Cv8U, 1))
            {
                pattern.SetTo(new MCvScalar(0));

                for (int i = classifierList.Count - 1; i >= classifierList.Count - _observeLayersCount; i--)
                {
                    VectorOfVectorOfPoint curPlatformContourList = classifierList[i][DefectType.OnPlatformContour];

                    for (int j = 0; j < curPlatformContourList.Size; j++)
                    {
                        using var contour = curPlatformContourList[j];
                        Point[] points = contour.ToArray();

                        // Конвертируем в VectorOfVectorOfPointF для рисования
                        using var contourVector = new VectorOfVectorOfPointF();
                        // Преобразуем VectorOfPoint в VectorOfPointF
                        var pointsF = points.Select(p => new PointF(p.X, p.Y)).ToArray();
                        contourVector.Push(new VectorOfPointF(pointsF));

                        // Рисуем заполненный контур на паттерне
                        DrawProcess.DrawFoundContoursPoly(pattern, contourVector, new MCvScalar(255.0), -1);
                    }
                }

                using (var hierarchy = new Mat())
                {
                    using var contours = new VectorOfVectorOfPoint();

                    CvInvoke.FindContours(pattern, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxNone);

                    double areaPx2Sum = 0.0; // Площадь в пикселях²
                    for (int i = 0; i < contours.Size; i++)
                    {
                        using var contour = contours[i];
                        areaPx2Sum += CvInvoke.ContourArea(contour); // Возвращает пиксели^2
                    }

                    // Переводим площадь в мм²
                    double areaMm2Sum = areaPx2Sum * _mm2PerPx2;

                    // Сравниваем в мм²
                    if (areaMm2Sum / _totalAreaMm2 > _areaFactor)
                    {
                        for (int i = 0; i < contours.Size; i++)
                        {
                            using var contour = contours[i];
                            var points = contour.ToArray();

                            // Координаты в contour уже в пикселях, как и были в classifierList.
                            // Создаём VectorOfPoint и VectorOfPointF напрямую из points.

                            // Создаём PointF массив для VectorOfPointF
                            var pointsF = points.Select(p => new PointF(p.X, p.Y)).ToArray();
                            VectorOfPointF convertedMatF = new VectorOfPointF(pointsF);

                            // Создаём VectorOfPoint
                            VectorOfPoint convertedMat = new VectorOfPoint(points);

                            defects.Add(new Defect(
                                DefectType.OnPlatformContour,
                                DefectAction.InfoMessage,
                                DefectLevel.Warning,
                                convertedMat,
                                convertedMatF));
                        }
                    }
                }
            }

            return defects;
        }

        public int GetNecessaryCountLayerForCache()
        {
            return _observeLayersCount;
        }
    }
}