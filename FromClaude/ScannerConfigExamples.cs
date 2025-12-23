using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PrintMateMC.ScannerConfig.Examples
{
    #region Configuration Data Classes

    /// <summary>
    /// Основная конфигурация одной карты сканера
    /// </summary>
    public class ScannerCardConfiguration
    {
        [JsonPropertyName("cardInfo")]
        public CardInfo CardInfo { get; set; }

        [JsonPropertyName("beamConfig")]
        public BeamConfig BeamConfig { get; set; }

        [JsonPropertyName("functionSwitcherConfig")]
        public FunctionSwitcherConfig FunctionSwitcherConfig { get; set; }

        [JsonPropertyName("laserPowerConfig")]
        public LaserPowerConfig LaserPowerConfig { get; set; }

        [JsonPropertyName("processVariablesMap")]
        public ProcessVariablesMap ProcessVariablesMap { get; set; }

        [JsonPropertyName("scannerConfig")]
        public ScannerConfig ScannerConfig { get; set; }

        [JsonPropertyName("thirdAxisConfig")]
        public ThirdAxisConfig ThirdAxisConfig { get; set; }
    }

    /// <summary>
    /// Информация о сетевой карте сканера
    /// Используется для подключения к физическому оборудованию
    /// </summary>
    public class CardInfo
    {
        /// <summary>
        /// IP адрес карты сканера в локальной сети
        /// Пример: "172.18.34.227"
        /// Используется для установки TCP/IP соединения с контроллером сканера
        /// </summary>
        [JsonPropertyName("ipAddress")]
        public string IpAddress { get; set; }

        /// <summary>
        /// Порядковый индекс карты в системе (0, 1, 2...)
        /// Используется для идентификации карты при работе с несколькими лазерами
        /// </summary>
        [JsonPropertyName("seqIndex")]
        public string SeqIndex { get; set; }
    }

    /// <summary>
    /// Конфигурация лазерного луча
    /// Используется для расчетов фокусировки и диаметра пятна
    /// </summary>
    public class BeamConfig
    {
        /// <summary>
        /// Фокусное расстояние линзы в мм
        /// Пример: 538.46 мм
        /// Используется для расчета положения фокальной плоскости
        /// </summary>
        [JsonPropertyName("focalLengthMm")]
        public string FocalLengthMm { get; set; }

        /// <summary>
        /// Фактор качества луча M² (M-squared)
        /// Пример: 1.127 (близко к 1.0 = идеальный гауссов луч)
        /// Используется для расчета реального диаметра пятна
        /// Формула: d_real = d_ideal × M²
        /// </summary>
        [JsonPropertyName("m2")]
        public string M2 { get; set; }

        /// <summary>
        /// Минимальный диаметр луча в фокусе в микронах
        /// Пример: 48.141 мкм
        /// Используется для расчета разрешения и точности печати
        /// Формула: d₀ = (4 × λ × f × M²) / (π × D)
        /// где λ - длина волны, f - фокусное расстояние, D - диаметр коллимированного луча
        /// </summary>
        [JsonPropertyName("minBeamDiameterMicron")]
        public string MinBeamDiameterMicron { get; set; }

        /// <summary>
        /// Длина Рэлея в микронах (глубина фокуса)
        /// Пример: 1426.715 мкм
        /// Определяет диапазон по оси Z, в котором луч остается сфокусированным
        /// Формула: z_R = (π × d₀² × M²) / (4 × λ)
        /// Используется для расчета допустимого отклонения по высоте
        /// </summary>
        [JsonPropertyName("rayleighLengthMicron")]
        public string RayleighLengthMicron { get; set; }

        /// <summary>
        /// Длина волны лазера в нанометрах
        /// Пример: 1070.0 нм (инфракрасный иттербиевый волоконный лазер)
        /// Используется в расчетах оптических параметров
        /// </summary>
        [JsonPropertyName("wavelengthNano")]
        public string WavelengthNano { get; set; }
    }

    /// <summary>
    /// Переключатели функций - включение/выключение различных коррекций
    /// </summary>
    public class FunctionSwitcherConfig
    {
        /// <summary>
        /// Разрешить динамическое изменение диаметра луча (расфокусировку)
        /// Используется для изменения размера пятна во время печати
        /// </summary>
        [JsonPropertyName("enableDiameterChange")]
        public bool EnableDiameterChange { get; set; }

        /// <summary>
        /// Разрешить динамическое изменение параметров процесса
        /// Позволяет менять скорость, задержки и другие параметры на лету
        /// </summary>
        [JsonPropertyName("enableDynamicChangeVariables")]
        public bool EnableDynamicChangeVariables { get; set; }

        /// <summary>
        /// Включить коррекцию мощности лазера
        /// Компенсирует нелинейность характеристики лазера
        /// </summary>
        [JsonPropertyName("enablePowerCorrection")]
        public bool EnablePowerCorrection { get; set; }

        /// <summary>
        /// Включить смещение мощности (калибровочное смещение)
        /// Компенсирует систематическую ошибку измерения мощности
        /// </summary>
        [JsonPropertyName("enablePowerOffset")]
        public bool EnablePowerOffset { get; set; }

        /// <summary>
        /// Включить переменную задержку прыжков (зависит от расстояния)
        /// Оптимизирует задержки в зависимости от длины прыжка
        /// </summary>
        [JsonPropertyName("enableVariableJumpDelay")]
        public bool EnableVariableJumpDelay { get; set; }

        /// <summary>
        /// Включить коррекцию по оси Z (компенсация кривизны поля)
        /// Корректирует фокус для компенсации оптических искажений
        /// </summary>
        [JsonPropertyName("enableZCorrection")]
        public bool EnableZCorrection { get; set; }

        /// <summary>
        /// Ограничить максимальные значения параметров процесса
        /// Защита от установки опасных значений
        /// </summary>
        [JsonPropertyName("limitVariablesMaxPoint")]
        public bool LimitVariablesMaxPoint { get; set; }

        /// <summary>
        /// Ограничить минимальные значения параметров процесса
        /// </summary>
        [JsonPropertyName("limitVariablesMinPoint")]
        public bool LimitVariablesMinPoint { get; set; }
    }

    /// <summary>
    /// Конфигурация мощности лазера и коррекции
    /// </summary>
    public class LaserPowerConfig
    {
        /// <summary>
        /// Таблица коррекции мощности: реальные значения мощности
        /// Индекс: заданная мощность, Значение: фактическая мощность
        /// Пример: [0.0, 67.0, 176.0, 281.0, 382.0, 475.0]
        /// Для заданных 0%, 20%, 40%, 60%, 80%, 100%
        /// Используется для компенсации нелинейности лазера
        /// </summary>
        [JsonPropertyName("actualPowerCorrectionValue")]
        public List<string> ActualPowerCorrectionValue { get; set; }

        /// <summary>
        /// Максимальная мощность лазера в ваттах
        /// Пример: 500.0 Вт
        /// Используется для нормализации и ограничения мощности
        /// </summary>
        [JsonPropertyName("maxPower")]
        public string MaxPower { get; set; }

        /// <summary>
        /// Коэффициент C для расчета смещения мощности
        /// Формула: PowerOffset = K × Power + C
        /// Используется для калибровки датчика мощности
        /// </summary>
        [JsonPropertyName("powerOffsetCFactor")]
        public string PowerOffsetCFactor { get; set; }

        /// <summary>
        /// Коэффициент K для расчета смещения мощности
        /// Формула: PowerOffset = K × Power + C
        /// </summary>
        [JsonPropertyName("powerOffsetKFactor")]
        public string PowerOffsetKFactor { get; set; }
    }

    /// <summary>
    /// Карта параметров процесса - набор режимов печати
    /// </summary>
    public class ProcessVariablesMap
    {
        /// <summary>
        /// Наборы параметров для разных скоростей маркировки
        /// Используются для контуров, заливок и других элементов
        /// </summary>
        [JsonPropertyName("markSpeed")]
        public List<ProcessVariables> MarkSpeed { get; set; }

        /// <summary>
        /// Независимые параметры (не зависят от скорости)
        /// Используются для специальных операций
        /// </summary>
        [JsonPropertyName("nonDepends")]
        public List<ProcessVariables> NonDepends { get; set; }
    }

    /// <summary>
    /// Параметры процесса печати для одного режима
    /// </summary>
    public class ProcessVariables
    {
        /// <summary>
        /// Текущий диаметр луча в микронах
        /// Пример: 65.0 мкм
        /// Используется для установки фокуса (расфокусировки)
        /// Больше диаметр = больше расфокусировка = больше энергии на площадь
        /// </summary>
        [JsonPropertyName("curBeamDiameterMicron")]
        public string CurBeamDiameterMicron { get; set; }

        /// <summary>
        /// Текущая мощность лазера в ваттах
        /// Пример: 50.0 Вт
        /// Устанавливается перед началом сканирования
        /// </summary>
        [JsonPropertyName("curPower")]
        public string CurPower { get; set; }

        /// <summary>
        /// Задержка прыжка в наносекундах
        /// Пример: 40000 нс = 40 мкс
        /// Время ожидания после завершения прыжка перед следующей операцией
        /// Нужно для стабилизации зеркал гальваносканера
        /// </summary>
        [JsonPropertyName("jumpDelay")]
        public string JumpDelay { get; set; }

        /// <summary>
        /// Максимальная длина прыжка в мм
        /// Пример: 400.0 мм
        /// Если прыжок длиннее - разбивается на несколько
        /// Ограничивает скорость движения зеркал
        /// </summary>
        [JsonPropertyName("jumpMaxLengthLimitMm")]
        public string JumpMaxLengthLimitMm { get; set; }

        /// <summary>
        /// Скорость прыжка в мм/с
        /// Пример: 25000 мм/с = 25 м/с
        /// Скорость перемещения луча без включения лазера
        /// Ограничена инерцией зеркал гальваносканера
        /// </summary>
        [JsonPropertyName("jumpSpeed")]
        public string JumpSpeed { get; set; }

        /// <summary>
        /// Задержка выключения лазера в наносекундах (обычный режим)
        /// Пример: 490.0 нс
        /// Время между командой "выключить" и фактическим выключением
        /// Компенсирует задержку электроники и оптики
        /// </summary>
        [JsonPropertyName("laserOffDelay")]
        public string LaserOffDelay { get; set; }

        /// <summary>
        /// Задержка выключения лазера для режима SkyWriting
        /// SkyWriting - режим, когда лазер не выключается между сегментами
        /// Улучшает качество сложных траекторий
        /// </summary>
        [JsonPropertyName("laserOffDelayForSkyWriting")]
        public string LaserOffDelayForSkyWriting { get; set; }

        /// <summary>
        /// Задержка включения лазера в наносекундах (обычный режим)
        /// Пример: 420.0 нс
        /// Время между командой "включить" и достижением стабильной мощности
        /// </summary>
        [JsonPropertyName("laserOnDelay")]
        public string LaserOnDelay { get; set; }

        /// <summary>
        /// Задержка включения лазера для режима SkyWriting
        /// </summary>
        [JsonPropertyName("laserOnDelayForSkyWriting")]
        public string LaserOnDelayForSkyWriting { get; set; }

        /// <summary>
        /// Задержка маркировки в наносекундах
        /// Пример: 470 нс
        /// Дополнительная пауза перед началом маркировки
        /// Используется для синхронизации
        /// </summary>
        [JsonPropertyName("markDelay")]
        public string MarkDelay { get; set; }

        /// <summary>
        /// Скорость маркировки (сканирования) в мм/с
        /// Пример: 800, 1250, 2000 мм/с
        /// Скорость движения луча при включенном лазере
        /// Определяет производительность и качество
        /// Меньше скорость = больше энергии на единицу длины
        /// </summary>
        [JsonPropertyName("markSpeed")]
        public string MarkSpeed { get; set; }

        /// <summary>
        /// Минимальная задержка прыжка в наносекундах
        /// Пример: 400 нс
        /// Даже для коротких прыжков должна быть минимальная пауза
        /// </summary>
        [JsonPropertyName("minJumpDelay")]
        public string MinJumpDelay { get; set; }

        /// <summary>
        /// Задержка полигона в наносекундах
        /// Пример: 385 нс
        /// Пауза между сегментами полилинии (многоугольника)
        /// Уменьшает "закругление" углов
        /// </summary>
        [JsonPropertyName("polygonDelay")]
        public string PolygonDelay { get; set; }

        /// <summary>
        /// Включить режим SkyWriting
        /// true - лазер не выключается между короткими сегментами
        /// false - лазер выключается после каждого сегмента
        /// </summary>
        [JsonPropertyName("swenable")]
        public bool SWEnable { get; set; }

        /// <summary>
        /// Параметр umax - максимальное отклонение траектории (в мм?)
        /// Используется в алгоритмах сглаживания
        /// </summary>
        [JsonPropertyName("umax")]
        public string Umax { get; set; }
    }

    /// <summary>
    /// Конфигурация сканера (поле сканирования и калибровка)
    /// </summary>
    public class ScannerConfig
    {
        /// <summary>
        /// Код типа системы координат
        /// Пример: "5"
        /// Определяет систему координат сканера
        /// </summary>
        [JsonPropertyName("coordinateTypeCode")]
        public string CoordinateTypeCode { get; set; }

        /// <summary>
        /// Размер поля сканирования по X в мм
        /// Пример: 400.0 мм
        /// Максимальный диапазон перемещения луча по оси X
        /// Определяется углом отклонения зеркал и фокусным расстоянием
        /// </summary>
        [JsonPropertyName("fieldSizeX")]
        public string FieldSizeX { get; set; }

        /// <summary>
        /// Размер поля сканирования по Y в мм
        /// Пример: 400.0 мм
        /// Максимальный диапазон перемещения луча по оси Y
        /// </summary>
        [JsonPropertyName("fieldSizeY")]
        public string FieldSizeY { get; set; }

        /// <summary>
        /// Смещение по оси X в мм
        /// Пример: 0.0 мм (для карты 0), -2.636 мм (для карты 1)
        /// Калибровочное смещение центра поля сканирования
        /// Используется для выравнивания нескольких лазеров
        /// Применяется к каждой координате: X_real = X_command + offsetX
        /// </summary>
        [JsonPropertyName("offsetX")]
        public string OffsetX { get; set; }

        /// <summary>
        /// Смещение по оси Y в мм
        /// Пример: 105.03 мм (для карты 0), -105.03 мм (для карты 1)
        /// Используется для позиционирования нескольких лазеров
        /// В данном случае лазеры расположены на расстоянии 210 мм друг от друга
        /// </summary>
        [JsonPropertyName("offsetY")]
        public string OffsetY { get; set; }

        /// <summary>
        /// Смещение по оси Z в мм
        /// Пример: -0.001 мм, 0.102 мм
        /// Калибровка фокуса для компенсации механических допусков
        /// Положительное значение = фокус ниже, отрицательное = выше
        /// </summary>
        [JsonPropertyName("offsetZ")]
        public string OffsetZ { get; set; }

        /// <summary>
        /// Код протокола связи
        /// Пример: "1"
        /// Определяет протокол коммуникации с картой сканера
        /// </summary>
        [JsonPropertyName("protocolCode")]
        public string ProtocolCode { get; set; }

        /// <summary>
        /// Угол поворота поля сканирования в градусах
        /// Пример: 0.0°
        /// Используется для компенсации механического наклона сканера
        /// Применяется матрица поворота к координатам
        /// </summary>
        [JsonPropertyName("rotateAngle")]
        public string RotateAngle { get; set; }

        /// <summary>
        /// Масштабный коэффициент по оси X
        /// Пример: 1.0 (без масштабирования)
        /// X_real = X_command × scaleX
        /// Используется для калибровки (если реальное поле отличается от номинального)
        /// </summary>
        [JsonPropertyName("scaleX")]
        public string ScaleX { get; set; }

        /// <summary>
        /// Масштабный коэффициент по оси Y
        /// Пример: 1.0
        /// Y_real = Y_command × scaleY
        /// </summary>
        [JsonPropertyName("scaleY")]
        public string ScaleY { get; set; }

        /// <summary>
        /// Масштабный коэффициент по оси Z
        /// Пример: 1.0
        /// Z_real = Z_command × scaleZ
        /// </summary>
        [JsonPropertyName("scaleZ")]
        public string ScaleZ { get; set; }
    }

    /// <summary>
    /// Конфигурация третьей оси (ось Z - фокус)
    /// Используется для коррекции кривизны поля (field curvature correction)
    /// </summary>
    public class ThirdAxisConfig
    {
        /// <summary>
        /// Коэффициент A квадратичной коррекции
        /// Формула: Z_correction = A × r² + B × r + C
        /// где r = sqrt(X² + Y²) - расстояние от центра поля
        /// Пример: 0.0 (обычно не используется)
        /// </summary>
        [JsonPropertyName("afactor")]
        public string AFactor { get; set; }

        /// <summary>
        /// Коэффициент B линейной коррекции
        /// Пример: 0.013944261
        /// Компенсирует линейную составляющую кривизны поля
        /// Для точки на расстоянии 100 мм: Z += 0.014 × 100 = 1.4 мм
        /// </summary>
        [JsonPropertyName("bfactor")]
        public string BFactor { get; set; }

        /// <summary>
        /// Коэффициент C постоянного смещения
        /// Пример: -7.5056114 мм
        /// Базовое смещение фокуса
        /// Компенсирует систематическую ошибку фокусировки
        /// </summary>
        [JsonPropertyName("cfactor")]
        public string CFactor { get; set; }
    }

    #endregion

    #region Example Usage Classes

    /// <summary>
    /// Примеры использования конфигурации сканера
    /// </summary>
    public class ScannerConfigExamples
    {
        /// <summary>
        /// Пример 1: Загрузка и парсинг конфигурации из JSON
        /// </summary>
        public static void Example1_LoadConfiguration(string jsonFilePath)
        {
            Console.WriteLine("=== Пример 1: Загрузка конфигурации ===\n");

            // Чтение JSON файла
            string jsonContent = System.IO.File.ReadAllText(jsonFilePath);

            // Десериализация в массив конфигураций (для нескольких карт)
            var configs = JsonSerializer.Deserialize<List<ScannerCardConfiguration>>(jsonContent);

            Console.WriteLine($"Загружено конфигураций: {configs.Count}");

            foreach (var config in configs)
            {
                Console.WriteLine($"\nКарта #{config.CardInfo.SeqIndex}:");
                Console.WriteLine($"  IP адрес: {config.CardInfo.IpAddress}");
                Console.WriteLine($"  Фокусное расстояние: {config.BeamConfig.FocalLengthMm} мм");
                Console.WriteLine($"  Минимальный диаметр луча: {config.BeamConfig.MinBeamDiameterMicron} мкм");
                Console.WriteLine($"  Размер поля: {config.ScannerConfig.FieldSizeX} × {config.ScannerConfig.FieldSizeY} мм");
                Console.WriteLine($"  Смещение: X={config.ScannerConfig.OffsetX}, Y={config.ScannerConfig.OffsetY}, Z={config.ScannerConfig.OffsetZ} мм");
            }
        }

        /// <summary>
        /// Пример 2: Расчет реального диаметра луча в зависимости от расфокусировки
        /// </summary>
        public static void Example2_CalculateBeamDiameter(BeamConfig beamConfig, double zOffset)
        {
            Console.WriteLine("\n=== Пример 2: Расчет диаметра луча ===\n");

            double minDiameter = double.Parse(beamConfig.MinBeamDiameterMicron);
            double rayleighLength = double.Parse(beamConfig.RayleighLengthMicron);
            double m2 = double.Parse(beamConfig.M2);

            // Формула расчета диаметра луча на расстоянии z от фокуса:
            // d(z) = d₀ × sqrt(1 + (z / z_R)²)
            // где d₀ - минимальный диаметр, z_R - длина Рэлея

            double diameter = minDiameter * Math.Sqrt(1 + Math.Pow(zOffset / rayleighLength, 2));

            Console.WriteLine($"Минимальный диаметр луча (d₀): {minDiameter:F3} мкм");
            Console.WriteLine($"Длина Рэлея (z_R): {rayleighLength:F3} мкм");
            Console.WriteLine($"Фактор M²: {m2}");
            Console.WriteLine($"Смещение от фокуса (z): {zOffset:F3} мкм");
            Console.WriteLine($"Результирующий диаметр луча: {diameter:F3} мкм");

            // Для z = ±z_R диаметр увеличивается в sqrt(2) ≈ 1.414 раза
            if (Math.Abs(zOffset) >= rayleighLength)
            {
                Console.WriteLine($"ВНИМАНИЕ: Смещение превышает длину Рэлея! Луч значительно расфокусирован.");
            }
        }

        /// <summary>
        /// Пример 3: Расчет коррекции мощности лазера
        /// </summary>
        public static double Example3_CalculatePowerCorrection(LaserPowerConfig powerConfig, double requestedPower)
        {
            Console.WriteLine("\n=== Пример 3: Коррекция мощности ===\n");

            double maxPower = double.Parse(powerConfig.MaxPower);
            var correctionTable = powerConfig.ActualPowerCorrectionValue;

            // Нормализуем запрошенную мощность (0.0 - 1.0)
            double normalizedPower = requestedPower / maxPower;

            Console.WriteLine($"Запрошенная мощность: {requestedPower} Вт ({normalizedPower * 100:F1}%)");
            Console.WriteLine($"Максимальная мощность: {maxPower} Вт");

            // Интерполяция по таблице коррекции
            // Таблица: [0%, 20%, 40%, 60%, 80%, 100%] = 6 точек
            int tableSize = correctionTable.Count;
            double index = normalizedPower * (tableSize - 1);
            int lowerIndex = (int)Math.Floor(index);
            int upperIndex = (int)Math.Ceiling(index);

            if (lowerIndex >= tableSize - 1)
            {
                lowerIndex = tableSize - 1;
                upperIndex = tableSize - 1;
            }

            double lowerValue = double.Parse(correctionTable[lowerIndex]);
            double upperValue = double.Parse(correctionTable[upperIndex]);
            double fraction = index - lowerIndex;

            double correctedPower = lowerValue + (upperValue - lowerValue) * fraction;

            Console.WriteLine($"Индекс в таблице: {index:F3} (между {lowerIndex} и {upperIndex})");
            Console.WriteLine($"Нижнее значение: {lowerValue} Вт");
            Console.WriteLine($"Верхнее значение: {upperValue} Вт");
            Console.WriteLine($"Скорректированная мощность: {correctedPower:F2} Вт");

            // Применяем смещение мощности
            double kFactor = double.Parse(powerConfig.PowerOffsetKFactor);
            double cFactor = double.Parse(powerConfig.PowerOffsetCFactor);
            double powerOffset = kFactor * correctedPower + cFactor;

            Console.WriteLine($"\nСмещение мощности:");
            Console.WriteLine($"  K-фактор: {kFactor}");
            Console.WriteLine($"  C-фактор: {cFactor}");
            Console.WriteLine($"  Offset = {kFactor} × {correctedPower:F2} + {cFactor} = {powerOffset:F2}");
            Console.WriteLine($"  Финальная мощность: {correctedPower + powerOffset:F2} Вт");

            return correctedPower + powerOffset;
        }

        /// <summary>
        /// Пример 4: Трансформация координат с учетом смещений и масштаба
        /// </summary>
        public static (double x, double y, double z) Example4_TransformCoordinates(
            ScannerConfig scannerConfig,
            ThirdAxisConfig thirdAxisConfig,
            double inputX,
            double inputY,
            double inputZ)
        {
            Console.WriteLine("\n=== Пример 4: Трансформация координат ===\n");

            Console.WriteLine($"Входные координаты: X={inputX}, Y={inputY}, Z={inputZ}");

            // Шаг 1: Применяем масштаб
            double scaleX = double.Parse(scannerConfig.ScaleX);
            double scaleY = double.Parse(scannerConfig.ScaleY);
            double scaleZ = double.Parse(scannerConfig.ScaleZ);

            double scaledX = inputX * scaleX;
            double scaledY = inputY * scaleY;
            double scaledZ = inputZ * scaleZ;

            Console.WriteLine($"После масштабирования: X={scaledX}, Y={scaledY}, Z={scaledZ}");

            // Шаг 2: Применяем поворот
            double rotateAngle = double.Parse(scannerConfig.RotateAngle);
            double angleRad = rotateAngle * Math.PI / 180.0;

            double rotatedX = scaledX * Math.Cos(angleRad) - scaledY * Math.Sin(angleRad);
            double rotatedY = scaledX * Math.Sin(angleRad) + scaledY * Math.Cos(angleRad);

            if (rotateAngle != 0)
            {
                Console.WriteLine($"После поворота на {rotateAngle}°: X={rotatedX:F3}, Y={rotatedY:F3}");
            }

            // Шаг 3: Применяем смещения
            double offsetX = double.Parse(scannerConfig.OffsetX);
            double offsetY = double.Parse(scannerConfig.OffsetY);
            double offsetZ = double.Parse(scannerConfig.OffsetZ);

            double finalX = rotatedX + offsetX;
            double finalY = rotatedY + offsetY;

            Console.WriteLine($"После смещения XY: X={finalX:F3}, Y={finalY:F3}");

            // Шаг 4: Коррекция Z (кривизна поля)
            double aFactor = double.Parse(thirdAxisConfig.AFactor);
            double bFactor = double.Parse(thirdAxisConfig.BFactor);
            double cFactor = double.Parse(thirdAxisConfig.CFactor);

            // Расстояние от центра поля
            double r = Math.Sqrt(finalX * finalX + finalY * finalY);

            // Z_correction = A × r² + B × r + C
            double zCorrection = aFactor * r * r + bFactor * r + cFactor;
            double finalZ = scaledZ + offsetZ + zCorrection;

            Console.WriteLine($"\nКоррекция Z:");
            Console.WriteLine($"  Расстояние от центра (r): {r:F3} мм");
            Console.WriteLine($"  A × r²: {aFactor * r * r:F3}");
            Console.WriteLine($"  B × r: {bFactor * r:F3}");
            Console.WriteLine($"  C: {cFactor:F3}");
            Console.WriteLine($"  Суммарная коррекция Z: {zCorrection:F3} мм");
            Console.WriteLine($"  Финальная Z: {finalZ:F3} мм");

            Console.WriteLine($"\nИтоговые координаты: X={finalX:F3}, Y={finalY:F3}, Z={finalZ:F3}");

            return (finalX, finalY, finalZ);
        }

        /// <summary>
        /// Пример 5: Выбор параметров процесса в зависимости от скорости
        /// </summary>
        public static ProcessVariables Example5_SelectProcessVariables(
            ProcessVariablesMap variablesMap,
            double desiredMarkSpeed)
        {
            Console.WriteLine("\n=== Пример 5: Выбор параметров процесса ===\n");

            Console.WriteLine($"Желаемая скорость маркировки: {desiredMarkSpeed} мм/с");

            // Ищем ближайший набор параметров
            ProcessVariables selectedVariables = null;
            double minDifference = double.MaxValue;

            foreach (var variables in variablesMap.MarkSpeed)
            {
                double speed = double.Parse(variables.MarkSpeed);
                double difference = Math.Abs(speed - desiredMarkSpeed);

                Console.WriteLine($"  Проверяем набор со скоростью {speed} мм/с (разница: {difference})");

                if (difference < minDifference)
                {
                    minDifference = difference;
                    selectedVariables = variables;
                }
            }

            if (selectedVariables != null)
            {
                Console.WriteLine($"\nВыбран набор параметров:");
                Console.WriteLine($"  Скорость маркировки: {selectedVariables.MarkSpeed} мм/с");
                Console.WriteLine($"  Скорость прыжка: {selectedVariables.JumpSpeed} мм/с");
                Console.WriteLine($"  Мощность: {selectedVariables.CurPower} Вт");
                Console.WriteLine($"  Диаметр луча: {selectedVariables.CurBeamDiameterMicron} мкм");
                Console.WriteLine($"  Задержка включения лазера: {selectedVariables.LaserOnDelay} нс");
                Console.WriteLine($"  Задержка выключения лазера: {selectedVariables.LaserOffDelay} нс");
                Console.WriteLine($"  Задержка прыжка: {selectedVariables.JumpDelay} нс");
                Console.WriteLine($"  Задержка полигона: {selectedVariables.PolygonDelay} нс");
                Console.WriteLine($"  SkyWriting: {(selectedVariables.SWEnable ? "включен" : "выключен")}");
            }

            return selectedVariables;
        }

        /// <summary>
        /// Пример 6: Расчет времени выполнения операций
        /// </summary>
        public static void Example6_CalculateExecutionTime(ProcessVariables variables,
            double markingLength, double jumpLength, int numberOfSegments)
        {
            Console.WriteLine("\n=== Пример 6: Расчет времени выполнения ===\n");

            double markSpeed = double.Parse(variables.MarkSpeed);
            double jumpSpeed = double.Parse(variables.JumpSpeed);
            double laserOnDelay = double.Parse(variables.LaserOnDelay) / 1_000_000.0; // нс -> мс
            double laserOffDelay = double.Parse(variables.LaserOffDelay) / 1_000_000.0;
            double jumpDelay = double.Parse(variables.JumpDelay) / 1_000_000.0;
            double polygonDelay = double.Parse(variables.PolygonDelay) / 1_000_000.0;
            double markDelay = double.Parse(variables.MarkDelay) / 1_000_000.0;

            Console.WriteLine($"Параметры:");
            Console.WriteLine($"  Длина маркировки: {markingLength} мм");
            Console.WriteLine($"  Длина прыжка: {jumpLength} мм");
            Console.WriteLine($"  Количество сегментов: {numberOfSegments}");

            // Время маркировки одного сегмента
            double markingTime = (markingLength / markSpeed) * 1000.0; // -> мс

            // Время прыжка
            double jumpTime = (jumpLength / jumpSpeed) * 1000.0; // -> мс

            // Суммарное время задержек на один сегмент
            double delaysPerSegment = laserOnDelay + laserOffDelay + markDelay + polygonDelay;

            // Задержка прыжка добавляется только между сегментами
            double totalTime = numberOfSegments * (markingTime + delaysPerSegment) +
                              (numberOfSegments - 1) * (jumpTime + jumpDelay);

            Console.WriteLine($"\nВремя выполнения:");
            Console.WriteLine($"  Время маркировки одного сегмента: {markingTime:F3} мс");
            Console.WriteLine($"  Время прыжка: {jumpTime:F3} мс");
            Console.WriteLine($"  Задержки на сегмент: {delaysPerSegment:F3} мс");
            Console.WriteLine($"    - Включение лазера: {laserOnDelay:F3} мс");
            Console.WriteLine($"    - Выключение лазера: {laserOffDelay:F3} мс");
            Console.WriteLine($"    - Задержка маркировки: {markDelay:F3} мс");
            Console.WriteLine($"    - Задержка полигона: {polygonDelay:F3} мс");
            Console.WriteLine($"  Задержка прыжка: {jumpDelay:F3} мс");
            Console.WriteLine($"\nОбщее время выполнения: {totalTime:F3} мс = {totalTime / 1000.0:F3} сек");

            if (variables.SWEnable)
            {
                // В режиме SkyWriting используются другие задержки
                double swLaserOnDelay = double.Parse(variables.LaserOnDelayForSkyWriting) / 1_000_000.0;
                double swLaserOffDelay = double.Parse(variables.LaserOffDelayForSkyWriting) / 1_000_000.0;
                double swDelaysPerSegment = swLaserOnDelay + swLaserOffDelay + markDelay + polygonDelay;
                double swTotalTime = numberOfSegments * (markingTime + swDelaysPerSegment) +
                                    (numberOfSegments - 1) * (jumpTime + jumpDelay);

                Console.WriteLine($"\nВремя с SkyWriting: {swTotalTime:F3} мс = {swTotalTime / 1000.0:F3} сек");
                Console.WriteLine($"Экономия времени: {totalTime - swTotalTime:F3} мс ({(1 - swTotalTime / totalTime) * 100:F1}%)");
            }
        }

        /// <summary>
        /// Пример 7: Проверка корректности конфигурации
        /// </summary>
        public static void Example7_ValidateConfiguration(ScannerCardConfiguration config)
        {
            Console.WriteLine("\n=== Пример 7: Валидация конфигурации ===\n");

            bool isValid = true;
            var errors = new List<string>();

            // Проверка IP адреса
            if (!System.Net.IPAddress.TryParse(config.CardInfo.IpAddress, out _))
            {
                errors.Add($"Некорректный IP адрес: {config.CardInfo.IpAddress}");
                isValid = false;
            }

            // Проверка фокусного расстояния
            double focalLength = double.Parse(config.BeamConfig.FocalLengthMm);
            if (focalLength <= 0)
            {
                errors.Add($"Фокусное расстояние должно быть положительным: {focalLength}");
                isValid = false;
            }

            // Проверка M²
            double m2 = double.Parse(config.BeamConfig.M2);
            if (m2 < 1.0)
            {
                errors.Add($"Фактор M² не может быть меньше 1.0: {m2}");
                isValid = false;
            }

            // Проверка размера поля
            double fieldX = double.Parse(config.ScannerConfig.FieldSizeX);
            double fieldY = double.Parse(config.ScannerConfig.FieldSizeY);
            if (fieldX <= 0 || fieldY <= 0)
            {
                errors.Add($"Размер поля должен быть положительным: {fieldX} × {fieldY}");
                isValid = false;
            }

            // Проверка максимальной мощности
            double maxPower = double.Parse(config.LaserPowerConfig.MaxPower);
            if (maxPower <= 0)
            {
                errors.Add($"Максимальная мощность должна быть положительной: {maxPower}");
                isValid = false;
            }

            // Проверка параметров процесса
            foreach (var variables in config.ProcessVariablesMap.MarkSpeed)
            {
                double markSpeed = double.Parse(variables.MarkSpeed);
                double jumpSpeed = double.Parse(variables.JumpSpeed);
                double power = double.Parse(variables.CurPower);

                if (markSpeed <= 0 || markSpeed > 10000)
                {
                    errors.Add($"Подозрительная скорость маркировки: {markSpeed} мм/с");
                }

                if (jumpSpeed <= 0 || jumpSpeed > 50000)
                {
                    errors.Add($"Подозрительная скорость прыжка: {jumpSpeed} мм/с");
                }

                if (power < 0 || power > maxPower)
                {
                    errors.Add($"Мощность {power} Вт выходит за пределы 0-{maxPower} Вт");
                    isValid = false;
                }
            }

            if (isValid)
            {
                Console.WriteLine("✓ Конфигурация корректна");
            }
            else
            {
                Console.WriteLine("✗ Обнаружены ошибки в конфигурации:");
                foreach (var error in errors)
                {
                    Console.WriteLine($"  - {error}");
                }
            }
        }

        /// <summary>
        /// Пример 8: Расчет плотности энергии
        /// </summary>
        public static void Example8_CalculateEnergyDensity(ProcessVariables variables, BeamConfig beamConfig)
        {
            Console.WriteLine("\n=== Пример 8: Расчет плотности энергии ===\n");

            double power = double.Parse(variables.CurPower); // Вт
            double markSpeed = double.Parse(variables.MarkSpeed); // мм/с
            double beamDiameter = double.Parse(variables.CurBeamDiameterMicron); // мкм

            // Площадь пятна (круг)
            double beamRadius = beamDiameter / 2.0 / 1000.0; // мкм -> мм
            double spotArea = Math.PI * beamRadius * beamRadius; // мм²

            // Интенсивность (плотность мощности)
            double intensity = power / spotArea; // Вт/мм²

            // Линейная плотность энергии
            double linearEnergyDensity = power / markSpeed; // Дж/мм

            // Площадная плотность энергии (флюенс)
            double fluence = linearEnergyDensity / beamDiameter * 1000.0; // Дж/мм²

            Console.WriteLine($"Входные параметры:");
            Console.WriteLine($"  Мощность: {power} Вт");
            Console.WriteLine($"  Скорость: {markSpeed} мм/с");
            Console.WriteLine($"  Диаметр луча: {beamDiameter} мкм");

            Console.WriteLine($"\nРезультаты:");
            Console.WriteLine($"  Площадь пятна: {spotArea:F6} мм² = {spotArea * 1e6:F3} мкм²");
            Console.WriteLine($"  Интенсивность: {intensity:F2} Вт/мм² = {intensity / 10000.0:F2} Вт/см²");
            Console.WriteLine($"  Линейная плотность энергии: {linearEnergyDensity:F3} Дж/мм");
            Console.WriteLine($"  Флюенс (площадная плотность): {fluence:F3} Дж/мм²");

            // Сравнение с типичными значениями для металлов
            Console.WriteLine($"\nСправочная информация:");
            Console.WriteLine($"  Типичная плотность энергии для плавления металлов: 0.5-5 Дж/мм²");
            Console.WriteLine($"  Типичная плотность для испарения: 5-50 Дж/мм²");

            if (fluence < 0.5)
                Console.WriteLine($"  ⚠ Низкая плотность энергии - материал может не расплавиться");
            else if (fluence > 50)
                Console.WriteLine($"  ⚠ Высокая плотность энергии - возможно интенсивное испарение");
            else
                Console.WriteLine($"  ✓ Плотность энергии в нормальном диапазоне");
        }
    }

    #endregion

    #region Main Program

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            // Пример JSON конфигурации (укороченный для демонстрации)
            string exampleJson = @"
[
    {
        ""cardInfo"": {
            ""ipAddress"": ""172.18.34.227"",
            ""seqIndex"": ""0""
        },
        ""beamConfig"": {
            ""focalLengthMm"": ""538.46"",
            ""m2"": ""1.127"",
            ""minBeamDiameterMicron"": ""48.141"",
            ""rayleighLengthMicron"": ""1426.715"",
            ""wavelengthNano"": ""1070.0""
        },
        ""laserPowerConfig"": {
            ""actualPowerCorrectionValue"": [""0.0"", ""67.0"", ""176.0"", ""281.0"", ""382.0"", ""475.0""],
            ""maxPower"": ""500.0"",
            ""powerOffsetCFactor"": ""51.298943"",
            ""powerOffsetKFactor"": ""-0.6839859""
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
            ""scaleZ"": ""1.0"",
            ""coordinateTypeCode"": ""5"",
            ""protocolCode"": ""1""
        },
        ""thirdAxisConfig"": {
            ""afactor"": ""0.0"",
            ""bfactor"": ""0.013944261"",
            ""cfactor"": ""-7.5056114""
        },
        ""processVariablesMap"": {
            ""markSpeed"": [
                {
                    ""curBeamDiameterMicron"": ""65.0"",
                    ""curPower"": ""50.0"",
                    ""jumpDelay"": ""40000"",
                    ""jumpMaxLengthLimitMm"": ""400.0"",
                    ""jumpSpeed"": ""25000"",
                    ""laserOffDelay"": ""490.0"",
                    ""laserOffDelayForSkyWriting"": ""730.0"",
                    ""laserOnDelay"": ""420.0"",
                    ""laserOnDelayForSkyWriting"": ""600.0"",
                    ""markDelay"": ""470"",
                    ""markSpeed"": ""800"",
                    ""minJumpDelay"": ""400"",
                    ""polygonDelay"": ""385"",
                    ""swenable"": true,
                    ""umax"": ""0.1""
                },
                {
                    ""curBeamDiameterMicron"": ""65.0"",
                    ""curPower"": ""50.0"",
                    ""jumpDelay"": ""40000"",
                    ""jumpMaxLengthLimitMm"": ""400.0"",
                    ""jumpSpeed"": ""25000"",
                    ""laserOffDelay"": ""500.0"",
                    ""laserOffDelayForSkyWriting"": ""725.0"",
                    ""laserOnDelay"": ""375.0"",
                    ""laserOnDelayForSkyWriting"": ""615.0"",
                    ""markDelay"": ""496"",
                    ""markSpeed"": ""1250"",
                    ""minJumpDelay"": ""400"",
                    ""polygonDelay"": ""465"",
                    ""swenable"": true,
                    ""umax"": ""0.1""
                }
            ],
            ""nonDepends"": []
        },
        ""functionSwitcherConfig"": {
            ""enableDiameterChange"": true,
            ""enableDynamicChangeVariables"": true,
            ""enablePowerCorrection"": true,
            ""enablePowerOffset"": true,
            ""enableVariableJumpDelay"": true,
            ""enableZCorrection"": true,
            ""limitVariablesMaxPoint"": true,
            ""limitVariablesMinPoint"": true
        }
    }
]";

            // Сохраняем пример в файл
            string tempFile = "scanner_config_example.json";
            System.IO.File.WriteAllText(tempFile, exampleJson);

            // Загружаем конфигурацию
            var configs = JsonSerializer.Deserialize<List<ScannerCardConfiguration>>(exampleJson);
            var config = configs[0];

            // Выполняем примеры
            ScannerConfigExamples.Example1_LoadConfiguration(tempFile);

            ScannerConfigExamples.Example2_CalculateBeamDiameter(
                config.BeamConfig, zOffset: 1000.0);

            ScannerConfigExamples.Example3_CalculatePowerCorrection(
                config.LaserPowerConfig, requestedPower: 250.0);

            ScannerConfigExamples.Example4_TransformCoordinates(
                config.ScannerConfig,
                config.ThirdAxisConfig,
                inputX: 50.0,
                inputY: 100.0,
                inputZ: 0.0);

            var selectedVariables = ScannerConfigExamples.Example5_SelectProcessVariables(
                config.ProcessVariablesMap,
                desiredMarkSpeed: 1200);

            if (selectedVariables != null)
            {
                ScannerConfigExamples.Example6_CalculateExecutionTime(
                    selectedVariables,
                    markingLength: 10.0,
                    jumpLength: 5.0,
                    numberOfSegments: 100);

                ScannerConfigExamples.Example8_CalculateEnergyDensity(
                    selectedVariables,
                    config.BeamConfig);
            }

            ScannerConfigExamples.Example7_ValidateConfiguration(config);

            Console.WriteLine("\n\n=== Все примеры выполнены ===");
            Console.WriteLine("Нажмите Enter для выхода...");
            Console.ReadLine();
        }
    }

    #endregion
}
