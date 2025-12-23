using System.Drawing;
using System.Drawing.Drawing2D;
using Emgu.CV.Util;
using LayerAnalyzer.Lib.Models.Defects;
using LayerAnalyzer.Lib.Services.LayerAnalyzerSystem.AnalyzeRules;

namespace LayerAnalyzer.Lib.Services.LayerAnalyzerSystem
{

    /// <summary>
    /// Главный анализатор - запускает правила и фильтрует результаты
    /// </summary>
    public class LayerAnalyzer
    {
        private readonly List<IAnalyzeRule> _analyzeRules = [];
        private readonly List<GraphicsPath> _skippedContours = [];

        /// <summary>
        /// Анализирует контуры и возвращает найденные дефекты
        /// </summary>
        public List<Defect> Analyze(List<Dictionary<DefectType, VectorOfVectorOfPoint>> cacheList)
        {
            List<Defect> defects = [];

            // Запускаем все правила анализа
            foreach (var analyzeRule in _analyzeRules)
            {
                defects.AddRange(analyzeRule.GetDefects(cacheList));
            }

            // Фильтруем дефекты по пропускаемым контурам
            return Filtered(defects);
        }

        /// <summary>
        /// Добавляет дефект в список пропускаемых (чтобы игнорировать его в будущем)
        /// </summary>
        public void AddSkipDefect(Defect skippedDefect)
        {
            _skippedContours.Add(skippedDefect.ContourMicron);
            //if (skippedDefect.ContourMicrons != null)
            //{
            //    // Конвертируем VectorOfPoint в GraphicsPath
            //    GraphicsPath path = MatConverterService.ConvertMat2Path(
            //        MatConverterService.VectorOfPointToVectorOfPointF(skippedDefect.ContourMicrons));
            //    _skippedContours.Add(path);
            //}
        }

        /// <summary>
        /// Добавляет контур в список пропускаемых
        /// </summary>
        public void AddSkipContour(GraphicsPath skippedContour)
        {
            _skippedContours.Add(skippedContour);
        }

        /// <summary>
        /// Получает список пропускаемых контуров
        /// </summary>
        public List<GraphicsPath> GetSkippedContours()
        {
            return [.._skippedContours];
        }

        /// <summary>
        /// Фильтрует дефекты - убирает те, которые пересекаются с пропускаемыми контурами
        /// </summary>
        public List<Defect> Filtered(List<Defect> defects)
        {
            if (_skippedContours.Count == 0)
            {
                return defects;
            }

            List<Defect> filteredDefects = [];

            foreach (var defect in defects)
            {
                var defectArea = new Region(defect.ContourMicron);
                var isIntersected = false;

                foreach (var skippedContour in _skippedContours)
                {
                    var skippedDefectArea = new Region(skippedContour);
                    skippedDefectArea.Intersect(defectArea);
                    if (!skippedDefectArea.IsEmpty(Graphics.FromImage(new Bitmap(1, 1))))
                    {
                        isIntersected = true;
                        break;
                    }
                }

                if (!isIntersected)
                {
                    filteredDefects.Add(defect);
                }
            }

            return filteredDefects;

            //foreach (var defect in defects)
            //{
            //    if (defect.ContourMicrons == null)
            //    {
            //        filteredDefects.Add(defect);
            //        continue;
            //    }

            //    // Создаём Region из контура дефекта
            //    using GraphicsPath defectPath = MatConverterService.ConvertMat2Path(
            //        MatConverterService.VectorOfPointToVectorOfPointF(defect.ContourMicrons));
            //    using Region defectRegion = new(defectPath);

            //    bool isIntersected = false;

            //    // Проверяем пересечение со всеми пропускаемыми контурами
            //    foreach (var skippedContour in _skippedContours)
            //    {
            //        using Region skippedRegion = new(skippedContour);

            //        // Создаём копию для пересечения
            //        using Region intersectRegion = skippedRegion.Clone();
            //        intersectRegion.Intersect(defectRegion);

            //        // Проверяем, есть ли пересечение
            //        if (!intersectRegion.IsEmpty(Graphics.FromImage(new Bitmap(1, 1))))
            //        {
            //            isIntersected = true;
            //            break;
            //        }
            //    }

            //    if (!isIntersected)
            //    {
            //        filteredDefects.Add(defect);
            //    }
            //}

            //return filteredDefects;
        }

        /// <summary>
        /// Добавляет правило анализа
        /// </summary>
        public void AddAnalyzeRule(IAnalyzeRule analyzeRule)
        {
            _analyzeRules.Add(analyzeRule);
        }

        /// <summary>
        /// Получает все правила анализа
        /// </summary>
        public IReadOnlyList<IAnalyzeRule> GetAnalyzeRules()
        {
            return _analyzeRules.AsReadOnly();
        }
    }
}
