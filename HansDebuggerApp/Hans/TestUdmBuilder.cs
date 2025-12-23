using Hans.NET.libs;
using Hans.NET.Models;
using System;
using System.IO;
using System.Text;
using static Hans.NET.libs.HM_UDM_DLL;

namespace HansDebuggerApp.Hans
{
    /// <summary>
    /// Тестовый UDM Builder - ТОЧНЫЙ ПОРТ Java логики из UdmProducer.java
    ///
    /// Основан на: hans-dev/Hans4Java/src/org/iiv/hlssystem/multi/UdmProducer.java
    /// и hans-dev/Hans4Java/src/org/iiv/hans4java/controlCardProfiles/BeamConfig.java
    ///
    /// ВАЖНО: НЕТ интерполяции диаметра пучка!
    /// Диаметр устанавливается один раз (как DIAMETER operation в Java) и используется для всех точек
    /// </summary>
    public class TestUdmBuilder
    {
        private readonly ScanatorConfiguration _config;
        private readonly string _outputDirectory;

        // Текущее состояние (как в Java: cardProfile.beamConfig.curBeamDiameterMicron и curPower)
        private double _currentBeamDiameterMicron;
        private double _currentPowerWatts;

        // Для отладки и логирования
        public static double FocalLengthMm;
        public static double FocalLengthMicron;
        public static double LensTravelMicron;
        public static double PowerOffsetMicrons;
        public static double ZFinal;

        public TestUdmBuilder(ScanatorConfiguration config, string outputDirectory = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _outputDirectory = outputDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UdmBinFiles");

            // Инициализируем текущее состояние значениями по умолчанию
            _currentBeamDiameterMicron = config.BeamConfig.MinBeamDiameterMicron;
            _currentPowerWatts = config.LaserPowerConfig.MaxPower * 0.5;

            if (!Directory.Exists(_outputDirectory))
                Directory.CreateDirectory(_outputDirectory);
        }

        /// <summary>
        /// Прямая функция: Желаемый диаметр → SDK диаметр (для компенсации систематической ошибки)
        /// </summary>
        /// <param name="desiredDiameterMicron">Желаемый реальный диаметр в микронах</param>
        /// <returns>Диаметр для передачи в Hans SDK</returns>
        public static double CorrectDiameterPrecise(double desiredDiameterMicron)
        {
            const double K = 1.19760479;   // 1 / 0.835
            const double B = -0.17964072;  // -0.15 / 0.835

            return K * desiredDiameterMicron + B;
        }

        /// <summary>
        /// Обратная функция: SDK диаметр → Реальный диаметр
        /// </summary>
        /// <param name="sdkDiameterMicron">Диаметр из Hans SDK</param>
        /// <returns>Реальный диаметр в микронах</returns>
        public static double GetRealDiameterFromSDK(double sdkDiameterMicron)
        {
            const double K = 1.19760479;   // 1 / 0.835
            const double B = -0.17964072;  // -0.15 / 0.835

            // Обратная формула: desiredDiameter = (sdkDiameter - B) / K
            return (sdkDiameterMicron - B) / K;
        }



