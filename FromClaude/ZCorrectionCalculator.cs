using System;

namespace Hans.NET.ZCorrection
{
    /// <summary>
    /// Калькулятор Z-коррекции для компенсации кривизны поля сканера
    ///
    /// Используется для расчёта корректирующего смещения фокуса (Z) в зависимости
    /// от положения точки в рабочем поле сканера. Компенсирует оптические искажения
    /// F-Theta линзы (field curvature).
    /// </summary>
    public class ZCorrectionCalculator
    {
        #region Поля конфигурации

        /// <summary>
        /// Коэффициент A квадратичной коррекции (мм)
        /// Отвечает за параболическую кривизну линзы
        /// </summary>
        public double AFactor { get; set; }

        /// <summary>
        /// Коэффициент B линейной коррекции (безразмерный)
        /// Отвечает за наклон фокальной плоскости
        /// </summary>
        public double BFactor { get; set; }

        /// <summary>
        /// Коэффициент C константной коррекции (мм)
        /// Базовое смещение фокуса
        /// </summary>
        public double CFactor { get; set; }

        /// <summary>
        /// Смещение по оси Z из конфигурации сканера (мм)
        /// </summary>
        public double OffsetZ { get; set; }

        /// <summary>
        /// Масштабный коэффициент по оси Z
        /// </summary>
        public double ScaleZ { get; set; }

        /// <summary>
        /// Включена ли Z-коррекция
        /// </summary>
        public bool EnableZCorrection { get; set; }

        #endregion

        #region Конструкторы

        /// <summary>
        /// Создаёт калькулятор с параметрами по умолчанию (без коррекции)
        /// </summary>
        public ZCorrectionCalculator()
        {
            AFactor = 0.0;
            BFactor = 0.0;
            CFactor = 0.0;
            OffsetZ = 0.0;
            ScaleZ = 1.0;
            EnableZCorrection = false;
        }

        /// <summary>
        /// Создаёт калькулятор с заданными коэффициентами
        /// </summary>
        /// <param name="aFactor">Коэффициент A (квадратичный)</param>
        /// <param name="bFactor">Коэффициент B (линейный)</param>
        /// <param name="cFactor">Коэффициент C (константа)</param>
        /// <param name="offsetZ">Смещение Z (мм)</param>
        /// <param name="scaleZ">Масштаб Z</param>
        public ZCorrectionCalculator(double aFactor, double bFactor, double cFactor,
                                     double offsetZ = 0.0, double scaleZ = 1.0)
        {
            AFactor = aFactor;
            BFactor = bFactor;
            CFactor = cFactor;
            OffsetZ = offsetZ;
            ScaleZ = scaleZ;
            EnableZCorrection = true;
        }

        #endregion

        #region Основные методы расчёта

        /// <summary>
        /// Рассчитывает корректирующее смещение Z для заданных координат XY
        ///
        /// Формула: Z_correction = A × r² + B × r + C
        /// где r = √(X² + Y²) - расстояние от центра поля
        /// </summary>
        /// <param name="x">Координата X в мм</param>
        /// <param name="y">Координата Y в мм</param>
        /// <returns>Значение Z-коррекции в мм</returns>
        public double CalculateZCorrection(double x, double y)
        {
            if (!EnableZCorrection)
                return 0.0;

            // Расстояние от центра поля (радиус)
            double r = Math.Sqrt(x * x + y * y);

            // Квадратичная формула коррекции
            double zCorrection = AFactor * r * r + BFactor * r + CFactor;

            return zCorrection;
        }

        /// <summary>
        /// Рассчитывает финальное значение Z с учётом всех трансформаций
        /// </summary>
        /// <param name="x">Координата X в мм</param>
        /// <param name="y">Координата Y в мм</param>
        /// <param name="inputZ">Входное значение Z в мм</param>
        /// <returns>Финальное значение Z с учётом масштаба, смещения и коррекции</returns>
        public double CalculateFinalZ(double x, double y, double inputZ = 0.0)
        {
            // Шаг 1: Применяем масштаб
            double scaledZ = inputZ * ScaleZ;

            // Шаг 2: Применяем смещение
            double offsettedZ = scaledZ + OffsetZ;

            // Шаг 3: Применяем коррекцию кривизны поля
            double zCorrection = CalculateZCorrection(x, y);
            double finalZ = offsettedZ + zCorrection;

            return finalZ;
        }

