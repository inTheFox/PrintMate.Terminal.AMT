using Emgu.CV;
using Emgu.CV.Util;
using LayerAnalyzer.Lib.Models.Defects;
using System.Drawing;

namespace LayerAnalyzer.Lib.Services.LayerAnalyzerSystem.AnalyzeRules
{
    /// <summary>
    /// Правило обнаружения аномалий на платформе.
    /// Ищет посторонние объекты, превышающие порог площади.
    /// При первом обнаружении - предупреждение.
    /// Если объект наблюдается в N слоях подряд - ошибка.
    /// </summary>
    public class PlatformAnomalyRule : IAnalyzeRule
    {
        private readonly int _observeLayersCount; // N слоёв для срабатывания Error
        private readonly double _minContourAreaMm2; // Минимальная площадь контура (в мм²) для начала отслеживания
        private readonly double _mm2PerPx2; // Масштабный коэффициент

        // Состояние: отслеживаемые аномалии (их контуры из первого слоя и счётчик последовательных слоёв)
        private readonly Dictionary<string, AnomalyTracker> _trackedAnomalies = new(); // Ключ - хеш/описание контура
        private int _lastProcessedLayer = -1; // Для сброса счётчиков при "пропуске" слоёв

        public PlatformAnomalyRule(int observeLayersCount, double minContourAreaMm2, double mm2PerPx2)
        {
            if (observeLayersCount <= 0)
                throw new ArgumentException("observeLayersCount must be greater than 0.", nameof(observeLayersCount));
            if (minContourAreaMm2 <= 0)
                throw new ArgumentException("minContourAreaMm2 must be greater than 0.", nameof(minContourAreaMm2));

            _observeLayersCount = observeLayersCount;
            _minContourAreaMm2 = minContourAreaMm2;
            _mm2PerPx2 = mm2PerPx2;
        }
        public PlatformAnomalyRule(int observeLayersCount, double minContourAreaMm2)
            : this(observeLayersCount, minContourAreaMm2, LayerAnalyzerSystemBuilder.Mm2PerPx2)
        {
        }


        public List<Defect> GetDefects(List<Dictionary<DefectType, VectorOfVectorOfPoint>> classifierList)
        {
            var defects = new List<Defect>();

            if (classifierList.Count < _observeLayersCount)
            {
                return defects;
            }

            // Получаем OnPlatform контуры из последнего слоя
            var lastLayerData = classifierList.Last();
            if (!lastLayerData.ContainsKey(DefectType.OnPlatform) || lastLayerData[DefectType.OnPlatform].Size == 0)
            {
                // Если в последнем слое нет OnPlatform, сбрасываем счётчики текущих аномалий
                foreach (var tracker in _trackedAnomalies.Values)
                {
                    tracker.ResetCounter();
                }
                return defects;
            }

            var currentLayerContours = lastLayerData[DefectType.OnPlatform];
            var currentLayerAnomalies = new HashSet<string>(); // Ключи аномалий, найденных в текущем слое

            for (int i = 0; i < currentLayerContours.Size; i++)
            {
                using var contour = currentLayerContours[i];
                double areaPx2 = CvInvoke.ContourArea(contour);

                // Пересчитываем площадь в мм²
                double areaMm2 = areaPx2 * _mm2PerPx2;

                if (areaMm2 >= _minContourAreaMm2)
                {
                    var contourClone = new VectorOfPoint(contour.ToArray());
                    // Потенциальная аномалия
                    string anomalyKey = GenerateContourKey(contour); // Уникальный ключ для контура (например, центр масс и площадь)
                    currentLayerAnomalies.Add(anomalyKey);

                    if (_trackedAnomalies.ContainsKey(anomalyKey))
                    {
                        // Аномалия уже отслеживается
                        var tracker = _trackedAnomalies[anomalyKey];
                        tracker.IncrementCounter();

                        // Проверяем, достиг ли счётчик порога
                        if (tracker.Counter >= _observeLayersCount)
                        {
                            // Срабатывание: N слоёв подряд
                            defects.Add(new Defect(
                                DefectType.OnPlatform,
                                DefectAction.Pause, // Ошибка
                                DefectLevel.Error,  // Уровень
                                contourClone,       // Контур (клонируем)
                                new VectorOfPointF(
                                    contour.ToArray().Select(p => new PointF(p.X, p.Y)).ToArray())
                            ));
                            _trackedAnomalies.Remove(anomalyKey); // Удаляем после срабатывания
                        }
                    }
                    else
                    {
                        // Новая потенциальная аномалия, начинаем отслеживание
                        _trackedAnomalies[anomalyKey] = new AnomalyTracker(contourClone); // Клонируем контур для хранения

                        // Создаём предупреждение при первом обнаружении
                        defects.Add(new Defect(
                            DefectType.OnPlatform,
                            DefectAction.InfoMessage, // Предупреждение
                            DefectLevel.Warning,      // Уровень
                            contourClone,             // Контур
                            new VectorOfPointF(
                                contour.ToArray().Select(p => new PointF(p.X, p.Y)).ToArray())
                        ));
                    }
                }
            }

            // Сброс счётчиков для аномалий, не найденных в текущем слое
            foreach (var key in _trackedAnomalies.Keys.ToList())
            {
                if (!currentLayerAnomalies.Contains(key))
                {
                    _trackedAnomalies[key].ResetCounter();
                }
            }

            // Удаляем аномалии, счётчик которых сброшен до 0
            var keysToRemove = _trackedAnomalies.Where(kvp => kvp.Value.Counter == 0).Select(kvp => kvp.Key).ToList();
            foreach (var key in keysToRemove)
            {
                _trackedAnomalies.Remove(key);
            }

            return defects;
        }

        public int GetNecessaryCountLayerForCache()
        {
            return _observeLayersCount;
        }

        // --- Вспомогательные классы и методы ---

        /// <summary>
        /// Хранит информацию об отслеживаемой аномалии
        /// </summary>
        private class AnomalyTracker
        {
            public VectorOfPoint StoredContour { get; }
            public int Counter { get; private set; } = 1;

            public AnomalyTracker(VectorOfPoint storedContour)
            {
                StoredContour = storedContour;
            }

            public void IncrementCounter()
            {
                Counter++;
            }

            public void ResetCounter()
            {
                Counter = 0;
            }
        }

        /// <summary>
        /// Генерирует уникальный ключ для контура (например, округлённый центр масс и площадь).
        /// Это упрощённый способ сопоставления контуров между слоями.
        /// Более точное сопоставление возможно через IoU (Intersection over Union) или другие методы.
        /// </summary>
        private static string GenerateContourKey(VectorOfPoint contour)
        {
            var points = contour.ToArray();
            if (points.Length == 0) return "empty";

            // Центр масс
            double cx = 0, cy = 0;
            foreach (var pt in points)
            {
                cx += pt.X;
                cy += pt.Y;
            }
            cx /= points.Length;
            cy /= points.Length;

            // Площадь (в пикселях²)
            double area = CvInvoke.ContourArea(contour);

            // Округляем для устойчивости к небольшим изменениям
            int roundedCx = (int)Math.Round(cx);
            int roundedCy = (int)Math.Round(cy);
            double roundedArea = Math.Round(area, 2);

            return $"{roundedCx}_{roundedCy}_{roundedArea}";
        }
    }
}