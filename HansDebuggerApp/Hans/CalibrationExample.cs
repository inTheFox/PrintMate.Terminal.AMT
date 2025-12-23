using Hans.NET.Models;
using System;
using System.Collections.Generic;

namespace HansDebuggerApp.Hans
{
    /// <summary>
    /// Пример использования калибратора полинома
    /// </summary>
    public class CalibrationExample
    {
        public static void RunCalibrationExample()
        {
            Console.WriteLine("=== POLYNOMIAL CALIBRATION EXAMPLE ===\n");

            // 1. Создаём конфигурацию луча (из вашей системы)
            var beamConfig = new BeamConfig
            {
                MinBeamDiameterMicron = 65.0,
                WavelengthNano = 1070.0,
                RayleighLengthMicron = 1921.0,
                M2 = 1.593,
                FocalLengthMm = 538.46
            };

            double baseFocal = 538.46; // mm

            // 2. Создаём калибратор
            var calibrator = new PolynomialCalibrator(beamConfig, baseFocal);

            Console.WriteLine("SCENARIO 1: Simple Calibration (using existing bfactor)\n");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

            // Простая калибровка с существующим bfactor
            double existingBfactor = 0.013944261; // из вашей конфигурации
            var simpleResult = calibrator.CalibrateSimple(existingBfactor, beamConfig.MinBeamDiameterMicron);

            Console.WriteLine(simpleResult);
            Console.WriteLine("\n" + calibrator.GenerateCalibrationReport(simpleResult));

            Console.WriteLine("\n\nSCENARIO 2: Full Calibration (with real measurements)\n");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

            // Полная калибровка с реальными измерениями

            // Шаг 1: Подготавливаем точки для тестирования
            double[] targetDiameters = { 65.0, 80.0, 100.0, 120.0, 150.0 };
            var calibrationPoints = calibrator.PrepareCalibrationPoints(targetDiameters);

            Console.WriteLine("Step 1: Test points prepared. Generate UDM files for these diameters:");
            Console.WriteLine("┌─────────────┬──────────────┬──────────────────────────┐");
            Console.WriteLine("│ Diameter    │ Z Offset     │ Action                   │");
            Console.WriteLine("├─────────────┼──────────────┼──────────────────────────┤");
            foreach (var point in calibrationPoints)
            {
                Console.WriteLine($"│ {point.TargetDiameterMicron,11:F1} │ {point.ZOffsetMm,12:F6} │ Generate UDM & Measure   │");
            }
            Console.WriteLine("└─────────────┴──────────────┴──────────────────────────┘");

            Console.WriteLine("\nStep 2: After measuring real diameters on equipment, fill in measurements:");
            Console.WriteLine("(This is SIMULATED data - replace with real measurements!)\n");

            // СИМУЛЯЦИЯ: Предположим, что измерили следующие диаметры
            // (В реальности вы измеряете на оборудовании!)
            var measurements = new List<PolynomialCalibrator.CalibrationPoint>
            {
                new PolynomialCalibrator.CalibrationPoint
                {
                    TargetDiameterMicron = 65.0,
                    MeasuredDiameterMicron = 66.5,  // Измерили чуть больше
                    ZOffsetMm = 0.0,
                    FocalLengthMm = 538.46
                },
                new PolynomialCalibrator.CalibrationPoint
                {
                    TargetDiameterMicron = 80.0,
                    MeasuredDiameterMicron = 82.1,  // Измерили чуть больше
                    ZOffsetMm = 1.378,
                    FocalLengthMm = 539.838
                },
                new PolynomialCalibrator.CalibrationPoint
                {
                    TargetDiameterMicron = 100.0,
                    MeasuredDiameterMicron = 98.5,  // Измерили чуть меньше
                    ZOffsetMm = 2.246,
                    FocalLengthMm = 540.706
                },
                new PolynomialCalibrator.CalibrationPoint
                {
                    TargetDiameterMicron = 120.0,
                    MeasuredDiameterMicron = 119.2, // Почти точно
                    ZOffsetMm = 3.042,
                    FocalLengthMm = 541.502
                },
                new PolynomialCalibrator.CalibrationPoint
                {
                    TargetDiameterMicron = 150.0,
                    MeasuredDiameterMicron = 153.8, // Измерили чуть больше
                    ZOffsetMm = 4.188,
                    FocalLengthMm = 542.648
                }
            };

            Console.WriteLine("Measured data:");
            Console.WriteLine("┌─────────────┬─────────────┬──────────┐");
            Console.WriteLine("│ Target (μm) │ Measured    │ Error    │");
            Console.WriteLine("├─────────────┼─────────────┼──────────┤");
            foreach (var point in measurements)
            {
                double error = point.MeasuredDiameterMicron - point.TargetDiameterMicron;
                Console.WriteLine($"│ {point.TargetDiameterMicron,11:F1} │ {point.MeasuredDiameterMicron,11:F1} │ {error,8:+0.0;-0.0} │");
            }
            Console.WriteLine("└─────────────┴─────────────┴──────────┘\n");

            // Шаг 3: Выполняем калибровку
            Console.WriteLine("Step 3: Running least squares calibration...\n");
            var fullResult = calibrator.CalibrateLinearPolynomial(measurements);

            Console.WriteLine(calibrator.GenerateCalibrationReport(fullResult));

            Console.WriteLine("\n\nCOMPARISON:\n");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
            Console.WriteLine($"Simple Calibration:  cfactor = {simpleResult.Cfactor:F6}");
            Console.WriteLine($"Full Calibration:    cfactor = {fullResult.Cfactor:F6}");
            Console.WriteLine($"Difference:          {Math.Abs(simpleResult.Cfactor - fullResult.Cfactor):F6}");
            Console.WriteLine();
            Console.WriteLine($"Full calibration RMS error: {fullResult.RmsErrorMicron:F2} μm");
            Console.WriteLine($"Full calibration Max error: {fullResult.MaxErrorMicron:F2} μm");

            Console.WriteLine("\n\nRECOMMENDATION:\n");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");
            if (fullResult.RmsErrorMicron < 5.0)
            {
                Console.WriteLine("✓ Calibration quality: EXCELLENT");
                Console.WriteLine($"  Use full calibration result: cfactor = {fullResult.Cfactor:F6}");
            }
            else if (fullResult.RmsErrorMicron < 10.0)
            {
                Console.WriteLine("✓ Calibration quality: GOOD");
                Console.WriteLine($"  Use full calibration result: cfactor = {fullResult.Cfactor:F6}");
            }
            else
            {
                Console.WriteLine("⚠ Calibration quality: NEEDS IMPROVEMENT");
                Console.WriteLine("  Consider:");
                Console.WriteLine("  1. Check measurement accuracy");
                Console.WriteLine("  2. Recalibrate RayleighLengthMicron");
                Console.WriteLine("  3. Use more calibration points");
            }
        }

        /// <summary>
        /// Интерактивный помощник калибровки
        /// </summary>
        public static void InteractiveCalibrationWizard()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════════╗");
            Console.WriteLine("║    INTERACTIVE POLYNOMIAL CALIBRATION WIZARD          ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════╝\n");

            Console.WriteLine("This wizard will guide you through polynomial calibration.\n");
            Console.WriteLine("You will need:");
            Console.WriteLine("  1. Access to the scanner equipment");
            Console.WriteLine("  2. Ability to measure beam diameter (camera, burn test, etc.)");
            Console.WriteLine("  3. At least 3-5 different target diameters to test\n");

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            Console.Clear();

            // TODO: Реализовать интерактивный ввод данных
            Console.WriteLine("\n[Interactive wizard - to be implemented]\n");
            Console.WriteLine("For now, use RunCalibrationExample() to see the full workflow.");
        }
    }
}
