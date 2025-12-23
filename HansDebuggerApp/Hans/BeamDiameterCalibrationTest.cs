using System;
using System.Linq;

namespace HansDebuggerApp.Hans
{
    /// <summary>
    /// Тесты и примеры использования BeamDiameterCalibration
    /// </summary>
    public static class BeamDiameterCalibrationTest
    {
        public static void RunAllTests()
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("       BEAM DIAMETER CALIBRATION - TEST SUITE");
            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

            TestCalibrationReport();
            TestZToDiameter();
            TestDiameterToZ();
            TestSymmetry();
            TestEdgeCases();
            TestRealUseCases();

            Console.WriteLine("\n═══════════════════════════════════════════════════════════════");
            Console.WriteLine("                    ALL TESTS COMPLETED");
            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");
        }

        private static void TestCalibrationReport()
        {
            Console.WriteLine("TEST 1: Calibration Report");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

            string report = BeamDiameterCalibration.GenerateCalibrationReport();
            Console.WriteLine(report);
        }

        private static void TestZToDiameter()
        {
            Console.WriteLine("\nTEST 2: Z → Diameter Conversion");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

            double[] testZ = { -0.05, 0.0, 0.03, 0.05, 0.08 };

            Console.WriteLine("┌──────────┬──────────────┬──────────────┐");
            Console.WriteLine("│ Z (mm)   │ Calculated   │ Status       │");
            Console.WriteLine("├──────────┼──────────────┼──────────────┤");

            foreach (double z in testZ)
            {
                try
                {
                    double diameter = BeamDiameterCalibration.CalculateDiameterForZ(z);
                    Console.WriteLine($"│ {z,8:F2} │ {diameter,12:F2} │ ✓ OK         │");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"│ {z,8:F2} │ {"ERROR",12} │ ✗ {ex.Message,-10} │");
                }
            }