        /// <summary>
        /// Создает UDM файл для прожига одной точки
        /// </summary>
        /// <param name="x">Координата X в мм</param>
        /// <param name="y">Координата Y в мм</param>
        /// <param name="beamDiameterMicron">Диаметр пучка в микронах</param>
        /// <param name="powerWatts">Мощность лазера в ваттах</param>
        /// <param name="dwellTimeMs">Время экспозиции в миллисекундах</param>
        public string BuildSinglePoint(
            float x,
            float y,
            double beamDiameterMicron,
            float powerWatts,
            int dwellTimeMs = 500)
        {
            Console.WriteLine("=== Тест UDM Builder (Java порт) ===");
            Console.WriteLine($"Входные параметры:");
            Console.WriteLine($"  Позиция: X={x:F3} мм, Y={y:F3} мм");
            Console.WriteLine($"  Диаметр пучка: {beamDiameterMicron:F1} мкм");
            Console.WriteLine($"  Мощность лазера: {powerWatts:F1} Вт");
            Console.WriteLine($"  Время экспозиции: {dwellTimeMs} мс");
            Console.WriteLine();

            // Обновляем текущее состояние (эквивалент DIAMETER и POWER операций в Java)
            UpdateCurrentState(beamDiameterMicron, powerWatts);

            // 1. Создаем новый UDM файл
            string udmFilePath = Path.Combine(_outputDirectory, $"SinglePoint_{Guid.NewGuid()}.bin");

            if (UDM_NewFile() != 0)
                throw new InvalidOperationException("Не удалось создать UDM файл");

            // 2. Инициализируем UDM (как в Java UdmProducer.setOpsBefore() line 285-295)
            UDM_Main();
            UDM_SkyWriting(1);  // Всегда включаем SkyWriting
            UDM_SetProtocol(1, 1);  // XY2_100_PROTOCOL_INDEX=1, DIMENSIONAL_3D_INDEX=1
            double[] paraK = _config.ThirdAxisConfig.CorrectionPolynomial;
            int correctionResult = UDM_Set3dCorrectionPara((float)_config.BeamConfig.FocalLengthMm, paraK, paraK.Length);


            // 3. Применяем трансформации сканера (offset, rotate)
            ApplyScannerTransforms();

            // 4. Вычисляем параметры маркировки
            var markParams = CalculateMarkParameters(powerWatts, dwellTimeMs);

            // 5. Устанавливаем параметры слоя
            MarkParameter[] parameters = new MarkParameter[] { markParams };
            UDM_SetLayersPara(parameters, 1);

            // 6. Вычисляем Z-координату (ТОЧНЫЙ ПОРТ Java BeamConfig.getCorrectZValue)
            float zCoord = GetCorrectZValue(x, y, 0.0f);  // coordZMm = 0 для SLM печати

            Console.WriteLine($"\nИтоговая точка UDM:");
            Console.WriteLine($"  X = {x:F3} мм");
            Console.WriteLine($"  Y = {y:F3} мм");
            Console.WriteLine($"  Z = {zCoord:F6} мм");
            Console.WriteLine();

            // 7. Добавляем точку
            // ПОПЫТКА 1: Используем UDM_AddPolyline3D вместо UDM_AddPoint2D
            // Причина: UDM_AddPolyline3D правильно обрабатывает Z координату
            structUdmPos point = new structUdmPos
            {
                x = x,
                y = y,
                z = UDM_GetZvalue(0,0, (float)(CorrectDiameterPrecise(beamDiameterMicron))),  // БЕЗ умножения на 4!
                a = 0
            };
            ZFinal = UDM_GetZvalue(0, 0, (float)(CorrectDiameterPrecise(beamDiameterMicron)));

            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 50; i < 250; i++)
            {
                float value = UDM_GetZvalue(0, 0, (float)(CorrectDiameterPrecise(i)));
                stringBuilder.AppendLine(
                    $"Diameter: {i} = {value}, {value/4}, Обратная: {GetRealDiameterFromSDK(CorrectDiameterPrecise(i))}");
            }


            if (!Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ztest")))
            {
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ztest"));
            }
            File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ztest", Guid.NewGuid().ToString() + ".txt"), stringBuilder.ToString());



            UDM_AddPoint2D(point, dwellTimeMs, 0);


            // 8. Завершаем построение (как в Java UdmProducer.setOpsAfter() line 298-300)
            UDM_Jump(0, 0, 0);  // Возврат в home position
            UDM_EndMain();

            // 9. Сохраняем файл
            int saveResult = UDM_SaveToFile(udmFilePath);
            if (saveResult != 0)
                throw new InvalidOperationException($"Не удалось сохранить UDM файл. Код ошибки: {saveResult}");

            Console.WriteLine($"✓ UDM файл успешно создан:");
            Console.WriteLine($"  {udmFilePath}");
            Console.WriteLine();

            // Диагностика: проверяем что заданный диаметр соответствует полученному Z
            VerifyDiameterMatch(beamDiameterMicron, zCoord);

            return udmFilePath;
        }

