using System;
using System.Collections.Generic;
using System.Linq;

namespace HansDebuggerApp.Hans
{
    /// <summary>
    /// Точная калибровка диаметра пучка на основе реальных измерений
    /// Использует табличные данные Z-Diameter с интерполяцией
    /// </summary>
    public class BeamDiameterCalibration
    {
        /// <summary>
        /// Калибровочная таблица: Z (mm) → Диаметр (μm)
        /// Реальные измерения от -0.1 до +0.1 мм
        /// </summary>
        private static readonly List<(double Z, double Diameter)> CalibrationTable = new()
        {
            ( -0.10, 86.300 ),
            ( -0.09, 79.500 ),
            ( -0.08, 75.050 ),
            ( -0.07, 70.850 ),
            ( -0.06, 66.550 ),
            ( -0.05, 62.150 ),
            ( -0.04, 58.600 ),
            ( -0.03, 56.300 ),
            ( -0.02, 53.300 ),
            ( -0.01, 51.650 ),
            (  0.00, 50.050 ),
            (  0.01, 49.550 ),
            (  0.02, 49.600 ),
            (  0.03, 49.850 ),
            (  0.04, 51.300 ),
            (  0.05, 53.450 ),
            (  0.06, 56.100 ),
            (  0.07, 58.550 ),
            (  0.08, 62.300 ),
            (  0.09, 65.300 ),
            (  0.10, 68.850 ),
        };

        /// <summary>
        /// Минимальный диаметр (фокус)
        /// </summary>
        public static double MinDiameterMicron => 49.09;

        /// <summary>
        /// Z координата фокуса
        /// </summary>
        public static double FocusZ => 0.03;

