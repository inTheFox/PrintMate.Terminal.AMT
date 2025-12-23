using System;
using System.Windows.Data;
using PrintMate.Terminal.Services;

namespace PrintMate.Terminal.ViewModels;

public class LogMessageTypeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        switch (value)
        {
            case LogMessageType.Info: return System.Windows.Media.Brushes.White;
            case LogMessageType.Warning: return System.Windows.Media.Brushes.DarkOrange;
            case LogMessageType.Success: return System.Windows.Media.Brushes.LightGreen;
            case LogMessageType.Error: return System.Windows.Media.Brushes.IndianRed;
            default: return System.Windows.Media.Brushes.Gray;

        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}