        /// <summary>
        /// Перегрузка для ручного задания Z координаты (для тестов)
        /// </summary>
        public string BuildSinglePoint(
            float x,
            float y,
            float z,
            double beamDiameterMicron,
            float powerWatts,
            int dwellTimeMs = 500)
        {
            Console.WriteLine("=== Тест UDM Builder (ручная Z координата) ===");
            Console.WriteLine($"Входные параметры:");
            Console.WriteLine($"  Позиция: X={x:F3} мм, Y={y:F3} мм, Z={z:F6} мм (ручная)");
            Console.WriteLine($"  Диаметр пучка: {beamDiameterMicron:F1} мкм");
            Console.WriteLine($"  Мощность лазера: {powerWatts:F1} Вт");
            Console.WriteLine($"  Время экспозиции: {dwellTimeMs} мс");
            Console.WriteLine();

            string udmFilePath = Path.Combine(_outputDirectory, $"SinglePoint_ManualZ_{Guid.NewGuid()}.bin");

            if (UDM_NewFile() != 0)
                throw new InvalidOperationException("Не удалось создать UDM файл");

            UDM_Main();
            UDM_SkyWriting(1);
            UDM_SetProtocol(1, 1);
            ApplyScannerTransforms();

            var markParams = CalculateMarkParameters(powerWatts, dwellTimeMs);
            MarkParameter[] parameters = new MarkParameter[] { markParams };
            UDM_SetLayersPara(parameters, 1);

            // ТЕСТ: Используем UDM_AddPolyline3D вместо UDM_AddPoint2D
            structUdmPos point = new structUdmPos
            {
                x = x,
                y = y,
                z = z,  // БЕЗ умножения!
                a = 0
            };

            Console.WriteLine($"⚠️ ТЕСТ: Используем UDM_AddPolyline3D");
            Console.WriteLine($"   Z координата: {z:F6} мм (без K_FACTOR)");
            Console.WriteLine();

            // Используем UDM_AddPolyline3D
            structUdmPos[] polyline = new structUdmPos[] { point };
            UDM_AddPolyline3D(polyline, 1, 0);

            // Для эмуляции времени экспозиции
            if (dwellTimeMs > 0)
            {
                structUdmPos dwellPoint = new structUdmPos { x = x, y = y, z = z, a = 0 };
                UDM_AddPoint2D(dwellPoint, dwellTimeMs, 0);
            }

            UDM_Jump(0, 0, 0);
            UDM_EndMain();

            int saveResult = UDM_SaveToFile(udmFilePath);
            if (saveResult != 0)
                throw new InvalidOperationException($"Не удалось сохранить UDM файл. Код ошибки: {saveResult}");

            Console.WriteLine($"✓ UDM файл успешно создан: {udmFilePath}");
            Console.WriteLine();

            return udmFilePath;
        }

        /// <summary>
        /// Обновляет текущее состояние (эквивалент DIAMETER и POWER операций в Java)
        /// Java: UdmProducer.java lines 242-246 (DIAMETER) и 231-240 (POWER)
        /// </summary>
        private void UpdateCurrentState(double beamDiameterMicron, float powerWatts)
        {
            Console.WriteLine("--- Обновление текущего состояния ---");

            // Java line 243-246: обновляем curBeamDiameterMicron
            if (beamDiameterMicron < _config.BeamConfig.MinBeamDiameterMicron)
            {
                Console.WriteLine($"⚠️ Внимание: Диаметр {beamDiameterMicron:F1} мкм < минимум {_config.BeamConfig.MinBeamDiameterMicron:F1} мкм");
                Console.WriteLine($"   Используем минимальный диаметр (фокус)");
                _currentBeamDiameterMicron = _config.BeamConfig.MinBeamDiameterMicron;
            }
            else
            {
                _currentBeamDiameterMicron = beamDiameterMicron;
            }

            // Java line 237: обновляем curPower
            _currentPowerWatts = powerWatts;

            Console.WriteLine($"Состояние: Диаметр={_currentBeamDiameterMicron:F1} мкм, Мощность={_currentPowerWatts:F1} Вт");
            Console.WriteLine();
        }