        /// <summary>
        /// Рассчитывает корректирующее смещение Z для заданного радиуса
        /// </summary>
        /// <param name="radius">Расстояние от центра поля в мм</param>
        /// <returns>Значение Z-коррекции в мм</returns>
        public double CalculateZCorrectionByRadius(double radius)
        {
            if (!EnableZCorrection)
                return 0.0;

            double zCorrection = AFactor * radius * radius + BFactor * radius + CFactor;
            return zCorrection;
        }

        /// <summary>
        /// Рассчитывает таблицу Z-коррекции для заданного диапазона радиусов
        /// Полезно для анализа и визуализации кривизны поля
        /// </summary>
        /// <param name="maxRadius">Максимальный радиус в мм</param>
        /// <param name="steps">Количество шагов</param>
        /// <returns>Массив пар (радиус, Z-коррекция)</returns>
        public (double radius, double zCorrection)[] GenerateCorrectionTable(double maxRadius, int steps = 20)
        {
            var table = new (double, double)[steps + 1];
            double step = maxRadius / steps;

            for (int i = 0; i <= steps; i++)
            {
                double r = i * step;
                double z = CalculateZCorrectionByRadius(r);
                table[i] = (r, z);
            }

            return table;
        }

        #endregion

        #region Утилиты и диагностика

        /// <summary>
        /// Выводит информацию о текущей конфигурации
        /// </summary>
        public void PrintConfiguration()
        {
            Console.WriteLine("=== Конфигурация Z-коррекции ===");
            Console.WriteLine($"Включено: {EnableZCorrection}");
            Console.WriteLine($"A (квадратичный): {AFactor:F9} мм⁻¹");
            Console.WriteLine($"B (линейный):     {BFactor:F9}");
            Console.WriteLine($"C (константа):    {CFactor:F6} мм");
            Console.WriteLine($"Смещение Z:       {OffsetZ:F6} мм");
            Console.WriteLine($"Масштаб Z:        {ScaleZ:F3}");
            Console.WriteLine();

            if (EnableZCorrection)
            {
                Console.WriteLine("Формула: Z_correction = A × r² + B × r + C");
                Console.WriteLine($"         Z_correction = {AFactor:F9} × r² + {BFactor:F9} × r + ({CFactor:F6})");
            }
        }

        /// <summary>
        /// Выводит таблицу Z-коррекции для различных радиусов
        /// </summary>
        /// <param name="maxRadius">Максимальный радиус в мм</param>
        public void PrintCorrectionTable(double maxRadius = 200.0)
        {
            Console.WriteLine("\n=== Таблица Z-коррекции ===");
            Console.WriteLine("Радиус (мм) | Z-коррекция (мм) | Примечание");
            Console.WriteLine("------------|------------------|---------------------------");

            var table = GenerateCorrectionTable(maxRadius, 10);
            foreach (var (radius, zCorrection) in table)
            {
                string note = "";
                if (radius == 0)
                    note = "Центр поля";
                else if (Math.Abs(radius - maxRadius) < 0.01)
                    note = "Край поля";
                else if (Math.Abs(radius - maxRadius / 2) < 0.01)
                    note = "Середина";

                Console.WriteLine($"{radius,11:F2} | {zCorrection,16:F6} | {note}");
            }
        }

        /// <summary>
        /// Возвращает максимальное отклонение Z в пределах заданного радиуса
        /// Полезно для оценки глубины кривизны поля
        /// </summary>
        /// <param name="maxRadius">Максимальный радиус поля в мм</param>
        /// <returns>Диапазон Z-коррекции (мин, макс, размах)</returns>
        public (double min, double max, double range) GetCorrectionRange(double maxRadius)
        {
            double centerZ = CalculateZCorrectionByRadius(0);
            double edgeZ = CalculateZCorrectionByRadius(maxRadius);

            double min = Math.Min(centerZ, edgeZ);
            double max = Math.Max(centerZ, edgeZ);
            double range = max - min;

            return (min, max, range);
        }

        #endregion

        #region Примеры использования

