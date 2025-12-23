using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace Hans.NET.ZCorrection
{
    #region Классы конфигурации

    /// <summary>
    /// Конфигурация сканера (упрощённая версия для Z-коррекции)
    /// </summary>
    public class ScannerConfiguration
    {
        [JsonPropertyName("scannerConfig")]
        public ScannerConfig ScannerConfig { get; set; }

        [JsonPropertyName("thirdAxisConfig")]
        public ThirdAxisConfig ThirdAxisConfig { get; set; }

        [JsonPropertyName("functionSwitcherConfig")]
        public FunctionSwitcherConfig FunctionSwitcherConfig { get; set; }

        [JsonPropertyName("cardInfo")]
        public CardInfo CardInfo { get; set; }
    }

    public class CardInfo
    {
        [JsonPropertyName("ipAddress")]
        public string IpAddress { get; set; }

        [JsonPropertyName("seqIndex")]
        public string SeqIndex { get; set; }
    }

    public class ScannerConfig
    {
        [JsonPropertyName("fieldSizeX")]
        public string FieldSizeX { get; set; }

        [JsonPropertyName("fieldSizeY")]
        public string FieldSizeY { get; set; }

        [JsonPropertyName("offsetX")]
        public string OffsetX { get; set; }

        [JsonPropertyName("offsetY")]
        public string OffsetY { get; set; }

        [JsonPropertyName("offsetZ")]
        public string OffsetZ { get; set; }

        [JsonPropertyName("rotateAngle")]
        public string RotateAngle { get; set; }

        [JsonPropertyName("scaleX")]
        public string ScaleX { get; set; }

        [JsonPropertyName("scaleY")]
        public string ScaleY { get; set; }

        [JsonPropertyName("scaleZ")]
        public string ScaleZ { get; set; }
    }

    public class ThirdAxisConfig
    {
        [JsonPropertyName("afactor")]
        public string AFactor { get; set; }

        [JsonPropertyName("bfactor")]
        public string BFactor { get; set; }

        [JsonPropertyName("cfactor")]
        public string CFactor { get; set; }
    }

    public class FunctionSwitcherConfig
    {
        [JsonPropertyName("enableZCorrection")]
        public bool EnableZCorrection { get; set; }

        [JsonPropertyName("enableDiameterChange")]
        public bool EnableDiameterChange { get; set; }

        [JsonPropertyName("enableDynamicChangeVariables")]
        public bool EnableDynamicChangeVariables { get; set; }

        [JsonPropertyName("enablePowerCorrection")]
        public bool EnablePowerCorrection { get; set; }
    }

    #endregion

    /// <summary>
    /// Калькулятор Z-коррекции, загружаемый из JSON конфигурации
    /// </summary>
    public class ZCorrectionFromConfig
    {
        #region Поля

        private double aFactor;
        private double bFactor;
        private double cFactor;
        private double offsetX;
        private double offsetY;
        private double offsetZ;
        private double rotateAngle;
        private double scaleX;
        private double scaleY;
        private double scaleZ;
        private bool enableZCorrection;
        private string cardIpAddress;

        #endregion

        #region Свойства

        public double AFactor => aFactor;
        public double BFactor => bFactor;
        public double CFactor => cFactor;
        public double OffsetX => offsetX;
        public double OffsetY => offsetY;
        public double OffsetZ => offsetZ;
        public double RotateAngle => rotateAngle;
        public double ScaleX => scaleX;
        public double ScaleY => scaleY;
        public double ScaleZ => scaleZ;
        public bool EnableZCorrection => enableZCorrection;
        public string CardIpAddress => cardIpAddress;

        #endregion

        #region Конструкторы

        /// <summary>
        /// Создаёт калькулятор из JSON конфигурации
        /// </summary>
        /// <param name="jsonFilePath">Путь к JSON файлу с конфигурацией</param>
        public ZCorrectionFromConfig(string jsonFilePath)
        {
            LoadFromJson(jsonFilePath);
        }

        /// <summary>
        /// Создаёт калькулятор из объекта конфигурации
        /// </summary>
        public ZCorrectionFromConfig(ScannerConfiguration config)
        {
            LoadFromConfig(config);
        }

        #endregion

        #region Загрузка конфигурации

        /// <summary>
        /// Загружает конфигурацию из JSON файла
        /// </summary>
        private void LoadFromJson(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
                throw new FileNotFoundException($"Файл конфигурации не найден: {jsonFilePath}");

            string json = File.ReadAllText(jsonFilePath);
            var configs = JsonSerializer.Deserialize<List<ScannerConfiguration>>(json);

            if (configs == null || configs.Count == 0)
                throw new InvalidDataException("JSON не содержит конфигурации сканера");

            LoadFromConfig(configs[0]); // Берём первую карту
        }

        /// <summary>
        /// Загружает конфигурацию из объекта
        /// </summary>
        private void LoadFromConfig(ScannerConfiguration config)
        {
            if (config.ThirdAxisConfig == null)
                throw new InvalidDataException("Отсутствует секция thirdAxisConfig");

            if (config.ScannerConfig == null)
                throw new InvalidDataException("Отсутствует секция scannerConfig");

            // Загружаем коэффициенты Z-коррекции
            aFactor = ParseDouble(config.ThirdAxisConfig.AFactor, "afactor");
            bFactor = ParseDouble(config.ThirdAxisConfig.BFactor, "bfactor");
            cFactor = ParseDouble(config.ThirdAxisConfig.CFactor, "cfactor");

            // Загружаем параметры сканера
            offsetX = ParseDouble(config.ScannerConfig.OffsetX, "offsetX");
            offsetY = ParseDouble(config.ScannerConfig.OffsetY, "offsetY");
            offsetZ = ParseDouble(config.ScannerConfig.OffsetZ, "offsetZ");
            rotateAngle = ParseDouble(config.ScannerConfig.RotateAngle, "rotateAngle");
            scaleX = ParseDouble(config.ScannerConfig.ScaleX, "scaleX");
            scaleY = ParseDouble(config.ScannerConfig.ScaleY, "scaleY");
            scaleZ = ParseDouble(config.ScannerConfig.ScaleZ, "scaleZ");

            // Загружаем флаги
            if (config.FunctionSwitcherConfig != null)
            {
                enableZCorrection = config.FunctionSwitcherConfig.EnableZCorrection;
            }
            else
            {
                enableZCorrection = false;
            }

            // Информация о карте
            if (config.CardInfo != null)
            {
                cardIpAddress = config.CardInfo.IpAddress;
            }
        }

        /// <summary>
        /// Парсит строку в double с проверкой
        /// </summary>
        private double ParseDouble(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException($"Параметр {paramName} пустой или отсутствует");

            if (!double.TryParse(value, System.Globalization.NumberStyles.Float,
                               System.Globalization.CultureInfo.InvariantCulture, out double result))
            {
                throw new InvalidDataException($"Не удалось распарсить {paramName}: '{value}'");
            }

            return result;
        }

        #endregion

        #region Основные методы расчёта

        /// <summary>
        /// Рассчитывает Z-коррекцию для заданных координат XY
        ///
        /// Формула: Z_correction = A × r² + B × r + C
        /// где r = √(X² + Y²) - расстояние от центра поля
        /// </summary>
        /// <param name="x">Координата X в мм</param>
        /// <param name="y">Координата Y в мм</param>
        /// <returns>Значение Z-коррекции в мм</returns>
        public double CalculateZCorrection(double x, double y)
        {
            if (!enableZCorrection)
                return 0.0;

            // Расстояние от центра поля
            double r = Math.Sqrt(x * x + y * y);

            // Квадратичная формула коррекции
            double zCorrection = aFactor * r * r + bFactor * r + cFactor;

            return zCorrection;
        }

        /// <summary>
        /// Полная трансформация координат (X, Y, Z) с учётом всех параметров
        /// Применяет: масштаб → поворот → смещение → Z-коррекцию
        /// </summary>
        /// <param name="inputX">Входная координата X в мм</param>
        /// <param name="inputY">Входная координата Y в мм</param>
        /// <param name="inputZ">Входная координата Z в мм</param>
        /// <returns>Финальные координаты (X, Y, Z)</returns>
        public (double x, double y, double z) TransformCoordinates(double inputX, double inputY, double inputZ = 0.0)
        {
            // Шаг 1: Применяем масштаб
            double scaledX = inputX * scaleX;
            double scaledY = inputY * scaleY;
            double scaledZ = inputZ * scaleZ;

            // Шаг 2: Применяем поворот (вокруг оси Z)
            double angleRad = rotateAngle * Math.PI / 180.0;
            double rotatedX = scaledX * Math.Cos(angleRad) - scaledY * Math.Sin(angleRad);
            double rotatedY = scaledX * Math.Sin(angleRad) + scaledY * Math.Cos(angleRad);

            // Шаг 3: Применяем смещение XY
            double offsettedX = rotatedX + offsetX;
            double offsettedY = rotatedY + offsetY;

            // Шаг 4: Применяем Z-коррекцию кривизны поля
            double zCorrection = CalculateZCorrection(offsettedX, offsettedY);
            double finalZ = scaledZ + offsetZ + zCorrection;

            return (offsettedX, offsettedY, finalZ);
        }

        /// <summary>
        /// Рассчитывает только финальное Z (упрощённая версия)
        /// </summary>
        public double CalculateFinalZ(double x, double y, double inputZ = 0.0)
        {
            var (_, _, z) = TransformCoordinates(x, y, inputZ);
            return z;
        }

        /// <summary>
        /// Генерирует таблицу Z-коррекции для анализа
        /// </summary>
        public (double radius, double zCorrection)[] GenerateCorrectionTable(double maxRadius, int steps = 20)
        {
            var table = new (double, double)[steps + 1];
            double step = maxRadius / steps;

            for (int i = 0; i <= steps; i++)
            {
                double r = i * step;
                // Создаём точку на оси X для простоты
                double z = CalculateZCorrection(r, 0);
                table[i] = (r, z);
            }

            return table;
        }

        #endregion

        #region Диагностика и вывод

        /// <summary>
        /// Выводит полную конфигурацию
        /// </summary>
        public void PrintConfiguration()
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║           Конфигурация Z-коррекции из JSON                    ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            Console.WriteLine($"IP адрес карты: {cardIpAddress}");
            Console.WriteLine($"Z-коррекция включена: {enableZCorrection}");
            Console.WriteLine();

            Console.WriteLine("=== Коэффициенты Z-коррекции (thirdAxisConfig) ===");
            Console.WriteLine($"A (квадратичный): {aFactor:F9} мм⁻¹");
            Console.WriteLine($"B (линейный):     {bFactor:F9}");
            Console.WriteLine($"C (константа):    {cFactor:F6} мм");
            Console.WriteLine();

            if (enableZCorrection)
            {
                Console.WriteLine("Формула: Z_correction = A × r² + B × r + C");
                Console.WriteLine($"         Z_correction = {aFactor:F9} × r² + {bFactor:F9} × r + ({cFactor:F6})");
                Console.WriteLine();
            }

            Console.WriteLine("=== Параметры трансформации (scannerConfig) ===");
            Console.WriteLine($"Смещение X:      {offsetX:F3} мм");
            Console.WriteLine($"Смещение Y:      {offsetY:F3} мм");
            Console.WriteLine($"Смещение Z:      {offsetZ:F6} мм");
            Console.WriteLine($"Угол поворота:   {rotateAngle:F3}°");
            Console.WriteLine($"Масштаб X:       {scaleX:F3}");
            Console.WriteLine($"Масштаб Y:       {scaleY:F3}");
            Console.WriteLine($"Масштаб Z:       {scaleZ:F3}");
        }

        /// <summary>
        /// Выводит таблицу Z-коррекции
        /// </summary>
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

                Console.WriteLine($"{radius,11:F2} | {zCorrection,16:F6} | {note}");
            }
        }

        /// <summary>
        /// Анализ диапазона Z-коррекции для поля заданного размера
        /// </summary>
        public void PrintFieldAnalysis(double fieldSizeX, double fieldSizeY)
        {
            Console.WriteLine($"\n=== Анализ кривизны поля {fieldSizeX}×{fieldSizeY} мм ===");

            // Характерные точки
            var points = new[]
            {
                (x: 0.0, y: 0.0, name: "Центр (0, 0)"),
                (x: fieldSizeX / 2, y: 0.0, name: $"Край X ({fieldSizeX / 2:F0}, 0)"),
                (x: 0.0, y: fieldSizeY / 2, name: $"Край Y (0, {fieldSizeY / 2:F0})"),
                (x: fieldSizeX / 2, y: fieldSizeY / 2, name: $"Угол ({fieldSizeX / 2:F0}, {fieldSizeY / 2:F0})")
            };

            double minZ = double.MaxValue;
            double maxZ = double.MinValue;

            Console.WriteLine("\nТочка                    | Радиус (мм) | Z-коррекция (мм)");
            Console.WriteLine("-------------------------|-------------|------------------");

            foreach (var point in points)
            {
                double r = Math.Sqrt(point.x * point.x + point.y * point.y);
                double z = CalculateZCorrection(point.x, point.y);

                minZ = Math.Min(minZ, z);
                maxZ = Math.Max(maxZ, z);

                Console.WriteLine($"{point.name,-24} | {r,11:F2} | {z,16:F6}");
            }

            double range = maxZ - minZ;
            Console.WriteLine("\n" + new string('─', 60));
            Console.WriteLine($"Минимальная Z-коррекция: {minZ:F6} мм");
            Console.WriteLine($"Максимальная Z-коррекция: {maxZ:F6} мм");
            Console.WriteLine($"Размах (глубина кривизны): {range:F6} мм = {range * 1000:F3} мкм");

            if (range > 1.0)
                Console.WriteLine("⚠ ВНИМАНИЕ: Большая кривизна поля (>1 мм), требуется Z-коррекция!");
            else if (range > 0.1)
                Console.WriteLine("⚠ Умеренная кривизна поля, рекомендуется Z-коррекция");
            else
                Console.WriteLine("✓ Небольшая кривизна поля");
        }

        #endregion

        #region Примеры использования

        /// <summary>
        /// Пример 1: Загрузка конфигурации из JSON и расчёт для одной точки
        /// </summary>
        public static void Example1_LoadAndCalculate(string jsonFilePath)
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Пример 1: Загрузка конфигурации и расчёт Z-коррекции        ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            var calculator = new ZCorrectionFromConfig(jsonFilePath);
            calculator.PrintConfiguration();

            // Тестовая точка
            double x = 100.0;
            double y = 100.0;

            Console.WriteLine($"\n=== Расчёт для точки ({x}, {y}) мм ===");

            double radius = Math.Sqrt(x * x + y * y);
            double zCorrection = calculator.CalculateZCorrection(x, y);
            double finalZ = calculator.CalculateFinalZ(x, y, inputZ: 0.0);

            Console.WriteLine($"Радиус от центра: {radius:F3} мм");
            Console.WriteLine($"Z-коррекция: {zCorrection:F6} мм");
            Console.WriteLine($"Финальная Z (с учётом offsetZ): {finalZ:F6} мм");
        }

        /// <summary>
        /// Пример 2: Полная трансформация координат
        /// </summary>
        public static void Example2_FullTransformation(string jsonFilePath)
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Пример 2: Полная трансформация координат                    ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            var calculator = new ZCorrectionFromConfig(jsonFilePath);

            double inputX = 50.0;
            double inputY = 100.0;
            double inputZ = 0.5;

            Console.WriteLine($"Входные координаты: ({inputX}, {inputY}, {inputZ}) мм\n");

            var (finalX, finalY, finalZ) = calculator.TransformCoordinates(inputX, inputY, inputZ);

            Console.WriteLine("Применённые трансформации:");
            Console.WriteLine($"  1. Масштаб: X×{calculator.ScaleX}, Y×{calculator.ScaleY}, Z×{calculator.ScaleZ}");
            Console.WriteLine($"  2. Поворот: {calculator.RotateAngle}°");
            Console.WriteLine($"  3. Смещение: +({calculator.OffsetX:F3}, {calculator.OffsetY:F3}, {calculator.OffsetZ:F6}) мм");
            Console.WriteLine($"  4. Z-коррекция: {(calculator.EnableZCorrection ? "включена" : "выключена")}");
            Console.WriteLine();
            Console.WriteLine($"Финальные координаты: ({finalX:F3}, {finalY:F3}, {finalZ:F6}) мм");
        }

        /// <summary>
        /// Пример 3: Анализ кривизны поля
        /// </summary>
        public static void Example3_FieldCurvatureAnalysis(string jsonFilePath)
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Пример 3: Анализ кривизны поля сканера                      ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            var calculator = new ZCorrectionFromConfig(jsonFilePath);

            // Анализ для поля 400×400 мм
            calculator.PrintFieldAnalysis(400.0, 400.0);

            // Таблица коррекции
            calculator.PrintCorrectionTable(200.0);
        }

        /// <summary>
        /// Пример 4: Применение к 3D траектории (цилиндр)
        /// </summary>
        public static void Example4_Apply3DTrajectory(string jsonFilePath)
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  Пример 4: Применение Z-коррекции к цилиндрической траектории║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝\n");

            var calculator = new ZCorrectionFromConfig(jsonFilePath);

            double cylinderRadius = 80.0;  // мм
            int pointsCount = 8;

            Console.WriteLine($"Цилиндр: радиус {cylinderRadius} мм, {pointsCount} точек");
            Console.WriteLine();
            Console.WriteLine("№  | Угол (°) | X (мм)   | Y (мм)   | Z без корр. | Z с корр. | Разница (мкм)");
            Console.WriteLine("---|----------|----------|----------|-------------|-----------|---------------");

            for (int i = 0; i < pointsCount; i++)
            {
                double angle = 2.0 * Math.PI * i / pointsCount;
                double x = cylinderRadius * Math.Cos(angle);
                double y = cylinderRadius * Math.Sin(angle);

                // Z для цилиндра (кривизна поверхности)
                double zCylinder = cylinderRadius * (1.0 - Math.Cos(angle));

                // Финальная Z с учётом коррекции
                double finalZ = calculator.CalculateFinalZ(x, y, zCylinder);

                double difference = (finalZ - zCylinder) * 1000.0; // в мкм

                Console.WriteLine($"{i + 1,2} | {angle * 180 / Math.PI,8:F1} | {x,8:F3} | {y,8:F3} | " +
                                  $"{zCylinder,11:F6} | {finalZ,9:F6} | {difference,13:F0}");
            }
        }

        #endregion

        #region Main

        public static void Main(string[] args)
        {
            // Создаём пример JSON конфигурации
            string exampleJson = @"[
    {
        ""cardInfo"": {
            ""ipAddress"": ""172.18.34.227"",
            ""seqIndex"": ""0""
        },
        ""scannerConfig"": {
            ""fieldSizeX"": ""400.0"",
            ""fieldSizeY"": ""400.0"",
            ""offsetX"": ""0.0"",
            ""offsetY"": ""105.03"",
            ""offsetZ"": ""-0.001"",
            ""rotateAngle"": ""0.0"",
            ""scaleX"": ""1.0"",
            ""scaleY"": ""1.0"",
            ""scaleZ"": ""1.0""
        },
        ""thirdAxisConfig"": {
            ""afactor"": ""0.0"",
            ""bfactor"": ""0.013944261"",
            ""cfactor"": ""-7.5056114""
        },
        ""functionSwitcherConfig"": {
            ""enableZCorrection"": true,
            ""enableDiameterChange"": true,
            ""enableDynamicChangeVariables"": true,
            ""enablePowerCorrection"": true
        }
    }
]";

            // Сохраняем в файл
            string tempFile = "scanner_z_correction_config.json";
            File.WriteAllText(tempFile, exampleJson);
            Console.WriteLine($"Создан тестовый файл конфигурации: {tempFile}\n");

            // Запускаем примеры
            try
            {
                Example1_LoadAndCalculate(tempFile);
                Example2_FullTransformation(tempFile);
                Example3_FieldCurvatureAnalysis(tempFile);
                Example4_Apply3DTrajectory(tempFile);

                Console.WriteLine("\n\n╔════════════════════════════════════════════════════════════════╗");
                Console.WriteLine("║                 Все примеры выполнены успешно!                ║");
                Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ ОШИБКА: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }

        #endregion
    }
}