        /// <summary>
        /// ТОЧНЫЙ ПОРТ Java BeamConfig.getCorrectZValue()
        /// Источник: hans-dev/Hans4Java/src/org/iiv/hans4java/controlCardProfiles/BeamConfig.java lines 103-117
        /// </summary>
        /// <param name="coordXMm">Координата X в мм</param>
        /// <param name="coordYMm">Координата Y в мм</param>
        /// <param name="coordZMm">Координата Z в мм (для SLM всегда 0)</param>
        /// <returns>Скорректированная Z координата в мм</returns>
        private float GetCorrectZValue(float coordXMm, float coordYMm, float coordZMm)
        {
            Console.WriteLine("--- Расчет Z координаты (Java BeamConfig.getCorrectZValue) ---");

            // Шаг 1: Вычисляем текущее фокусное расстояние
            // Java line 104: double focalLengthMicron = getCurrentFocalLengthMm(coordXMm, coordYMm, coordZMm) * 1e3;
            double focalLengthMm = GetCurrentFocalLengthMm(coordXMm, coordYMm, coordZMm);
            double focalLengthMicron = focalLengthMm * 1e3;  // мм -> мкм

            FocalLengthMm = focalLengthMm;
            FocalLengthMicron = focalLengthMicron;

            Console.WriteLine($"1. getCurrentFocalLengthMm (Java line 119-121):");
            Console.WriteLine($"   focalLengthMm = sqrt({coordXMm}² + {coordYMm}² + ({_config.BeamConfig.FocalLengthMm} + {coordZMm})²)");
            Console.WriteLine($"   focalLengthMm = {focalLengthMm:F4} мм = {focalLengthMicron:F1} мкм");
            Console.WriteLine();

            // Шаг 2: Добавляем смещение от диаметра пучка (если включено)
            // Java line 105-107: if (enableDiameterChange) { focalLengthMicron += getLensTravelMicron(...); }
            // if (_config.FunctionSwitcherConfig.EnableDiameterChange)
            // {
            //     double lensTravelMicron = GetLensTravelMicron(_currentBeamDiameterMicron, _config.BeamConfig.MinBeamDiameterMicron);
            //     focalLengthMicron += lensTravelMicron;

            //     LensTravelMicron = lensTravelMicron;
            //     FocalLengthMicron = focalLengthMicron;

            //     Console.WriteLine($"2. getLensTravelMicron (Java line 199-202):");
            //     Console.WriteLine($"   Текущий диаметр: {_currentBeamDiameterMicron:F1} мкм");
            //     Console.WriteLine($"   Минимальный диаметр: {_config.BeamConfig.MinBeamDiameterMicron:F1} мкм");
            //     Console.WriteLine($"   Формула: zR * sqrt( (d/d₀)² - 1 )");
            //     Console.WriteLine($"   zR (длина Рэлея): {_config.BeamConfig.RayleighLengthMicron:F1} мкм");
            //     Console.WriteLine($"   Смещение линзы: {lensTravelMicron:F3} мкм = {lensTravelMicron / 1000.0:F6} мм");
            //     Console.WriteLine($"   Новое focalLength: {focalLengthMicron:F1} мкм");

            //     // Предупреждение о расфокусировке
            //     if (_currentBeamDiameterMicron > _config.BeamConfig.MinBeamDiameterMicron * 1.5)
            //     {
            //         double ratio = _currentBeamDiameterMicron / _config.BeamConfig.MinBeamDiameterMicron;
            //         double intensityLoss = ratio * ratio;  // I₀/I = (d/d₀)²
            //         Console.WriteLine($"   ⚠️ РАСФОКУСИРОВКА:");
            //         Console.WriteLine($"      Коэффициент: {ratio:F2}x");
            //         Console.WriteLine($"      Потеря интенсивности: {intensityLoss:F2}x ({(1 - 1/intensityLoss) * 100:F1}%)");
            //     }
            //     Console.WriteLine();
            // }
            // else
            // {
            //     LensTravelMicron = 0;
            //     Console.WriteLine($"2. Смещение от диаметра пучка (ВЫКЛЮЧЕНО)");
            //     Console.WriteLine();
            // }

            // Шаг 3: Вычитаем power offset (если включен)
            // Java line 108-110: if (enablePowerOffset) { focalLengthMicron -= getPowerOffset(); }
            // if (_config.FunctionSwitcherConfig.EnablePowerOffset)
            // {
            //     // Java line 195-196: проверка минимальной мощности
            //     if (_currentPowerWatts <= _config.LaserPowerConfig.MaxPower * 0.15)
            //     {
            //         PowerOffsetMicrons = 0;
            //         Console.WriteLine($"3. Power offset (пропущен: мощность < 15% от максимума)");
            //         Console.WriteLine();
            //     }
            //     else
            //     {
            //         double powerOffsetMicrons = _config.BeamConfig.GetPowerOffset((float)_currentPowerWatts, _config.LaserPowerConfig.MaxPower);
            //         focalLengthMicron -= powerOffsetMicrons;

            //         PowerOffsetMicrons = powerOffsetMicrons;
            //         FocalLengthMicron = focalLengthMicron;

            //         Console.WriteLine($"3. getPowerOffset (Java line 188-197):");
            //         Console.WriteLine($"   Мощность: {_currentPowerWatts:F1} Вт");
            //         Console.WriteLine($"   Максимум: {_config.LaserPowerConfig.MaxPower:F1} Вт");
            //         Console.WriteLine($"   Power offset: {powerOffsetMicrons:F3} мкм");
            //         Console.WriteLine($"   Новое focalLength: {focalLengthMicron:F1} мкм");
            //         Console.WriteLine();
            //     }
            // }
            // else
            // {
            //     PowerOffsetMicrons = 0;
            //     Console.WriteLine($"3. Power offset (ВЫКЛЮЧЕНО)");
            //     Console.WriteLine();
            // }

            // Шаг 4: Преобразуем focalLength в Z координату через полином (если включено)
            // Java line 111-116: if (enableZCorrection) { return getCalculationZValue(focalLengthMicron * 1e-3); }
            float zFinal;
            if (_config.FunctionSwitcherConfig.EnableZCorrection)
            {
                double f = focalLengthMicron * 1e-3;  // мкм -> мм
                zFinal = GetCalculationZValue(f);

                ZFinal = zFinal;

                Console.WriteLine($"4. getCalculationZValue (Java line 205-209):");
                Console.WriteLine($"   f = {f:F4} мм");
                Console.WriteLine($"   Полином: Z = a*f² + b*f + c");
                Console.WriteLine($"   a = {_config.ThirdAxisConfig.Afactor}");
                Console.WriteLine($"   b = {_config.ThirdAxisConfig.Bfactor:F6}");
                Console.WriteLine($"   c = {_config.ThirdAxisConfig.Cfactor:F6}");
                Console.WriteLine($"   Z = {_config.ThirdAxisConfig.Afactor}*{f:F4}² + {_config.ThirdAxisConfig.Bfactor:F6}*{f:F4} + {_config.ThirdAxisConfig.Cfactor:F6}");
                Console.WriteLine($"   Z final: {zFinal:F6} мм");

                // Проверка диапазона
                if (zFinal < -0.5 || zFinal > 0.5)
                {
                    Console.WriteLine($"   ⚠️ ВНИМАНИЕ: Z = {zFinal:F6} мм вне рекомендуемого диапазона [-0.5, 0.5] мм!");
                }
                Console.WriteLine();
            }
            else
            {
                // Java line 114: return coordZMm (для SLM всегда 0)
                zFinal = coordZMm;
                ZFinal = zFinal;

                Console.WriteLine($"4. Z коррекция (ВЫКЛЮЧЕНО) -> Z = coordZMm = {coordZMm:F2} мм");
                Console.WriteLine();
            }

            return zFinal;
        }

