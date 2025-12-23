using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Hans.NET.ThreeD.Examples
{
    #region Структуры данных (из предыдущих примеров)

    [StructLayout(LayoutKind.Sequential)]
    public struct structUdmPos
    {
        public float x;
        public float y;
        public float z;
        public float a;

        public structUdmPos(float x, float y, float z = 0, float a = 0)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.a = a;
        }

        public override string ToString()
        {
            return $"({x:F3}, {y:F3}, {z:F3})";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MarkParameter
    {
        public UInt32 MarkSpeed;
        public UInt32 JumpSpeed;
        public UInt32 MarkDelay;
        public UInt32 JumpDelay;
        public UInt32 PolygonDelay;
        public UInt32 MarkCount;
        public float LaserOnDelay;
        public float LaserOffDelay;
        public float FPKDelay;
        public float FPKLength;
        public float QDelay;
        public float DutyCycle;
        public float Frequency;
        public float StandbyFrequency;
        public float StandbyDutyCycle;
        public float LaserPower;
        public UInt32 AnalogMode;
        public UInt32 Waveform;
        public UInt32 PulseWidthMode;
        public UInt32 PulseWidth;

        public static MarkParameter CreateDefault()
        {
            return new MarkParameter
            {
                MarkSpeed = 800,
                JumpSpeed = 25000,
                MarkDelay = 500,
                JumpDelay = 400,
                PolygonDelay = 200,
                MarkCount = 1,
                LaserOnDelay = 120.0f,
                LaserOffDelay = 120.0f,
                FPKDelay = 0.0f,
                FPKLength = 0.0f,
                QDelay = 0.0f,
                DutyCycle = 0.5f,
                Frequency = 20.0f,
                StandbyFrequency = 20.0f,
                StandbyDutyCycle = 0.1f,
                LaserPower = 50.0f,
                AnalogMode = 0,
                Waveform = 0,
                PulseWidthMode = 0,
                PulseWidth = 100
            };
        }
    }

    #endregion

    #region UDM DLL Import (только необходимые для 3D)

    public class HM_UDM_DLL
    {
        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_NewFile();

        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SaveToFile(string strFilePath);

        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_Main();

        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_EndMain();

        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetProtocol(int nProtocol, int nDimensional);

        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetLayersPara(MarkParameter[] layersParameter, int count);

        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_AddPolyline3D(structUdmPos[] nPos, int nCount, int layerIndex);

        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_Jump(float x, float y, float z);

        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_SetOffset(float offsetX, float offsetY, float offsetZ);

        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_Set3dCorrectionPara(float baseFocal, double[] paraK, int nCount);

        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_GetZvalue(float x, float y, float height);

        [DllImport("HM_HashuScan.dll", CallingConvention = CallingConvention.Cdecl)]
        public extern static int UDM_AddBreakAndCorPolyline3D(structUdmPos[] nPos, int nCount, float p2pGap, int layerIndex);
    }

    #endregion

    #region Примеры 3D маркировки

    /// <summary>
    /// Примеры 3D лазерной маркировки с динамическим изменением фокуса
    /// </summary>
    public class ThreeDMarkingExamples
    {
        /// <summary>
        /// Пример 1: Спираль с подъемом (базовая 3D траектория)
        /// </summary>
        public static void Example1_SimpleSpiral(string outputPath)
        {
            Console.WriteLine("=== Пример 1: 3D Спираль с подъемом ===\n");

            // Создать новый файл в режиме 3D
            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1); // SPI протокол, 3D режим!

            // Параметры для 3D маркировки
            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = MarkParameter.CreateDefault();
            layers[0].MarkSpeed = 600;      // Медленнее для 3D
            layers[0].LaserPower = 60.0f;
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            HM_UDM_DLL.UDM_Main();

            // Параметры спирали
            int turns = 8;              // Количество витков
            int pointsPerTurn = 24;     // Точек на виток
            float radius = 12.0f;       // Радиус спирали (мм)
            float heightPerTurn = 1.5f; // Подъем на виток (мм)

            int totalPoints = turns * pointsPerTurn + 1;
            structUdmPos[] spiral = new structUdmPos[totalPoints];

            Console.WriteLine($"Параметры спирали:");
            Console.WriteLine($"  Витков: {turns}");
            Console.WriteLine($"  Точек на виток: {pointsPerTurn}");
            Console.WriteLine($"  Радиус: {radius} мм");
            Console.WriteLine($"  Подъем: {heightPerTurn} мм/виток");
            Console.WriteLine($"  Общий подъем: {turns * heightPerTurn} мм");

            double angleStep = 2 * Math.PI / pointsPerTurn;

            for (int i = 0; i < totalPoints; i++)
            {
                double angle = i * angleStep;
                float x = radius * (float)Math.Cos(angle);
                float y = radius * (float)Math.Sin(angle);
                float z = (i / (float)pointsPerTurn) * heightPerTurn;

                spiral[i] = new structUdmPos(x, y, z);

                if (i < 3 || i > totalPoints - 3)
                {
                    Console.WriteLine($"  Точка {i}: {spiral[i]}");
                }
                else if (i == 3)
                {
                    Console.WriteLine($"  ... ({totalPoints - 6} точек) ...");
                }
            }

            HM_UDM_DLL.UDM_AddPolyline3D(spiral, spiral.Length, 0);
            Console.WriteLine($"\n✓ Спираль добавлена ({totalPoints} точек)");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"✓ Файл сохранен: {outputPath}");
        }

        /// <summary>
        /// Пример 2: Маркировка на цилиндре (раскатка цилиндра)
        /// </summary>
        public static void Example2_CylinderMarking(string outputPath)
        {
            Console.WriteLine("\n=== Пример 2: Маркировка на цилиндре ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1); // 3D режим

            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = MarkParameter.CreateDefault();
            layers[0].MarkSpeed = 800;
            layers[0].LaserPower = 55.0f;
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            HM_UDM_DLL.UDM_Main();

            // Параметры цилиндра
            float cylinderRadius = 20.0f;  // Радиус цилиндра (мм)
            float cylinderHeight = 40.0f;  // Высота цилиндра (мм)
            int circumferenceSegments = 36; // Сегментов по окружности
            int heightSegments = 20;        // Сегментов по высоте

            Console.WriteLine($"Параметры цилиндра:");
            Console.WriteLine($"  Радиус: {cylinderRadius} мм");
            Console.WriteLine($"  Высота: {cylinderHeight} мм");
            Console.WriteLine($"  Окружность: {2 * Math.PI * cylinderRadius:F2} мм");

            // Создать горизонтальные линии вокруг цилиндра
            Console.WriteLine("\nСоздание горизонтальных линий:");
            for (int h = 0; h <= heightSegments; h++)
            {
                float currentHeight = (h / (float)heightSegments) * cylinderHeight;

                structUdmPos[] circle = new structUdmPos[circumferenceSegments + 1];

                for (int i = 0; i <= circumferenceSegments; i++)
                {
                    double angle = (i / (float)circumferenceSegments) * 2 * Math.PI;

                    // XY координаты на плоскости (после раскатки)
                    float x = (float)(angle * cylinderRadius); // Длина дуги
                    float y = currentHeight;

                    // Z корректируется для фокусировки на поверхности цилиндра
                    // Z = R * (1 - cos(θ)), где θ - угол от центра
                    float z = cylinderRadius * (1.0f - (float)Math.Cos(angle));

                    circle[i] = new structUdmPos(x, y, z);
                }

                HM_UDM_DLL.UDM_AddPolyline3D(circle, circle.Length, 0);

                if (h % 5 == 0)
                {
                    Console.WriteLine($"  Линия на высоте {currentHeight:F2} мм");
                }
            }

            Console.WriteLine($"\n✓ Создано {heightSegments + 1} горизонтальных линий");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"✓ Файл сохранен: {outputPath}");
        }

        /// <summary>
        /// Пример 3: Маркировка на сфере
        /// </summary>
        public static void Example3_SphereMarking(string outputPath)
        {
            Console.WriteLine("\n=== Пример 3: Маркировка на сфере ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = MarkParameter.CreateDefault();
            layers[0].MarkSpeed = 700;
            layers[0].LaserPower = 65.0f;
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            HM_UDM_DLL.UDM_Main();

            // Параметры сферы
            float sphereRadius = 15.0f;  // Радиус сферы (мм)
            int latitudeLines = 12;      // Линий широты
            int longitudeLines = 16;     // Линий долготы
            int pointsPerLine = 24;      // Точек на линию

            Console.WriteLine($"Параметры сферы:");
            Console.WriteLine($"  Радиус: {sphereRadius} мм");
            Console.WriteLine($"  Линий широты: {latitudeLines}");
            Console.WriteLine($"  Линий долготы: {longitudeLines}");

            // Создать линии широты (параллели)
            Console.WriteLine("\nСоздание линий широты:");
            for (int lat = 1; lat < latitudeLines; lat++) // Пропускаем полюса
            {
                // Угол от экватора (-π/2 до +π/2)
                double theta = Math.PI * (lat / (double)latitudeLines - 0.5);
                float circleRadius = sphereRadius * (float)Math.Cos(theta);
                float circleHeight = sphereRadius * (float)Math.Sin(theta);

                structUdmPos[] latitudeLine = new structUdmPos[pointsPerLine + 1];

                for (int i = 0; i <= pointsPerLine; i++)
                {
                    double phi = 2 * Math.PI * i / pointsPerLine;

                    float x = circleRadius * (float)Math.Cos(phi);
                    float y = circleRadius * (float)Math.Sin(phi);

                    // Z корректируется для фокуса на поверхность сферы
                    // Z = R - sqrt(R² - x² - y²) + базовое смещение
                    float z = sphereRadius - (float)Math.Sqrt(sphereRadius * sphereRadius - x * x - y * y);
                    z += circleHeight; // Добавить смещение по высоте

                    latitudeLine[i] = new structUdmPos(x, y, z);
                }

                HM_UDM_DLL.UDM_AddPolyline3D(latitudeLine, latitudeLine.Length, 0);

                if (lat % 3 == 0)
                {
                    Console.WriteLine($"  Широта {lat}: радиус окружности = {circleRadius:F2} мм, высота = {circleHeight:F2} мм");
                }
            }

            Console.WriteLine($"\n✓ Создано {latitudeLines - 1} линий широты");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"✓ Файл сохранен: {outputPath}");
        }

        /// <summary>
        /// Пример 4: Маркировка на конусе
        /// </summary>
        public static void Example4_ConeMarking(string outputPath)
        {
            Console.WriteLine("\n=== Пример 4: Маркировка на конусе ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = MarkParameter.CreateDefault();
            layers[0].MarkSpeed = 750;
            layers[0].LaserPower = 58.0f;
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            HM_UDM_DLL.UDM_Main();

            // Параметры конуса
            float coneBaseRadius = 25.0f;   // Радиус основания (мм)
            float coneTopRadius = 5.0f;     // Радиус верха (мм)
            float coneHeight = 35.0f;       // Высота конуса (мм)
            int heightSteps = 20;           // Слоев по высоте
            int segmentsPerCircle = 32;     // Точек на окружность

            Console.WriteLine($"Параметры конуса:");
            Console.WriteLine($"  Радиус основания: {coneBaseRadius} мм");
            Console.WriteLine($"  Радиус верха: {coneTopRadius} мм");
            Console.WriteLine($"  Высота: {coneHeight} мм");
            Console.WriteLine($"  Угол наклона: {Math.Atan2(coneBaseRadius - coneTopRadius, coneHeight) * 180 / Math.PI:F1}°");

            // Создать спиральную линию вдоль конуса
            Console.WriteLine("\nСоздание спиральной линии вдоль конуса:");

            int totalPoints = heightSteps * segmentsPerCircle;
            structUdmPos[] spiralCone = new structUdmPos[totalPoints + 1];

            for (int i = 0; i <= totalPoints; i++)
            {
                // Прогресс по высоте (0.0 = основание, 1.0 = верх)
                float heightRatio = i / (float)totalPoints;

                // Текущий радиус (линейная интерполяция)
                float currentRadius = coneBaseRadius + (coneTopRadius - coneBaseRadius) * heightRatio;

                // Текущая высота
                float currentHeight = heightRatio * coneHeight;

                // Угол по окружности
                double angle = 2 * Math.PI * i / segmentsPerCircle;

                // Координаты
                float x = currentRadius * (float)Math.Cos(angle);
                float y = currentRadius * (float)Math.Sin(angle);

                // Z корректируется для фокуса на наклонную поверхность
                // Расстояние от центральной оси до поверхности меняется
                float surfaceAngle = (float)Math.Atan2(coneBaseRadius - coneTopRadius, coneHeight);
                float z = currentHeight + currentRadius * (float)Math.Sin(surfaceAngle);

                spiralCone[i] = new structUdmPos(x, y, z);

                if (i % (totalPoints / 10) == 0)
                {
                    Console.WriteLine($"  {heightRatio * 100:F0}%: радиус = {currentRadius:F2} мм, высота = {currentHeight:F2} мм");
                }
            }

            HM_UDM_DLL.UDM_AddPolyline3D(spiralCone, spiralCone.Length, 0);
            Console.WriteLine($"\n✓ Спираль вдоль конуса добавлена ({spiralCone.Length} точек)");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"✓ Файл сохранен: {outputPath}");
        }

        /// <summary>
        /// Пример 5: Многослойная 3D печать (послойное наращивание)
        /// </summary>
        public static void Example5_LayeredPrinting(string outputPath)
        {
            Console.WriteLine("\n=== Пример 5: Многослойная 3D печать ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Разные параметры для разных слоев
            MarkParameter[] layers = new MarkParameter[3];

            // Слой 0: Подложка (высокая мощность)
            layers[0] = MarkParameter.CreateDefault();
            layers[0].MarkSpeed = 600;
            layers[0].LaserPower = 80.0f;
            layers[0].MarkCount = 2;

            // Слой 1: Средние слои (средняя мощность)
            layers[1] = MarkParameter.CreateDefault();
            layers[1].MarkSpeed = 800;
            layers[1].LaserPower = 60.0f;

            // Слой 2: Верхние слои (малая мощность для точности)
            layers[2] = MarkParameter.CreateDefault();
            layers[2].MarkSpeed = 1000;
            layers[2].LaserPower = 45.0f;

            HM_UDM_DLL.UDM_SetLayersPara(layers, 3);

            Console.WriteLine("Параметры слоев:");
            Console.WriteLine($"  Слой 0 (подложка):  {layers[0].MarkSpeed} мм/с, {layers[0].LaserPower}%, {layers[0].MarkCount} прохода");
            Console.WriteLine($"  Слой 1 (средние):   {layers[1].MarkSpeed} мм/с, {layers[1].LaserPower}%");
            Console.WriteLine($"  Слой 2 (верхние):   {layers[2].MarkSpeed} мм/с, {layers[2].LaserPower}%");

            HM_UDM_DLL.UDM_Main();

            // Параметры печати
            int totalLayers = 15;           // Слоев по высоте
            float layerThickness = 0.05f;   // Толщина слоя (мм)
            float patternSize = 20.0f;      // Размер штриховки (мм)
            float hatchSpacing = 0.5f;      // Расстояние между линиями штриховки (мм)

            Console.WriteLine($"\nПараметры печати:");
            Console.WriteLine($"  Слоев: {totalLayers}");
            Console.WriteLine($"  Толщина слоя: {layerThickness} мм");
            Console.WriteLine($"  Общая высота: {totalLayers * layerThickness} мм");
            Console.WriteLine($"  Шаг штриховки: {hatchSpacing} мм");

            Console.WriteLine("\nГенерация слоев:");

            for (int layer = 0; layer < totalLayers; layer++)
            {
                float currentZ = layer * layerThickness;

                // Выбор параметров в зависимости от высоты
                int layerIndex = 0;
                if (layer == 0)
                    layerIndex = 0; // Подложка
                else if (layer < totalLayers - 3)
                    layerIndex = 1; // Средние слои
                else
                    layerIndex = 2; // Верхние слои

                // Направление штриховки чередуется на каждом слое
                bool horizontal = (layer % 2 == 0);

                int linesCount = (int)(patternSize / hatchSpacing);

                for (int line = 0; line < linesCount; line++)
                {
                    float offset = -patternSize / 2 + line * hatchSpacing;

                    structUdmPos[] hatchLine;

                    if (horizontal)
                    {
                        // Горизонтальные линии
                        hatchLine = new structUdmPos[]
                        {
                            new structUdmPos(-patternSize / 2, offset, currentZ),
                            new structUdmPos( patternSize / 2, offset, currentZ)
                        };
                    }
                    else
                    {
                        // Вертикальные линии
                        hatchLine = new structUdmPos[]
                        {
                            new structUdmPos(offset, -patternSize / 2, currentZ),
                            new structUdmPos(offset,  patternSize / 2, currentZ)
                        };
                    }

                    HM_UDM_DLL.UDM_AddPolyline3D(hatchLine, hatchLine.Length, layerIndex);
                }

                if (layer % 5 == 0 || layer == totalLayers - 1)
                {
                    Console.WriteLine($"  Слой {layer}: Z = {currentZ:F3} мм, направление = {(horizontal ? "горизонталь" : "вертикаль")}, параметры = слой {layerIndex}");
                }
            }

            Console.WriteLine($"\n✓ Создано {totalLayers} слоев для печати");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"✓ Файл сохранен: {outputPath}");
        }

        /// <summary>
        /// Пример 6: Синусоидальная волна в 3D
        /// </summary>
        public static void Example6_SineWave3D(string outputPath)
        {
            Console.WriteLine("\n=== Пример 6: Синусоидальная волна в 3D ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = MarkParameter.CreateDefault();
            layers[0].MarkSpeed = 900;
            layers[0].LaserPower = 52.0f;
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            HM_UDM_DLL.UDM_Main();

            // Параметры волны
            float waveLength = 40.0f;       // Длина волны по X (мм)
            float waveAmplitudeY = 8.0f;    // Амплитуда по Y (мм)
            float waveAmplitudeZ = 3.0f;    // Амплитуда по Z (мм)
            int periods = 3;                // Количество периодов
            int pointsPerPeriod = 30;       // Точек на период

            Console.WriteLine($"Параметры волны:");
            Console.WriteLine($"  Длина волны: {waveLength} мм");
            Console.WriteLine($"  Амплитуда по Y: {waveAmplitudeY} мм");
            Console.WriteLine($"  Амплитуда по Z: {waveAmplitudeZ} мм");
            Console.WriteLine($"  Периодов: {periods}");

            int totalPoints = periods * pointsPerPeriod + 1;
            structUdmPos[] wave = new structUdmPos[totalPoints];

            Console.WriteLine("\nГенерация точек волны:");

            for (int i = 0; i < totalPoints; i++)
            {
                float t = i / (float)pointsPerPeriod; // Параметр от 0 до periods

                float x = t * waveLength;
                float y = waveAmplitudeY * (float)Math.Sin(2 * Math.PI * t);
                float z = waveAmplitudeZ * (float)Math.Sin(2 * Math.PI * t + Math.PI / 2); // Сдвиг фазы на 90°

                wave[i] = new structUdmPos(x, y, z);

                if (i % pointsPerPeriod == 0)
                {
                    Console.WriteLine($"  Период {i / pointsPerPeriod}: ({x:F2}, {y:F2}, {z:F2})");
                }
            }

            HM_UDM_DLL.UDM_AddPolyline3D(wave, wave.Length, 0);
            Console.WriteLine($"\n✓ Волна добавлена ({totalPoints} точек)");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"✓ Файл сохранен: {outputPath}");
        }

        /// <summary>
        /// Пример 7: Параметрическая поверхность (тор)
        /// </summary>
        public static void Example7_TorusMarking(string outputPath)
        {
            Console.WriteLine("\n=== Пример 7: Маркировка на торе ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = MarkParameter.CreateDefault();
            layers[0].MarkSpeed = 650;
            layers[0].LaserPower = 62.0f;
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            HM_UDM_DLL.UDM_Main();

            // Параметры тора
            float majorRadius = 18.0f;      // Большой радиус (от центра до центра трубы)
            float minorRadius = 6.0f;       // Малый радиус (радиус трубы)
            int majorSteps = 24;            // Шагов по большому кругу
            int minorSteps = 16;            // Шагов по малому кругу

            Console.WriteLine($"Параметры тора:");
            Console.WriteLine($"  Большой радиус: {majorRadius} мм");
            Console.WriteLine($"  Малый радиус: {minorRadius} мм");
            Console.WriteLine($"  Внешний диаметр: {2 * (majorRadius + minorRadius)} мм");
            Console.WriteLine($"  Внутренний диаметр: {2 * (majorRadius - minorRadius)} мм");

            Console.WriteLine("\nСоздание кругов вдоль тора:");

            // Создать малые окружности вдоль большого круга
            for (int major = 0; major < majorSteps; major++)
            {
                double theta = 2 * Math.PI * major / majorSteps; // Угол по большому кругу

                // Центр малой окружности
                float centerX = majorRadius * (float)Math.Cos(theta);
                float centerY = majorRadius * (float)Math.Sin(theta);

                structUdmPos[] minorCircle = new structUdmPos[minorSteps + 1];

                for (int minor = 0; minor <= minorSteps; minor++)
                {
                    double phi = 2 * Math.PI * minor / minorSteps; // Угол по малому кругу

                    // Параметрические уравнения тора:
                    // x = (R + r*cos(φ)) * cos(θ)
                    // y = (R + r*cos(φ)) * sin(θ)
                    // z = r * sin(φ)

                    float x = (majorRadius + minorRadius * (float)Math.Cos(phi)) * (float)Math.Cos(theta);
                    float y = (majorRadius + minorRadius * (float)Math.Cos(phi)) * (float)Math.Sin(theta);
                    float z = minorRadius * (float)Math.Sin(phi);

                    minorCircle[minor] = new structUdmPos(x, y, z);
                }

                HM_UDM_DLL.UDM_AddPolyline3D(minorCircle, minorCircle.Length, 0);

                if (major % 6 == 0)
                {
                    Console.WriteLine($"  Круг {major}: угол = {theta * 180 / Math.PI:F1}°, центр = ({centerX:F2}, {centerY:F2})");
                }
            }

            Console.WriteLine($"\n✓ Создано {majorSteps} окружностей на поверхности тора");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"✓ Файл сохранен: {outputPath}");
        }

        /// <summary>
        /// Пример 8: 3D текст с переменной высотой
        /// </summary>
        public static void Example8_3DText(string outputPath)
        {
            Console.WriteLine("\n=== Пример 8: 3D текст с переменной высотой ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = MarkParameter.CreateDefault();
            layers[0].MarkSpeed = 550;
            layers[0].LaserPower = 70.0f;
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            HM_UDM_DLL.UDM_Main();

            // Создать букву "A" с объемным эффектом
            float baseHeight = 20.0f;   // Высота буквы
            float baseWidth = 15.0f;    // Ширина буквы
            float depth = 2.0f;         // Глубина (Z)

            Console.WriteLine("Создание объемной буквы 'A':");
            Console.WriteLine($"  Высота: {baseHeight} мм");
            Console.WriteLine($"  Ширина: {baseWidth} мм");
            Console.WriteLine($"  Глубина: {depth} мм");

            // Контур буквы A состоит из:
            // 1. Левая наклонная линия
            // 2. Правая наклонная линия
            // 3. Горизонтальная перемычка

            // Левая линия с градиентом глубины
            int pointsPerLine = 20;
            structUdmPos[] leftLine = new structUdmPos[pointsPerLine];
            for (int i = 0; i < pointsPerLine; i++)
            {
                float ratio = i / (float)(pointsPerLine - 1);
                leftLine[i] = new structUdmPos(
                    -baseWidth / 2 + ratio * baseWidth / 2,     // X: от левого низа к верху центру
                    ratio * baseHeight,                          // Y: снизу вверх
                    depth * (1.0f - ratio)                       // Z: от глубины к поверхности
                );
            }
            HM_UDM_DLL.UDM_AddPolyline3D(leftLine, leftLine.Length, 0);
            Console.WriteLine("  ✓ Левая линия добавлена");

            // Правая линия с градиентом глубины
            structUdmPos[] rightLine = new structUdmPos[pointsPerLine];
            for (int i = 0; i < pointsPerLine; i++)
            {
                float ratio = i / (float)(pointsPerLine - 1);
                rightLine[i] = new structUdmPos(
                    baseWidth / 2 - ratio * baseWidth / 2,      // X: от правого низа к верху центру
                    ratio * baseHeight,                          // Y: снизу вверх
                    depth * (1.0f - ratio)                       // Z: от глубины к поверхности
                );
            }
            HM_UDM_DLL.UDM_AddPolyline3D(rightLine, rightLine.Length, 0);
            Console.WriteLine("  ✓ Правая линия добавлена");

            // Горизонтальная перемычка на высоте 40%
            float crossbarHeight = baseHeight * 0.4f;
            float crossbarZ = depth * 0.6f; // Меньше глубины
            structUdmPos[] crossbar = new structUdmPos[pointsPerLine];
            for (int i = 0; i < pointsPerLine; i++)
            {
                float ratio = i / (float)(pointsPerLine - 1);
                crossbar[i] = new structUdmPos(
                    -baseWidth / 4 + ratio * baseWidth / 2,     // От левой стороны к правой
                    crossbarHeight,
                    crossbarZ
                );
            }
            HM_UDM_DLL.UDM_AddPolyline3D(crossbar, crossbar.Length, 0);
            Console.WriteLine("  ✓ Перемычка добавлена");

            Console.WriteLine("\n✓ Объемная буква 'A' создана");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"✓ Файл сохранен: {outputPath}");
        }

        /// <summary>
        /// Пример 9: Использование 3D коррекции (кривизна поля)
        /// </summary>
        public static void Example9_FieldCurvatureCorrection(string outputPath)
        {
            Console.WriteLine("\n=== Пример 9: 3D коррекция кривизны поля ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            // Установить параметры коррекции
            // Используются коэффициенты из конфигурации
            float baseFocal = 538.46f; // Базовое фокусное расстояние

            // Коэффициенты полинома: Z = A×r² + B×r + C
            double[] correctionParams = new double[3];
            correctionParams[0] = 0.0;          // A-фактор (квадратичный)
            correctionParams[1] = 0.013944261;  // B-фактор (линейный)
            correctionParams[2] = -7.5056114;   // C-фактор (постоянный)

            Console.WriteLine("Параметры коррекции:");
            Console.WriteLine($"  Базовое фокусное расстояние: {baseFocal} мм");
            Console.WriteLine($"  A-фактор: {correctionParams[0]}");
            Console.WriteLine($"  B-фактор: {correctionParams[1]}");
            Console.WriteLine($"  C-фактор: {correctionParams[2]}");

            HM_UDM_DLL.UDM_Set3dCorrectionPara(baseFocal, correctionParams, 3);
            Console.WriteLine("✓ Параметры коррекции установлены");

            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = MarkParameter.CreateDefault();
            layers[0].MarkSpeed = 1200;
            layers[0].LaserPower = 48.0f;
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            HM_UDM_DLL.UDM_Main();

            // Создать сетку для демонстрации коррекции
            float gridSize = 180.0f;    // Размер сетки
            int gridDivisions = 9;      // Делений
            float step = gridSize / gridDivisions;

            Console.WriteLine($"\nСоздание сетки {gridDivisions}x{gridDivisions}:");
            Console.WriteLine($"  Размер: {gridSize} x {gridSize} мм");

            // Расчет коррекции Z для разных точек
            Console.WriteLine("\nПримеры коррекции Z:");
            float[] testDistances = { 0, 50, 100, 150, 200 };
            foreach (float r in testDistances)
            {
                double zCorrection = correctionParams[0] * r * r +
                                   correctionParams[1] * r +
                                   correctionParams[2];
                Console.WriteLine($"  r = {r:F0} мм: Z_correction = {zCorrection:F3} мм");
            }

            // Горизонтальные линии
            for (int i = 0; i <= gridDivisions; i++)
            {
                float y = -gridSize / 2 + i * step;
                structUdmPos[] hLine = new structUdmPos[gridDivisions + 1];

                for (int j = 0; j <= gridDivisions; j++)
                {
                    float x = -gridSize / 2 + j * step;

                    // Расстояние от центра
                    float r = (float)Math.Sqrt(x * x + y * y);

                    // Коррекция Z по формуле
                    float zCorrection = (float)(correctionParams[0] * r * r +
                                                correctionParams[1] * r +
                                                correctionParams[2]);

                    hLine[j] = new structUdmPos(x, y, zCorrection);
                }

                HM_UDM_DLL.UDM_AddPolyline3D(hLine, hLine.Length, 0);
            }

            // Вертикальные линии
            for (int j = 0; j <= gridDivisions; j++)
            {
                float x = -gridSize / 2 + j * step;
                structUdmPos[] vLine = new structUdmPos[gridDivisions + 1];

                for (int i = 0; i <= gridDivisions; i++)
                {
                    float y = -gridSize / 2 + i * step;
                    float r = (float)Math.Sqrt(x * x + y * y);
                    float zCorrection = (float)(correctionParams[0] * r * r +
                                                correctionParams[1] * r +
                                                correctionParams[2]);

                    vLine[i] = new structUdmPos(x, y, zCorrection);
                }

                HM_UDM_DLL.UDM_AddPolyline3D(vLine, vLine.Length, 0);
            }

            Console.WriteLine($"\n✓ Сетка с коррекцией создана");
            Console.WriteLine("  Z автоматически корректируется для компенсации кривизны поля");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"✓ Файл сохранен: {outputPath}");
        }

        /// <summary>
        /// Пример 10: Сложная 3D траектория - узел Лиссажу
        /// </summary>
        public static void Example10_LissajousKnot(string outputPath)
        {
            Console.WriteLine("\n=== Пример 10: Узел Лиссажу в 3D ===\n");

            HM_UDM_DLL.UDM_NewFile();
            HM_UDM_DLL.UDM_SetProtocol(0, 1);

            MarkParameter[] layers = new MarkParameter[1];
            layers[0] = MarkParameter.CreateDefault();
            layers[0].MarkSpeed = 850;
            layers[0].LaserPower = 55.0f;
            HM_UDM_DLL.UDM_SetLayersPara(layers, 1);

            HM_UDM_DLL.UDM_Main();

            // Параметры узла Лиссажу
            int a = 3;          // Частота по X
            int b = 2;          // Частота по Y
            int c = 5;          // Частота по Z
            float scale = 15.0f; // Масштаб
            int points = 500;   // Количество точек

            Console.WriteLine($"Параметры узла Лиссажу:");
            Console.WriteLine($"  Частоты: a={a}, b={b}, c={c}");
            Console.WriteLine($"  Масштаб: {scale} мм");
            Console.WriteLine($"  Точек: {points}");

            // Параметрические уравнения узла Лиссажу:
            // x(t) = cos(a×t)
            // y(t) = cos(b×t)
            // z(t) = cos(c×t)

            structUdmPos[] knot = new structUdmPos[points + 1];

            Console.WriteLine("\nГенерация узла:");
            for (int i = 0; i <= points; i++)
            {
                double t = 2 * Math.PI * i / points;

                float x = scale * (float)Math.Cos(a * t);
                float y = scale * (float)Math.Cos(b * t);
                float z = scale * 0.5f * (float)Math.Cos(c * t); // Меньший масштаб по Z

                knot[i] = new structUdmPos(x, y, z);

                if (i % (points / 10) == 0)
                {
                    Console.WriteLine($"  {i * 100 / points}%: {knot[i]}");
                }
            }

            HM_UDM_DLL.UDM_AddPolyline3D(knot, knot.Length, 0);
            Console.WriteLine($"\n✓ Узел Лиссажу добавлен ({knot.Length} точек)");

            HM_UDM_DLL.UDM_EndMain();
            HM_UDM_DLL.UDM_SaveToFile(outputPath);
            Console.WriteLine($"✓ Файл сохранен: {outputPath}");
        }
    }

    #endregion

    #region Главная программа

    class Program3DExamples
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("╔══════════════════════════════════════════════════════════╗");
            Console.WriteLine("║          Примеры 3D лазерной маркировки                  ║");
            Console.WriteLine("║     (Hans HM_HashuScan - 3D режим с изменением фокуса)   ║");
            Console.WriteLine("╚══════════════════════════════════════════════════════════╝\n");

            try
            {
                string outputDir = @"C:\MarkFiles\3D_Examples";

                if (!System.IO.Directory.Exists(outputDir))
                {
                    System.IO.Directory.CreateDirectory(outputDir);
                    Console.WriteLine($"📁 Создана директория: {outputDir}\n");
                }

                // Выполнить все примеры 3D маркировки
                ThreeDMarkingExamples.Example1_SimpleSpiral($@"{outputDir}\01_3d_spiral.bin");
                ThreeDMarkingExamples.Example2_CylinderMarking($@"{outputDir}\02_cylinder.bin");
                ThreeDMarkingExamples.Example3_SphereMarking($@"{outputDir}\03_sphere.bin");
                ThreeDMarkingExamples.Example4_ConeMarking($@"{outputDir}\04_cone.bin");
                ThreeDMarkingExamples.Example5_LayeredPrinting($@"{outputDir}\05_layered_printing.bin");
                ThreeDMarkingExamples.Example6_SineWave3D($@"{outputDir}\06_sine_wave_3d.bin");
                ThreeDMarkingExamples.Example7_TorusMarking($@"{outputDir}\07_torus.bin");
                ThreeDMarkingExamples.Example8_3DText($@"{outputDir}\08_3d_text_A.bin");
                ThreeDMarkingExamples.Example9_FieldCurvatureCorrection($@"{outputDir}\09_field_correction.bin");
                ThreeDMarkingExamples.Example10_LissajousKnot($@"{outputDir}\10_lissajous_knot.bin");

                Console.WriteLine("\n\n╔══════════════════════════════════════════════════════════╗");
                Console.WriteLine("║         Все примеры 3D маркировки выполнены!             ║");
                Console.WriteLine($"║  Файлы: {outputDir.PadRight(48)}║");
                Console.WriteLine("║                                                          ║");
                Console.WriteLine("║  Ключевые особенности 3D маркировки:                     ║");
                Console.WriteLine("║  ✓ Динамическое изменение фокуса (ось Z)                ║");
                Console.WriteLine("║  ✓ Маркировка на криволинейных поверхностях             ║");
                Console.WriteLine("║  ✓ Послойное наращивание материала                      ║");
                Console.WriteLine("║  ✓ Коррекция кривизны оптического поля                  ║");
                Console.WriteLine("║  ✓ Сложные параметрические траектории                   ║");
                Console.WriteLine("╚══════════════════════════════════════════════════════════╝");

                Console.WriteLine("\n📖 Дополнительная информация:");
                Console.WriteLine("  • Все примеры используют UDM_AddPolyline3D");
                Console.WriteLine("  • Координата Z управляет положением фокуса");
                Console.WriteLine("  • Положительное Z = фокус ниже, отрицательное = выше");
                Console.WriteLine("  • Для точной работы требуется калибровка Z-оси");
                Console.WriteLine("  • Используйте коррекцию кривизны поля для больших областей");

            }
            catch (DllNotFoundException ex)
            {
                Console.WriteLine($"\n✗ ОШИБКА: Не найдена библиотека HM_HashuScan.dll");
                Console.WriteLine($"  Детали: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n✗ ОШИБКА: {ex.Message}");
                Console.WriteLine($"  Стек: {ex.StackTrace}");
            }

            Console.WriteLine("\n\nНажмите Enter для выхода...");
            Console.ReadLine();
        }
    }

    #endregion
}
