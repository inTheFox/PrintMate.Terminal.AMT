using System;

namespace Hans.NET.Models
{
    public class ThirdAxisConfig
    {
        public double Bfactor { get; set; }
        public double Cfactor { get; set; }
        public double Afactor { get; set; }

        /// <summary>
        /// Базовая фокусная длина (для 3D коррекции)
        /// </summary>
        public float BaseFocal { get; set; } = 538.46f;

        /// <summary>
        /// Коэффициенты полинома 3D коррекции (для совместимости с Hans API)
        /// corPara3D[0] = Cfactor (константа)
        /// corPara3D[1] = Bfactor (линейный)
        /// corPara3D[2] = Afactor (квадратичный)
        /// </summary>
        public double[] CorrectionPolynomial
        {
            get => new[] { Cfactor, Bfactor, Afactor };
            set
            {
                if (value == null || value.Length < 3) return;
                Cfactor = value[0];
                Bfactor = value[1];
                Afactor = value[2];
            }
        }

        /// <summary>
        /// Вычисляет коррекцию кривизны поля для точки (x, y)
        /// Использует 2D радиальное расстояние (простая модель)
        /// </summary>
        public float CalculateFieldCorrection(float x, float y)
        {
            double r = Math.Sqrt(x * x + y * y);
            double z_corr = Afactor * r * r + Bfactor * r + Cfactor;
            return (float)z_corr;
        }

        /// <summary>
        /// Вычисляет Z-значение с 3D коррекцией (точная имплементация Hans UDM_GetZvalue)
        /// Использует 3D расстояние с учётом базовой фокусной длины (height = 0)
        /// </summary>
        /// <param name="x">Координата X в мм</param>
        /// <param name="y">Координата Y в мм</param>
        /// <returns>Скорректированное Z-значение в мм</returns>
        public float CalculateZValue3D(float x, float y)
        {
            // Точная реализация C-кода UDM_GetZvalue с height = 0:
            // double distance = sqrt(x*x + y*y + (baseFocal - 0)*(baseFocal - 0));
            // distance = sqrt(x*x + y*y + baseFocal*baseFocal);
            double distance = Math.Sqrt(x * x + y * y + BaseFocal * BaseFocal);

            // double zVal = 0;
            // for (int i = 0; i < corPara3DCount; i++) { zVal += (corPara3D[i] * pow(distance, i)); }
            double zVal = 0.0;
            double[] coeffs = CorrectionPolynomial;

            for (int i = 0; i < coeffs.Length; i++)
            {
                zVal += coeffs[i] * Math.Pow(distance, i);
            }

            return (float)zVal;
        }

        /// <summary>
        /// Сравнивает 2D и 3D методы коррекции
        /// </summary>
        public (float correction2D, float correction3D, float difference) Compare2DVs3D(float x, float y)
        {
            float corr2D = CalculateFieldCorrection(x, y);
            float corr3D = CalculateZValue3D(x, y);
            float diff = Math.Abs(corr3D - corr2D);

            return (corr2D, corr3D, diff);
        }
    }
}
