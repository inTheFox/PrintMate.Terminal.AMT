using System.Collections.Generic;

namespace Hans.NET.Models
{
    public class LaserPowerConfig
    {
        public float MaxPower { get; set; }
        public List<float> ActualPowerCorrectionValue { get; set; }
        public float PowerOffsetKFactor { get; set; }
        public float PowerOffsetCFactor { get; set; }


        public float ConvertPower(float powerWatts)
        {
            // 1. ������� ����������� (������)
            return (powerWatts / MaxPower) * 100f;
        }

        public float ApplyOffsetCorrection(float percent)
        {
            float offset = PowerOffsetKFactor * percent + PowerOffsetCFactor;
            percent += offset;
            return percent;
        }

        public float ApplyTableCorrection(float targetPercent)
        {
            if (ActualPowerCorrectionValue == null ||
                ActualPowerCorrectionValue.Count == 0)
            {
                return targetPercent;
            }

            int n = ActualPowerCorrectionValue.Count;
            float step = 100.0f / (n - 1);

            int index1 = (int)(targetPercent / step);
            int index2 = index1 + 1;

            if (index1 < 0) return ActualPowerCorrectionValue[0];
            if (index2 >= n) return ActualPowerCorrectionValue[n - 1];

            float x1 = index1 * step;
            float x2 = index2 * step;
            float y1 = ActualPowerCorrectionValue[index1];
            float y2 = ActualPowerCorrectionValue[index2];

            return y1 + (y2 - y1) * (targetPercent - x1) / (x2 - x1);
        }

        /// <summary>
        /// Применяет коррекцию мощности к ВАТТАМ (как в Java BeamConfig.getCorrectPower())
        /// Вход: мощность в ваттах
        /// Выход: скорректированная мощность в ваттах
        /// </summary>
        public float GetCorrectPowerWatts(float powerWatts)
        {
            if (ActualPowerCorrectionValue == null || ActualPowerCorrectionValue.Count == 0)
            {
                return powerWatts;
            }

            int n = ActualPowerCorrectionValue.Count;

            // Создаем массив теоретических мощностей (равномерное распределение от 0 до MaxPower)
            // Это соответствует Java: theorPowers[i] = i * (maxPower / (theorPowers.length - 1))
            float[] theorPowers = new float[n];
            for (int i = 0; i < n; i++)
            {
                theorPowers[i] = i * (MaxPower / (n - 1));
            }

            // ActualPowerCorrectionValue - это реальные измеренные мощности (X ось)
            // theorPowers - это теоретические целевые мощности (Y ось)
            // Интерполяция: по измеренной мощности находим, какую теоретическую нужно установить

            // Проверяем границы
            if (powerWatts <= ActualPowerCorrectionValue[0])
                return theorPowers[0];
            if (powerWatts >= ActualPowerCorrectionValue[n - 1])
                return theorPowers[n - 1];

            // Находим индексы для интерполяции
            int index1 = 0;
            for (int i = 0; i < n - 1; i++)
            {
                if (powerWatts >= ActualPowerCorrectionValue[i] && powerWatts <= ActualPowerCorrectionValue[i + 1])
                {
                    index1 = i;
                    break;
                }
            }
            int index2 = index1 + 1;

            float x1 = ActualPowerCorrectionValue[index1];
            float x2 = ActualPowerCorrectionValue[index2];
            float y1 = theorPowers[index1];
            float y2 = theorPowers[index2];

            // Линейная интерполяция
            float correctedPowerWatts = y1 + (y2 - y1) * (powerWatts - x1) / (x2 - x1);

            return correctedPowerWatts;
        }
    }
}
