using System;
using System.Globalization;
using System.Windows.Data;

namespace PrintMate.Terminal.Converters
{
    /// <summary>
    /// Конвертер для текста кнопки смены пароля
    /// true = "Скрыть форму смены пароля", false = "Сменить пароль"
    /// </summary>
    public class BoolToPasswordChangeTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "Скрыть форму смены пароля" : "Сменить пароль";
            }
            return "Сменить пароль";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
