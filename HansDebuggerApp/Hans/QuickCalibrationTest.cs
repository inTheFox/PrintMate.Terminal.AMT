using System;

namespace HansDebuggerApp.Hans
{
    /// <summary>
    /// Быстрый тест для проверки вычисления Z для конкретных диаметров
    /// </summary>
    public static class QuickCalibrationTest
    {
        public static void TestSpecificDiameters()
        {
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("     QUICK CALIBRATION TEST - Specific Diameters");
            Console.WriteLine("═══════════════════════════════════════════════════════════════\n");

            double[] testDiameters = { 65, 70, 80, 90, 100 };

            Console.WriteLine("┌──────────────┬──────────┬────────────────┬──────────────┐");
            Console.WriteLine("│ Requested    │ Z calc   │ Verification   │ Error        │");
            Console.WriteLine("│ Diameter(μm) │ (mm)     │ Diameter(μm)   │ (μm)         │");
            Console.WriteLine("├──────────────┼──────────┼────────────────┼──────────────┤");

            foreach (double diameter in testDiameters)
            {
                try
                {
                    // Вычисляем Z для запрошенного диаметра
                    double z = BeamDiameterCalibration.CalculateZForDiameter(diameter);

                    // Проверяем: какой диаметр получится при этом Z
                    double verifyDiameter = BeamDiameterCalibration.CalculateDiameterForZ(z);

                    // Ошибка
                    double error = verifyDiameter - diameter;

                    string status = Math.Abs(error) < 1.0 ? "✓" : "⚠";

                    Console.WriteLine($"│ {diameter,12:F2} │ {z,8:F3} │ {verifyDiameter,14:F2} │ {error,12:+0.00;-0.00} {status} │");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"│ {diameter,12:F2} │ {"ERROR",8} │ {"-",14} │ {ex.Message.Substring(0, Math.Min(10, ex.Message.Length)),10} │");
                }
            }

            Console.WriteLine("└──────────────┴──────────┴────────────────┴──────────────┘");
            Console.WriteLine("\n✓ = Error < 1 μm (excellent)");
            Console.WriteLine("⚠ = Error >= 1 μm (check calibration or add more measurements)\n");
        }
    }
}