        /// <summary>
        /// ТОЧНЫЙ ПОРТ Java BeamConfig.getCurrentFocalLengthMm()
        /// Источник: BeamConfig.java lines 119-121
        /// </summary>
        private double GetCurrentFocalLengthMm(float coordXMm, float coordYMm, float coordZMm)
        {
            // Java line 120: return Math.sqrt(coordXMm*coordXMm + coordYMm*coordYMm + Math.pow(focalLengthMm + coordZMm, 2));
            return Math.Sqrt(
                coordXMm * coordXMm +
                coordYMm * coordYMm +
                Math.Pow(_config.BeamConfig.FocalLengthMm + coordZMm, 2)
            );
        }

        /// <summary>
        /// ТОЧНЫЙ ПОРТ Java BeamConfig.getLensTravelMicron()
        /// Источник: BeamConfig.java lines 199-202
        /// </summary>
        private double GetLensTravelMicron(double beamDiam, double minDiameter)
        {
            // Java line 200: if (beamDiam < minDiameter) return 0;
            if (beamDiam < minDiameter) return 0;

            // Java line 201: return rayleighLengthMicron * Math.sqrt((((beamDiam / 2) * (beamDiam / 2)) / ((minDiameter / 2) * (minDiameter / 2))) - 1);
            return _config.BeamConfig.RayleighLengthMicron *
                   Math.Sqrt((((beamDiam / 2) * (beamDiam / 2)) / ((minDiameter / 2) * (minDiameter / 2))) - 1);
        }

