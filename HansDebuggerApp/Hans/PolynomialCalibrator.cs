using Hans.NET.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HansDebuggerApp.Hans
{
    /// <summary>
    /// Калибратор полинома 3D коррекции для управления диаметром пучка
    /// Вычисляет оптимальные коэффициенты bfactor и cfactor на основе реальных измерений
    /// </summary>
    public class PolynomialCalibrator
    {
        private readonly BeamConfig _beamConfig;
        private readonly double _baseFocal;

        public PolynomialCalibrator(BeamConfig beamConfig, double baseFocal)
        {
            _beamConfig = beamConfig;
            _baseFocal = baseFocal;
        }

        /// <summary>
        /// Данные одного измерения калибровки
        /// </summary>
        public class CalibrationPoint
        {
            /// <summary>Целевой диаметр, который запросили (μm)</summary>
            public double TargetDiameterMicron { get; set; }

            /// <summary>Реальный измеренный диаметр (μm)</summary>
            public double MeasuredDiameterMicron { get; set; }

            /// <summary>Z offset, который был вычислен для целевого диаметра (mm)</summary>
            public double ZOffsetMm { get; set; }

            /// <summary>Focal length после добавления Z offset (mm)</summary>
            public double FocalLengthMm { get; set; }
        }

        /// <summary>
        /// Результат калибровки
        /// </summary>
        public class CalibrationResult
        {
            /// <summary>Откалиброванный bfactor (линейный коэффициент)</summary>
            public double Bfactor { get; set; }

            /// <summary>Откалиброванный cfactor (свободный член)</summary>
            public double Cfactor { get; set; }

            /// <summary>Afactor (обычно 0 для линейного полинома)</summary>
            public double Afactor { get; set; } = 0.0;

            /// <summary>Среднеквадратичная ошибка (μm)</summary>
            public double RmsErrorMicron { get; set; }

            /// <summary>Максимальная ошибка (μm)</summary>
            public double MaxErrorMicron { get; set; }

            /// <summary>Точки калибровки с вычисленными ошибками</summary>
            public List<(CalibrationPoint Point, double ErrorMicron)> Points { get; set; }

            public override string ToString()
            {
                return $"Calibration Result:\n" +
                       $"  bfactor = {Bfactor:F9}\n" +
                       $"  cfactor = {Cfactor:F6}\n" +
                       $"  afactor = {Afactor:F6}\n" +
                       $"  RMS Error = {RmsErrorMicron:F2} μm\n" +
                       $"  Max Error = {MaxErrorMicron:F2} μm\n" +
                       $"  Polynomial: [-{Math.Abs(Cfactor):F6}, {Bfactor:F9}, {Afactor:F6}]";
            }
        }

        /// <summary>
        /// Подготавливает точки для калибровки (вычисляет Z offset и focal length для целевых диаметров)
        /// Эти точки нужно протестировать на реальном оборудовании и измерить фактический диаметр
        /// </summary>
        public List<CalibrationPoint> PrepareCalibrationPoints(double[] targetDiametersMicron)
        {
            var points = new List<CalibrationPoint>();

            foreach (var targetDiameter in targetDiametersMicron)
            {
                // Вычисляем теоретический Z offset для целевого диаметра
                double zOffsetMm = _beamConfig.CalculateZOffset(targetDiameter);

                // Вычисляем focal length с учётом Z offset
                // focalLength = sqrt(x² + y² + (baseFocal + coordZ)²)
                // Для точки (0,0) с coordZ = 0:
                double focalLengthMm = Math.Sqrt(0 + 0 + Math.Pow(_baseFocal + 0, 2));
                double focalLengthMicron = focalLengthMm * 1000.0 + zOffsetMm * 1000.0;
                focalLengthMm = focalLengthMicron / 1000.0;

                points.Add(new CalibrationPoint
                {
                    TargetDiameterMicron = targetDiameter,
                    MeasuredDiameterMicron = 0, // Заполнится после реального измерения
                    ZOffsetMm = zOffsetMm,
                    FocalLengthMm = focalLengthMm
                });
            }

            return points;
        }

        /// <summary>
        /// Выполняет калибровку линейного полинома Z = b*f + c
        /// Метод наименьших квадратов (Linear Least Squares)
        /// </summary>
        /// <param name="measurements">Точки калибровки с заполненными измеренными диаметрами</param>
        /// <returns>Результат калибровки с оптимальными коэффициентами</returns>
        public CalibrationResult CalibrateLinearPolynomial(List<CalibrationPoint> measurements)
        {
            if (measurements.Count < 2)
                throw new ArgumentException("Требуется минимум 2 точки измерения для калибровки");

            // Цель: найти bfactor и cfactor такие, что для каждой точки:
            // Z_target = b * f_target + c
            // где Z_target даёт нужный диаметр после применения SDK обратного полинома

            // Для каждой точки измерения вычисляем, какой Z нужен для получения измеренного диаметра
            var dataPoints = new List<(double focalLength, double zTarget)>();

            foreach (var point in measurements)
            {
                // Вычисляем, какой Z offset нужен для получения ИЗМЕРЕННОГО диаметра
                double zOffsetForMeasured = _beamConfig.CalculateZOffset(point.MeasuredDiameterMicron);

                // Вычисляем focal length для этого Z offset
                double focalLengthMicron = _baseFocal * 1000.0 + zOffsetForMeasured * 1000.0;
                double focalLengthMm = focalLengthMicron / 1000.0;

                dataPoints.Add((focalLengthMm, zOffsetForMeasured));
            }

            // Метод наименьших квадратов для линейной регрессии: Z = b*f + c
            // Решаем систему нормальных уравнений:
            // [Σf²   Σf  ] [b]   [Σ(f*Z)]
            // [Σf    n   ] [c] = [ΣZ    ]

            int n = dataPoints.Count;
            double sumF = dataPoints.Sum(p => p.focalLength);
            double sumZ = dataPoints.Sum(p => p.zTarget);
            double sumF2 = dataPoints.Sum(p => p.focalLength * p.focalLength);
            double sumFZ = dataPoints.Sum(p => p.focalLength * p.zTarget);

            // Определитель матрицы
            double det = n * sumF2 - sumF * sumF;

            if (Math.Abs(det) < 1e-10)
                throw new InvalidOperationException("Матрица вырожденная, невозможно вычислить коэффициенты");

            // Решение системы (формулы Крамера)
            double bfactor = (n * sumFZ - sumF * sumZ) / det;
            double cfactor = (sumF2 * sumZ - sumF * sumFZ) / det;

            // Вычисляем ошибки
            var errors = new List<(CalibrationPoint Point, double ErrorMicron)>();
            double sumSquaredError = 0;
            double maxError = 0;

            for (int i = 0; i < measurements.Count; i++)
            {
                var point = measurements[i];
                var (focalLength, zTarget) = dataPoints[i];

                // Вычисляем Z, который даст полином для этого focal length
                double zPredicted = bfactor * focalLength + cfactor;

                // Вычисляем, какой диаметр получится при этом Z
                double diameterPredicted = _beamConfig.CalculateDiameter((float)zPredicted);

                // Ошибка = разница между целевым и предсказанным диаметром
                double error = Math.Abs(point.TargetDiameterMicron - diameterPredicted);

                errors.Add((point, error));
                sumSquaredError += error * error;
                maxError = Math.Max(maxError, error);
            }

            double rmsError = Math.Sqrt(sumSquaredError / measurements.Count);

            return new CalibrationResult
            {
                Bfactor = bfactor,
                Cfactor = cfactor,
                Afactor = 0.0,
                RmsErrorMicron = rmsError,
                MaxErrorMicron = maxError,
                Points = errors
            };
        }

        /// <summary>
        /// Простая калибровка: приводит полином к нулю для минимального диаметра
        /// Использует существующий bfactor, вычисляет только cfactor
        /// </summary>
        public CalibrationResult CalibrateSimple(double existingBfactor, double minDiameterMicron)
        {
            // Для минимального диаметра (в фокусе) Z offset = 0
            double focalLengthMm = _baseFocal;

            // Вычисляем cfactor так, чтобы Z = 0 для минимального диаметра
            // 0 = bfactor * focalLength + cfactor
            // cfactor = -(bfactor * focalLength)
            double cfactor = -(existingBfactor * focalLengthMm);

            // Небольшой сдвиг для избежания отрицательных значений
            cfactor += 0.001;

            return new CalibrationResult
            {
                Bfactor = existingBfactor,
                Cfactor = cfactor,
                Afactor = 0.0,
                RmsErrorMicron = 0,
                MaxErrorMicron = 0,
                Points = new List<(CalibrationPoint Point, double ErrorMicron)>()
            };
        }

        /// <summary>
        /// Генерирует отчёт о калибровке в текстовом формате
        /// </summary>
        public string GenerateCalibrationReport(CalibrationResult result)
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine("╔═══════════════════════════════════════════════════════════════╗");
            report.AppendLine("║           CALIBRATION REPORT - Z CORRECTION POLYNOMIAL         ║");
            report.AppendLine("╚═══════════════════════════════════════════════════════════════╝");
            report.AppendLine();

            report.AppendLine("CALIBRATED COEFFICIENTS:");
            report.AppendLine($"  bfactor = {result.Bfactor:F9}");
            report.AppendLine($"  cfactor = {result.Cfactor:F6}");
            report.AppendLine($"  afactor = {result.Afactor:F6}");
            report.AppendLine();

            report.AppendLine("POLYNOMIAL FORMULA:");
            report.AppendLine($"  Z(f) = {result.Afactor:F6}×f² + {result.Bfactor:F9}×f + ({result.Cfactor:F6})");
            report.AppendLine();

            report.AppendLine("JSON CONFIGURATION:");
            report.AppendLine("  \"correctionPolynomial\": [");
            report.AppendLine($"    {result.Cfactor:F6},  // Cfactor (constant)");
            report.AppendLine($"    {result.Bfactor:F9},  // Bfactor (linear)");
            report.AppendLine($"    {result.Afactor:F6}   // Afactor (quadratic)");
            report.AppendLine("  ]");
            report.AppendLine();

            report.AppendLine("ERROR STATISTICS:");
            report.AppendLine($"  RMS Error:     {result.RmsErrorMicron:F2} μm");
            report.AppendLine($"  Max Error:     {result.MaxErrorMicron:F2} μm");
            report.AppendLine();

            if (result.Points.Count > 0)
            {
                report.AppendLine("CALIBRATION POINTS:");
                report.AppendLine("┌─────────────┬─────────────┬──────────┬──────────────┐");
                report.AppendLine("│ Target (μm) │ Measured    │ Z (mm)   │ Error (μm)   │");
                report.AppendLine("├─────────────┼─────────────┼──────────┼──────────────┤");

                foreach (var (point, error) in result.Points)
                {
                    report.AppendLine($"│ {point.TargetDiameterMicron,11:F1} │ {point.MeasuredDiameterMicron,11:F1} │ {point.ZOffsetMm,8:F3} │ {error,12:F2} │");
                }

                report.AppendLine("└─────────────┴─────────────┴──────────┴──────────────┘");
            }

            return report.ToString();
        }
    }
}
