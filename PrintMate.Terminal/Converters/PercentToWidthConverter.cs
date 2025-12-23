using System;
using System.Globalization;
using System.Windows.Data;

namespace PrintMate.Terminal.Converters
{
    /// <summary>
    /// Конвертер для преобразования процента (0-100) в ширину прогресс-бара
    /// </summary>
    public class PercentToWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return 0.0;

            try
            {
                // value - процент (0-100)
                double percent = System.Convert.ToDouble(value);
                // parameter - максимальная ширина
                double maxWidth = System.Convert.ToDouble(parameter);

                // Ограничиваем процент в диапазоне 0-100
                percent = Math.Max(0, Math.Min(100, percent));

                // Вычисляем ширину
                double width = (percent / 100.0) * maxWidth;

                return width;
            }
            catch
            {
                return 0.0;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