        /// <summary>
        /// ТОЧНЫЙ ПОРТ Java BeamConfig.getCalculationZValue()
        /// Источник: BeamConfig.java lines 205-209
        /// </summary>
        private float GetCalculationZValue(double realFocalLengthMm)
        {
            // Java line 206-208: return aFactor * Math.pow(realFocalLengthMm, 2) + bFactor * realFocalLengthMm + cFactor;
            return (float)(
                _config.ThirdAxisConfig.Afactor * Math.Pow(realFocalLengthMm, 2) +
                _config.ThirdAxisConfig.Bfactor * realFocalLengthMm +
                _config.ThirdAxisConfig.Cfactor
            );
        }

        /// <summary>
        /// Вычисляет параметры маркировки
        /// </summary>
        private MarkParameter CalculateMarkParameters(float powerWatts, int dwellTimeMs)
        {
            Console.WriteLine("--- Расчет параметров маркировки ---");

            // 1. Применяем коррекцию мощности (если включена)
            float correctedPowerWatts = powerWatts;
            if (_config.FunctionSwitcherConfig.EnablePowerCorrection)
            {
                correctedPowerWatts = _config.LaserPowerConfig.GetCorrectPowerWatts(powerWatts);
                Console.WriteLine($"Коррекция мощности: {powerWatts:F1} Вт -> {correctedPowerWatts:F1} Вт");
            }

            // 2. Конвертируем в проценты
            float powerPercent = _config.LaserPowerConfig.ConvertPower(correctedPowerWatts);
            powerPercent = Math.Max(20, Math.Min(100f, powerPercent));

            Console.WriteLine($"Мощность: {correctedPowerWatts:F1} Вт -> {powerPercent:F1}% (от {_config.LaserPowerConfig.MaxPower:F1} Вт макс)");

            // 3. Берем process variables
            var processVars = _config.ProcessVariablesMap.NonDepends[0];

            var markParams = new MarkParameter
            {
                MarkSpeed = (uint)processVars.MarkSpeed,
                JumpSpeed = (uint)processVars.JumpSpeed,
                MarkDelay = (uint)processVars.MarkDelay,
                JumpDelay = (uint)processVars.JumpDelay,
                PolygonDelay = (uint)processVars.PolygonDelay,
                LaserPower = powerPercent,
                AnalogMode = 1,
                MarkCount = 1,
                LaserOnDelay = (float)processVars.LaserOnDelay,
                LaserOffDelay = (float)processVars.LaserOffDelay
            };

            Console.WriteLine($"Process variables:");
            Console.WriteLine($"  MarkSpeed: {markParams.MarkSpeed} мм/с");
            Console.WriteLine($"  JumpSpeed: {markParams.JumpSpeed} мм/с");
            Console.WriteLine($"  MarkDelay: {markParams.MarkDelay} мкс");
            Console.WriteLine($"  LaserOnDelay: {markParams.LaserOnDelay} мкс");
            Console.WriteLine($"  LaserOffDelay: {markParams.LaserOffDelay} мкс");
            Console.WriteLine();

            return markParams;
        }

