using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfDisplay.Converters;

public class NullVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value != null;
        if (parameter != null)
            b = !b;//invert with parameter
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
