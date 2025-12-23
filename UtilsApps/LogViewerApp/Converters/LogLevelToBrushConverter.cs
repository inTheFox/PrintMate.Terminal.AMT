using LoggingService.Shared.Models;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace LogViewerApp.Converters
{
    public class LogLevelToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LogLevel level)
            {
                return level switch
                {
                    LogLevel.Trace => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E")),
                    LogLevel.Debug => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")),
                    LogLevel.Information => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
                    LogLevel.Warning => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")),
                    LogLevel.Error => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")),
                    LogLevel.Critical => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B71C1C")),
                    _ => Brushes.Black
                };
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