        /// <summary>
        /// Пример 1: Базовый расчёт Z-коррекции для конкретной точки
        /// </summary>
        public static void Example1_BasicCalculation()
        {
            Console.WriteLine("\n=== Пример 1: Базовый расчёт Z-коррекции ===\n");

            // Создаём калькулятор с реальными коэффициентами из конфигурации
            var calculator = new ZCorrectionCalculator(
                aFactor: 0.0,          // Нет квадратичной коррекции
                bFactor: 0.013944261,  // Линейная коррекция
                cFactor: -7.5056114    // Базовое смещение
            );

            // Точки для тестирования
            var testPoints = new[]
            {
                (x: 0.0, y: 0.0, name: "Центр поля"),
                (x: 50.0, y: 0.0, name: "50 мм вправо"),
                (x: 100.0, y: 0.0, name: "100 мм вправо"),
                (x: 100.0, y: 100.0, name: "Угол (100, 100)"),
                (x: 141.42, y: 141.42, name: "Край поля (200×200)")
            };

            foreach (var point in testPoints)
            {
                double zCorrection = calculator.CalculateZCorrection(point.x, point.y);
                double radius = Math.Sqrt(point.x * point.x + point.y * point.y);

                Console.WriteLine($"{point.name}:");
                Console.WriteLine($"  Координаты: ({point.x:F2}, {point.y:F2}) мм");
                Console.WriteLine($"  Радиус: {radius:F2} мм");
                Console.WriteLine($"  Z-коррекция: {zCorrection:F6} мм");
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Пример 2: Расчёт финального Z с учётом всех трансформаций
        /// </summary>
        public static void Example2_FullTransformation()
        {
            Console.WriteLine("\n=== Пример 2: Полная трансформация координат ===\n");

            var calculator = new ZCorrectionCalculator(
                aFactor: 0.0,
                bFactor: 0.013944261,
                cFactor: -7.5056114
            );
            calculator.OffsetZ = -0.001;  // Небольшое смещение из конфигурации
            calculator.ScaleZ = 1.0;

            // Входные координаты
            double x = 100.0;
            double y = 100.0;
            double inputZ = 0.5;  // Желаемое смещение фокуса +0.5 мм

            Console.WriteLine($"Входные координаты: X={x}, Y={y}, Z={inputZ}");
            Console.WriteLine();

            // Пошаговый расчёт
            double scaledZ = inputZ * calculator.ScaleZ;
            Console.WriteLine($"После масштабирования: Z = {inputZ} × {calculator.ScaleZ} = {scaledZ:F6} мм");

            double offsettedZ = scaledZ + calculator.OffsetZ;
            Console.WriteLine($"После смещения: Z = {scaledZ:F6} + {calculator.OffsetZ} = {offsettedZ:F6} мм");

            double radius = Math.Sqrt(x * x + y * y);
            double zCorrection = calculator.CalculateZCorrection(x, y);
            Console.WriteLine($"Радиус от центра: r = {radius:F2} мм");
            Console.WriteLine($"Z-коррекция: {zCorrection:F6} мм");

            double finalZ = calculator.CalculateFinalZ(x, y, inputZ);
            Console.WriteLine();
            Console.WriteLine($"ФИНАЛЬНАЯ Z: {finalZ:F6} мм");
        }

        /// <summary>
        /// Пример 3: Сравнение коррекции для разных конфигураций
        /// </summary>
        public static void Example3_CompareConfigurations()
        {
            Console.WriteLine("\n=== Пример 3: Сравнение конфигураций ===\n");

            // Конфигурация 1: Без коррекции (по умолчанию)
            var noCorrection = new ZCorrectionCalculator();

            // Конфигурация 2: Только линейная коррекция
            var linearCorrection = new ZCorrectionCalculator(
                aFactor: 0.0,
                bFactor: 0.013944261,
                cFactor: -7.5056114
            );

            // Конфигурация 3: С квадратичной коррекцией
            var quadraticCorrection = new ZCorrectionCalculator(
                aFactor: 0.00001,      // Небольшая параболическая кривизна
                bFactor: 0.013944261,
                cFactor: -7.5056114
            );

            // Тестовая точка на краю поля
            double x = 141.42;
            double y = 141.42;
            double radius = Math.Sqrt(x * x + y * y);

            Console.WriteLine($"Тестовая точка: ({x:F2}, {y:F2}) мм, радиус = {radius:F2} мм\n");

            Console.WriteLine("Конфигурация         | Z-коррекция (мм)");
            Console.WriteLine("---------------------|------------------");
            Console.WriteLine($"Без коррекции        | {noCorrection.CalculateZCorrection(x, y),16:F6}");
            Console.WriteLine($"Линейная коррекция   | {linearCorrection.CalculateZCorrection(x, y),16:F6}");
            Console.WriteLine($"Квадратичная коррекция| {quadraticCorrection.CalculateZCorrection(x, y),16:F6}");
        }

        /// <summary>
        /// Пример 4: Анализ глубины кривизны поля
        /// </summary>
        public static void Example4_FieldCurvatureAnalysis()
        {
            Console.WriteLine("\n=== Пример 4: Анализ глубины кривизны поля ===\n");

            var calculator = new ZCorrectionCalculator(
                aFactor: 0.0,
                bFactor: 0.013944261,
                cFactor: -7.5056114
            );

            // Анализ для разных размеров поля
            double[] fieldSizes = { 100.0, 200.0, 300.0, 400.0 };

            Console.WriteLine("Размер поля (мм) | Макс радиус (мм) | Размах Z (мм) | Примечание");
            Console.WriteLine("-----------------|------------------|---------------|------------------------");

            foreach (double fieldSize in fieldSizes)
            {
                double maxRadius = fieldSize / Math.Sqrt(2); // Радиус для квадратного поля
                var (min, max, range) = calculator.GetCorrectionRange(maxRadius);

                string note = range > 1.0 ? "⚠ Большая кривизна" : "✓ Приемлемо";

                Console.WriteLine($"{fieldSize,16:F0} | {maxRadius,16:F2} | {range,13:F6} | {note}");
            }
        }

        /// <summary>
        /// Пример 5: Применение Z-коррекции к траектории
        /// </summary>
        public static void Example5_ApplyToTrajectory()
        {
            Console.WriteLine("\n=== Пример 5: Применение Z-коррекции к круговой траектории ===\n");

            var calculator = new ZCorrectionCalculator(
                aFactor: 0.0,
                bFactor: 0.013944261,
                cFactor: -7.5056114
            );

            // Параметры окружности
            double centerX = 0.0;
            double centerY = 0.0;
            double circleRadius = 80.0;
            int pointsCount = 8;

            Console.WriteLine($"Окружность: центр ({centerX}, {centerY}), радиус {circleRadius} мм");
            Console.WriteLine($"Количество точек: {pointsCount}\n");

            Console.WriteLine("Точка | Угол (°) | X (мм)   | Y (мм)   | Z без корр. | Z с корр. | Разница (мкм)");
            Console.WriteLine("------|----------|----------|----------|-------------|-----------|---------------");

            for (int i = 0; i < pointsCount; i++)
            {
                double angle = 2.0 * Math.PI * i / pointsCount;
                double x = centerX + circleRadius * Math.Cos(angle);
                double y = centerY + circleRadius * Math.Sin(angle);

                double zWithoutCorrection = 0.0;
                double zWithCorrection = calculator.CalculateFinalZ(x, y, 0.0);
                double difference = (zWithCorrection - zWithoutCorrection) * 1000.0; // в мкм

                Console.WriteLine($"{i + 1,5} | {angle * 180 / Math.PI,8:F1} | {x,8:F3} | {y,8:F3} | " +
                                  $"{zWithoutCorrection,11:F6} | {zWithCorrection,9:F6} | {difference,13:F0}");
            }
        }

        #endregion

        #region Главная программа с примерами

        public static void Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║     Калькулятор Z-коррекции для сканера HashuScan             ║");
            Console.WriteLine("║     Компенсация кривизны поля (Field Curvature Correction)    ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");

            // Создаём калькулятор с реальными коэффициентами
            var calculator = new ZCorrectionCalculator(
                aFactor: 0.0,
                bFactor: 0.013944261,
                cFactor: -7.5056114
            );

            // Выводим конфигурацию
            calculator.PrintConfiguration();

            // Выводим таблицу коррекции
            calculator.PrintCorrectionTable(200.0);

            // Запускаем примеры
            Example1_BasicCalculation();
            Example2_FullTransformation();
            Example3_CompareConfigurations();
            Example4_FieldCurvatureAnalysis();
            Example5_ApplyToTrajectory();

            Console.WriteLine("\n\n=== Все примеры выполнены ===");
            Console.WriteLine("Нажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        #endregion
    }
}
