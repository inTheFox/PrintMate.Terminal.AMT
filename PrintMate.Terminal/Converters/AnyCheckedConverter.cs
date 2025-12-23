using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace PrintMate.Terminal.Converters
{
    /// <summary>
    /// Конвертер для проверки наличия хотя бы одного выбранного элемента с IsEnabled=true
    /// Используется для валидации списка прав доступа
    /// </summary>
    public class AnyCheckedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable collection)
            {
                // Проверяем, есть ли хотя бы один элемент с IsEnabled = true
                foreach (var item in collection)
                {
                    var isEnabledProperty = item.GetType().GetProperty("IsEnabled");
                    if (isEnabledProperty != null)
                    {
                        var isEnabled = isEnabledProperty.GetValue(item);
                        if (isEnabled is bool boolValue && boolValue)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
