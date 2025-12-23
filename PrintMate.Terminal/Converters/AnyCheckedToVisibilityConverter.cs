using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PrintMate.Terminal.Converters
{
    /// <summary>
    /// Мультиконвертер для отображения сообщения о валидации, если ни один элемент не выбран
    /// Принимает коллекцию и результат AnyCheckedConverter
    /// </summary>
    public class AnyCheckedToVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] - коллекция AvailablePermissions
            // values[1] - результат AnyCheckedConverter (bool)

            if (values.Length >= 2 && values[1] is bool anyChecked)
            {
                // Если хотя бы один выбран - скрываем сообщение об ошибке
                return anyChecked ? Visibility.Collapsed : Visibility.Visible;
            }

            // По умолчанию показываем сообщение (ничего не выбрано)
            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