        /// <summary>
        /// Вычисляет Z offset для заданного диаметра пучка
        /// Использует линейную интерполяцию между табличными значениями
        /// </summary>
        /// <param name="targetDiameterMicron">Целевой диаметр пучка (μm)</param>
        /// <returns>Z offset (mm) для достижения целевого диаметра</returns>
        public static double CalculateZForDiameter(double targetDiameterMicron)
        {
            // Проверка диапазона
            double minDiameter = CalibrationTable.Min(p => p.Diameter);
            double maxDiameter = CalibrationTable.Max(p => p.Diameter);

            if (targetDiameterMicron < minDiameter)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetDiameterMicron),
                    $"Requested diameter {targetDiameterMicron:F2} μm is below minimum achievable diameter {minDiameter:F2} μm");
            }

            // ЭКСТРАПОЛЯЦИЯ: Если диаметр больше максимального измеренного
            if (targetDiameterMicron > maxDiameter)
            {
                // ИЗМЕНЕНО: Используем линейную экстраполяцию на основе последних двух точек
                // на ОТРИЦАТЕЛЬНОЙ стороне (максимальные диаметры находятся на отрицательной стороне!)
                var sortedByZ = CalibrationTable.Where(p => p.Z <= FocusZ).OrderBy(p => p.Z).ToList();

                if (sortedByZ.Count >= 2)
                {
                    var p1_extrap = sortedByZ[0]; // Первая точка (минимальный Z, максимальный диаметр)
                    var p2_extrap = sortedByZ[1]; // Вторая точка

                    // Линейная экстраполяция влево (в сторону уменьшения Z)
                    double z_extrap = LinearInterpolate(
                        p2_extrap.Diameter, p2_extrap.Z,
                        p1_extrap.Diameter, p1_extrap.Z,
                        targetDiameterMicron);

                    // Предупреждение в консоль (только для информации, не блокируем)
                    Console.WriteLine($"⚠️ EXTRAPOLATION: Diameter {targetDiameterMicron:F2} μm > max measured {maxDiameter:F2} μm");
                    Console.WriteLine($"   Using linear extrapolation from last 2 points on NEGATIVE side:");
                    Console.WriteLine($"   ({p1_extrap.Diameter:F2} μm @ {p1_extrap.Z:F3} mm) → ({p2_extrap.Diameter:F2} μm @ {p2_extrap.Z:F3} mm)");
                    Console.WriteLine($"   Extrapolated Z = {z_extrap:F3} mm (USE WITH CAUTION!)");

                    return z_extrap;
                }
                else
                {
                    throw new InvalidOperationException("Not enough calibration points for extrapolation");
                }
            }

            // Особый случай: точно в фокусе
            if (Math.Abs(targetDiameterMicron - MinDiameterMicron) < 0.01)
            {
                return FocusZ;
            }

            // Для диаметров больше минимума нужно выбрать, с какой стороны от фокуса работать
            // ИЗМЕНЕНО: Для больших диаметров используем ОТРИЦАТЕЛЬНУЮ сторону (Z < 0.03), где диапазон больше
            // Положительная сторона: 49.09-63.52 μm (узкий диапазон)
            // Отрицательная сторона: 49.09-95.40 μm (широкий диапазон)

            // Пробуем сначала отрицательную сторону (больше диапазон для больших диаметров)
            var candidatePoints = FindInterpolationPoints(targetDiameterMicron, preferPositiveZ: false);

            if (candidatePoints == null)
            {
                // Если не нашли на отрицательной, пробуем положительную сторону
                candidatePoints = FindInterpolationPoints(targetDiameterMicron, preferPositiveZ: true);
            }

            if (candidatePoints == null)
            {
                throw new InvalidOperationException(
                    $"Cannot find interpolation points for diameter {targetDiameterMicron:F2} μm");
            }

            // Линейная интерполяция между двумя точками
            var (p1, p2) = candidatePoints.Value;
            double z = LinearInterpolate(
                p1.Diameter, p1.Z,
                p2.Diameter, p2.Z,
                targetDiameterMicron);

            return z;
        }

        /// <summary>
        /// Вычисляет диаметр пучка для заданного Z offset
        /// Использует линейную интерполяцию между табличными значениями
        /// </summary>
        /// <param name="z">Z offset (mm)</param>
        /// <returns>Диаметр пучка (μm) при данном Z</returns>
        public static double CalculateDiameterForZ(double z)
        {
            // Проверка диапазона
            double minZ = CalibrationTable.Min(p => p.Z);
            double maxZ = CalibrationTable.Max(p => p.Z);

            // ЭКСТРАПОЛЯЦИЯ: Если Z вне диапазона калибровки
            if (z < minZ)
            {
                // Экстраполяция влево (отрицательная сторона)
                var sortedByZ_left = CalibrationTable.OrderBy(p => p.Z).ToList();
                if (sortedByZ_left.Count >= 2)
                {
                    var p1_left = sortedByZ_left[0]; // Первая точка (минимальный Z)
                    var p2_left = sortedByZ_left[1]; // Вторая точка

                    double diameter = LinearInterpolate(p1_left.Z, p1_left.Diameter, p2_left.Z, p2_left.Diameter, z);
                    Console.WriteLine($"⚠️ EXTRAPOLATION: Z={z:F3} mm < min {minZ:F3} mm, extrapolated diameter = {diameter:F2} μm");
                    return diameter;
                }
            }
            else if (z > maxZ)
            {
                // Экстраполяция вправо (положительная сторона)
                var sortedByZ_right = CalibrationTable.OrderByDescending(p => p.Z).ToList();
                if (sortedByZ_right.Count >= 2)
                {
                    var p1_right = sortedByZ_right[0]; // Последняя точка (максимальный Z)
                    var p2_right = sortedByZ_right[1]; // Предпоследняя точка

                    double diameter = LinearInterpolate(p2_right.Z, p2_right.Diameter, p1_right.Z, p1_right.Diameter, z);
                    Console.WriteLine($"⚠️ EXTRAPOLATION: Z={z:F3} mm > max {maxZ:F3} mm, extrapolated diameter = {diameter:F2} μm");
                    return diameter;
                }
            }

            // Поиск двух ближайших точек для интерполяции
            var sortedByZ = CalibrationTable.OrderBy(p => p.Z).ToList();

            for (int i = 0; i < sortedByZ.Count - 1; i++)
            {
                var p1 = sortedByZ[i];
                var p2 = sortedByZ[i + 1];

                if (z >= p1.Z && z <= p2.Z)
                {
                    // Линейная интерполяция
                    double diameter = LinearInterpolate(
                        p1.Z, p1.Diameter,
                        p2.Z, p2.Diameter,
                        z);

                    return diameter;
                }
            }

            // Точное совпадение с последней точкой
            return sortedByZ.Last().Diameter;
        }

        /// <summary>
        /// Находит две точки для интерполяции диаметра
        /// </summary>
        private static ((double Z, double Diameter) p1, (double Z, double Diameter) p2)?
            FindInterpolationPoints(double targetDiameter, bool preferPositiveZ)
        {
            // Фильтруем точки с нужной стороны от фокуса
            var relevantPoints = preferPositiveZ
                ? CalibrationTable.Where(p => p.Z >= FocusZ).OrderBy(p => p.Z).ToList()
                : CalibrationTable.Where(p => p.Z <= FocusZ).OrderByDescending(p => p.Z).ToList();

            // Ищем две точки, между которыми находится целевой диаметр
            for (int i = 0; i < relevantPoints.Count - 1; i++)
            {
                var p1 = relevantPoints[i];
                var p2 = relevantPoints[i + 1];

                // Проверяем, находится ли целевой диаметр между этими точками
                if ((targetDiameter >= p1.Diameter && targetDiameter <= p2.Diameter) ||
                    (targetDiameter <= p1.Diameter && targetDiameter >= p2.Diameter))
                {
                    return (p1, p2);
                }
            }

            return null;
        }

        /// <summary>
        /// Линейная интерполяция
        /// </summary>
        private static double LinearInterpolate(double x1, double y1, double x2, double y2, double x)
        {
            if (Math.Abs(x2 - x1) < 1e-10)
                return y1;

            return y1 + (x - x1) * (y2 - y1) / (x2 - x1);
        }

        /// <summary>
        /// Получает полную калибровочную таблицу
        /// </summary>
        public static IReadOnlyList<(double Z, double Diameter)> GetCalibrationTable()
        {
            return CalibrationTable.AsReadOnly();
        }

        /// <summary>
        /// Находит все возможные Z для заданного диаметра (может быть две точки из-за симметрии)
        /// </summary>
        public static List<double> FindAllZForDiameter(double targetDiameterMicron)
        {
            var results = new List<double>();

            // Проверяем положительную сторону
            try
            {
                var candidatePoints = FindInterpolationPoints(targetDiameterMicron, preferPositiveZ: true);
                if (candidatePoints != null)
                {
                    var (p1, p2) = candidatePoints.Value;
                    double z = LinearInterpolate(p1.Diameter, p1.Z, p2.Diameter, p2.Z, targetDiameterMicron);
                    results.Add(z);
                }
            }
            catch { }

            // Проверяем отрицательную сторону
            try
            {
                var candidatePoints = FindInterpolationPoints(targetDiameterMicron, preferPositiveZ: false);
                if (candidatePoints != null)
                {
                    var (p1, p2) = candidatePoints.Value;
                    double z = LinearInterpolate(p1.Diameter, p1.Z, p2.Diameter, p2.Z, targetDiameterMicron);

                    // Добавляем только если это новое значение
                    if (!results.Any(r => Math.Abs(r - z) < 0.001))
                    {
                        results.Add(z);
                    }
                }
            }
            catch { }

            return results;
        }

        /// <summary>
        /// Генерирует отчёт о калибровке
        /// </summary>
        public static string GenerateCalibrationReport()
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
            report.AppendLine("║         BEAM DIAMETER CALIBRATION TABLE (Real Data)           ║");
            report.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
            report.AppendLine();

            report.AppendLine($"Focus Point: Z = {FocusZ:F3} mm, Diameter = {MinDiameterMicron:F2} μm");
            report.AppendLine();

            report.AppendLine("Calibration Data:");
            report.AppendLine("┌──────────┬──────────────┬─────────────────────┐");
            report.AppendLine("│ Z (mm)   │ Diameter(μm) │ Δ from focus (μm)   │");
            report.AppendLine("├──────────┼──────────────┼─────────────────────┤");

            foreach (var (z, diameter) in CalibrationTable.OrderBy(p => p.Z))
            {
                double delta = diameter - MinDiameterMicron;
                string marker = Math.Abs(z - FocusZ) < 0.001 ? " ← FOCUS" : "";
                report.AppendLine($"│ {z,8:F2} │ {diameter,12:F2} │ {delta,19:+0.00;-0.00} │{marker}");
            }

            report.AppendLine("└──────────┴──────────────┴─────────────────────┘");
            report.AppendLine();

            report.AppendLine("Usage Examples:");
            report.AppendLine($"  CalculateZForDiameter(65.0) → Z = {CalculateZForDiameter(65.0):F3} mm");
            report.AppendLine($"  CalculateZForDiameter(80.0) → Z = {CalculateZForDiameter(80.0):F3} mm");
            report.AppendLine($"  CalculateDiameterForZ(0.05) → Diameter = {CalculateDiameterForZ(0.05):F2} μm");

            return report.ToString();
        }
    }
}