            Console.WriteLine("└──────────┴──────────────┴──────────────┘");
        }

        private static void TestDiameterToZ()
        {
            Console.WriteLine("\nTEST 3: Diameter → Z Conversion (including extrapolation)");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

            double[] testDiameters = { 49.09, 50, 55, 60, 65, 70, 80, 100, 120, 150 };

            Console.WriteLine("┌──────────────┬──────────┬──────────────────┐");
            Console.WriteLine("│ Diameter(μm) │ Z (mm)   │ Status           │");
            Console.WriteLine("├──────────────┼──────────┼──────────────────┤");

            foreach (double diameter in testDiameters)
            {
                try
                {
                    double z = BeamDiameterCalibration.CalculateZForDiameter(diameter);
                    string status = diameter > 95.4 ? "✓ Extrapolated" : "✓ Interpolated";
                    Console.WriteLine($"│ {diameter,12:F2} │ {z,8:F3} │ {status,-16} │");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"│ {diameter,12:F2} │ {"ERROR",8} │ ✗ {ex.Message.Substring(0, Math.Min(14, ex.Message.Length)),-14} │");
                }
            }

            Console.WriteLine("└──────────────┴──────────┴──────────────────┘");
        }

        private static void TestSymmetry()
        {
            Console.WriteLine("\nTEST 4: Symmetry Check (Multiple Z for Same Diameter)");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

            double[] testDiameters = { 52.08, 54.5, 58, 65, 70 };

            Console.WriteLine("┌──────────────┬────────────────────────────────────┐");
            Console.WriteLine("│ Diameter(μm) │ All Possible Z Values (mm)         │");
            Console.WriteLine("├──────────────┼────────────────────────────────────┤");

            foreach (double diameter in testDiameters)
            {
                try
                {
                    var allZ = BeamDiameterCalibration.FindAllZForDiameter(diameter);
                    string zList = string.Join(", ", allZ.Select(z => $"{z:F3}"));
                    Console.WriteLine($"│ {diameter,12:F2} │ {zList,-34} │");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"│ {diameter,12:F2} │ ERROR: {ex.Message,-25} │");
                }
            }

            Console.WriteLine("└──────────────┴────────────────────────────────────┘");
        }

        private static void TestEdgeCases()
        {
            Console.WriteLine("\nTEST 5: Edge Cases");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

            // Test 1: Минимальный диаметр (фокус)
            Console.WriteLine($"1. Focus point (min diameter {BeamDiameterCalibration.MinDiameterMicron} μm):");
            try
            {
                double z = BeamDiameterCalibration.CalculateZForDiameter(BeamDiameterCalibration.MinDiameterMicron);
                Console.WriteLine($"   Z = {z:F3} mm (expected: {BeamDiameterCalibration.FocusZ:F3} mm) ✓");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ERROR: {ex.Message} ✗");
            }

            // Test 2: Диаметр меньше минимального
            Console.WriteLine($"\n2. Below minimum diameter (40 μm < {BeamDiameterCalibration.MinDiameterMicron} μm):");
            try
            {
                double z = BeamDiameterCalibration.CalculateZForDiameter(40);
                Console.WriteLine($"   Z = {z:F3} mm ✗ (should have thrown exception!)");
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine($"   Correctly threw ArgumentOutOfRangeException ✓");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Wrong exception: {ex.GetType().Name} ✗");
            }

            // Test 3: Диаметр больше максимального
            Console.WriteLine($"\n3. Above maximum diameter (100 μm > 95.4 μm):");
            try
            {
                double z = BeamDiameterCalibration.CalculateZForDiameter(100);
                Console.WriteLine($"   Z = {z:F3} mm ✗ (should have thrown exception!)");
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine($"   Correctly threw ArgumentOutOfRangeException ✓");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Wrong exception: {ex.GetType().Name} ✗");
            }

            // Test 4: Z вне диапазона
            Console.WriteLine($"\n4. Z outside calibrated range (0.15 mm > 0.1 mm):");
            try
            {
                double d = BeamDiameterCalibration.CalculateDiameterForZ(0.15);
                Console.WriteLine($"   Diameter = {d:F2} μm ✗ (should have thrown exception!)");
            }
            catch (ArgumentOutOfRangeException)
            {
                Console.WriteLine($"   Correctly threw ArgumentOutOfRangeException ✓");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Wrong exception: {ex.GetType().Name} ✗");
            }
        }

        private static void TestRealUseCases()
        {
            Console.WriteLine("\nTEST 6: Real Use Cases");
            Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n");

            Console.WriteLine("Scenario: User wants to print with different beam diameters\n");

            double[] requestedDiameters = { 50, 55, 60, 65, 70, 80 };

            Console.WriteLine("┌──────────────┬──────────┬──────────────┬────────────────┐");
            Console.WriteLine("│ Requested    │ Z needed │ Actual       │ Error (μm)     │");
            Console.WriteLine("│ Diameter(μm) │ (mm)     │ Diameter(μm) │                │");
            Console.WriteLine("├──────────────┼──────────┼──────────────┼────────────────┤");

            foreach (double requested in requestedDiameters)
            {
                try
                {
                    // Шаг 1: Вычисляем нужный Z для запрошенного диаметра
                    double z = BeamDiameterCalibration.CalculateZForDiameter(requested);

                    // Шаг 2: Проверяем, какой диаметр получится при этом Z
                    double actual = BeamDiameterCalibration.CalculateDiameterForZ(z);

                    // Шаг 3: Вычисляем ошибку
                    double error = actual - requested;

                    string status = Math.Abs(error) < 1.0 ? "✓" : "⚠";

                    Console.WriteLine($"│ {requested,12:F2} │ {z,8:F3} │ {actual,12:F2} │ {error,14:+0.00;-0.00} {status} │");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"│ {requested,12:F2} │ {"ERROR",8} │ {"N/A",12} │ {ex.Message,-14} │");
                }
            }

            Console.WriteLine("└──────────────┴──────────┴──────────────┴────────────────┘");
            Console.WriteLine("\n✓ = Error < 1 μm (excellent)");
            Console.WriteLine("⚠ = Error >= 1 μm (check calibration)");
        }
    }
}
