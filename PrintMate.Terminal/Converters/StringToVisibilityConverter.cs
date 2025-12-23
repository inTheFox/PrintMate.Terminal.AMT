using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrintMate.Terminal.Converters
{
    /// <summary>
    /// Конвертер для преобразования строки в Visibility
    /// Пустая строка или null = Collapsed, непустая строка = Visible
    /// Параметр "inverse" инвертирует логику
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isInverse = parameter?.ToString()?.Equals("inverse", StringComparison.OrdinalIgnoreCase) ?? false;
            bool hasValue = !string.IsNullOrWhiteSpace(value?.ToString());

            if (isInverse)
            {
                return hasValue ? Visibility.Collapsed : Visibility.Visible;
            }

            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