        /// <summary>
        /// Применяет трансформации сканера (offset, rotate)
        /// </summary>
        private void ApplyScannerTransforms()
        {

            float scaledOffsetX = _config.ScannerConfig.OffsetX;
            float scaledOffsetY = _config.ScannerConfig.OffsetY;

            UDM_SetOffset(
                scaledOffsetX,
                scaledOffsetY,
                _config.ScannerConfig.OffsetZ
            );
            Console.WriteLine($"Offset: X={_config.ScannerConfig.OffsetX}, Y={_config.ScannerConfig.OffsetY}, Z={_config.ScannerConfig.OffsetZ}");
            

            if (_config.ScannerConfig.RotateAngle != 0)
            {
                UDM_SetRotate(_config.ScannerConfig.RotateAngle, 0, 0);
                Console.WriteLine($"Rotate: {_config.ScannerConfig.RotateAngle}°");
            }
        }

        /// <summary>
        /// Проверяет соответствие заданного диаметра и полученной Z координаты
        /// </summary>
        private void VerifyDiameterMatch(double targetDiameterMicron, float zCoord)
        {
            Console.WriteLine("--- ДИАГНОСТИКА: Проверка соответствия диаметра ---");
            Console.WriteLine($"Заданный диаметр: {targetDiameterMicron:F1} мкм");
            Console.WriteLine($"Полученная Z координата: {zCoord:F6} мм");

            // Вычисляем ожидаемый диаметр при данной Z координате
            // Используя обратную формулу гауссова пучка
            double expectedDiameterMicron = _config.BeamConfig.CalculateDiameter(zCoord);

            double error = Math.Abs(expectedDiameterMicron - targetDiameterMicron);

            Console.WriteLine($"Ожидаемый диаметр при Z={zCoord:F6} мм: {expectedDiameterMicron:F2} мкм");
            Console.WriteLine($"Ошибка: {error:F2} мкм ({error / targetDiameterMicron * 100:F1}%)");

            if (error < 1.0)
            {
                Console.WriteLine($"✓ Отличная точность (< 1 мкм)");
            }
            else if (error < 3.0)
            {
                Console.WriteLine($"✓ Хорошая точность (< 3 мкм)");
            }
            else if (error < 10.0)
            {
                Console.WriteLine($"⚠️ Приемлемая точность (< 10 мкм)");
            }
            else
            {
                Console.WriteLine($"⚠️ ВНИМАНИЕ: Большая ошибка! Проверьте реальный диаметр на измерениях!");
            }
            Console.WriteLine();
        }
    }
}
