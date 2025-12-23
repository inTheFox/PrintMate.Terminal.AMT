using System;
using System.Collections.Generic;

namespace Hans.NET.Models
{
    public partial class BeamConfig
    {
        public double MinBeamDiameterMicron { get; set; } = 63;
        public double WavelengthNano { get; set; } = 1070.0;
        public double RayleighLengthMicron { get; set; } = 1426.715;
        public double M2 { get; set; } = 1.127;
        public double FocalLengthMm { get; set; } = 538.46;

        /// <summary>
        /// Массив диаметров пятна (μm) при разных мощностях для power offset коррекции
        /// Используется для интерполяции Z-смещения в зависимости от мощности
        /// </summary>
        public List<float> ActualPowerOffsetValue { get; set; }

        /// <summary>
        /// Рассчитать Z-offset (mm) для заданного целевого диаметра (μm)
        /// Формула Гауссова луча: w(z)² = w₀² × [1 + (z/zR)²]
        /// Решаем для z: z = zR × sqrt[(w/w₀)² - 1]
        /// </summary>
        public float CalculateZOffset(double targetDiameterMicron)
        {
            if (targetDiameterMicron < MinBeamDiameterMicron)
            {
                // Невозможно получить диаметр меньше минимального
                Console.WriteLine($"WARNING: Target diameter {targetDiameterMicron} μm " +
                                $"is less than minimum {MinBeamDiameterMicron} μm");
                return 0.0f;
            }

            if (Math.Abs(targetDiameterMicron - MinBeamDiameterMicron) < 0.001)
            {
                return 0.0f;  // Точно в фокусе
            }

            // Формула дефокусировки Гауссова луча (работает с диаметрами)
            // w(z) = w₀ × sqrt[1 + (z/zR)²]
            // d(z) = d₀ × sqrt[1 + (z/zR)²]  (т.к. d = 2w)
            // Решаем для z:
            double ratio = targetDiameterMicron / MinBeamDiameterMicron;
            double z_micron = RayleighLengthMicron * Math.Sqrt(ratio * ratio - 1.0);

            // Преобразовать μm -> mm
            float z_mm = (float)(z_micron / 1000.0);

            return z_mm;
        }

        /// <summary>
        /// Обратная функция: рассчитать диаметр (μm) для заданного Z-offset (mm)
        /// </summary>
        public double CalculateDiameter(float zOffsetMm)
        {
            if (Math.Abs(zOffsetMm) < 0.0001)
            {
                return MinBeamDiameterMicron;  // В фокусе
            }

            double z_micron = Math.Abs(zOffsetMm) * 1000.0;
            double ratio_squared = 1.0 + Math.Pow(z_micron / RayleighLengthMicron, 2);
            double diameter = MinBeamDiameterMicron * Math.Sqrt(ratio_squared);

            return diameter;
        }

        /// <summary>
        /// Проверка: рассчитать теоретическую длину Рэлея из параметров
        /// </summary>
        public double CalculateTheoreticalRayleighLength()
        {
            // z_R = π × w₀² × M² / λ
            // где w₀ - радиус перетяжки (радиус = диаметр / 2)
            // w₀ в μm, λ в μm (нужно перевести из nm)
            double w0_micron = MinBeamDiameterMicron / 2.0;  // радиус
            double lambda_micron = WavelengthNano / 1000.0;
            double zR = Math.PI * Math.Pow(w0_micron, 2) * M2 / lambda_micron;
            return zR;
        }

        /// <summary>
        /// Вычисляет Z-смещение (μm) для компенсации изменения диаметра пятна при изменении мощности
        /// Реализует логику Java BeamConfig.getPowerOffset()
        /// </summary>
        /// <param name="currentPowerWatts">Текущая мощность в ваттах</param>
        /// <param name="maxPowerWatts">Максимальная мощность лазера в ваттах</param>
        /// <returns>Z-смещение в микрометрах (μm)</returns>
        public float GetPowerOffset(float currentPowerWatts, float maxPowerWatts)
        {
            // Если мощность меньше 15% от максимальной - не применяем коррекцию
            if (currentPowerWatts <= maxPowerWatts * 0.15f)
                return 0.0f;

            // Если нет данных для коррекции
            if (ActualPowerOffsetValue == null || ActualPowerOffsetValue.Count < 2)
                return 0.0f;

            int n = ActualPowerOffsetValue.Count;

            // Создаём массив мощностей (равномерное распределение от 0 до maxPower)
            // powers[i] = i * (maxPower / (n - 1))
            float[] powers = new float[n];
            for (int i = 0; i < n; i++)
            {
                powers[i] = i * (maxPowerWatts / (n - 1));
            }

            // Создаём массив Z-смещений (рассчитываем через getLensTravelMicron)
            // offsetZMatrix[i] = CalculateZOffset(actualPowerOffsetValue[i]) * 1000 (в микронах)
            float[] offsetZMatrix = new float[n];
            for (int i = 0; i < n; i++)
            {
                // CalculateZOffset возвращает mm, нам нужны μm
                float zOffsetMm = CalculateZOffset(ActualPowerOffsetValue[i]);
                offsetZMatrix[i] = zOffsetMm * 1000.0f; // mm -> μm
            }

            // Проверяем границы
            if (currentPowerWatts <= powers[0])
                return offsetZMatrix[0];
            if (currentPowerWatts >= powers[n - 1])
                return offsetZMatrix[n - 1];

            // Находим индексы для интерполяции
            int index1 = 0;
            for (int i = 0; i < n - 1; i++)
            {
                if (currentPowerWatts >= powers[i] && currentPowerWatts <= powers[i + 1])
                {
                    index1 = i;
                    break;
                }
            }
            int index2 = index1 + 1;

            float x1 = powers[index1];
            float x2 = powers[index2];
            float y1 = offsetZMatrix[index1];
            float y2 = offsetZMatrix[index2];

            // Линейная интерполяция
            float offsetMicrons = y1 + (y2 - y1) * (currentPowerWatts - x1) / (x2 - x1);

            return offsetMicrons;
        }
    }
}
