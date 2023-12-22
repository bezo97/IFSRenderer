using System;
using System.Globalization;
using System.Windows.Data;

namespace WpfDisplay.Converters;

internal class ParameterEqualityToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => parameter.Equals(value);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if ((bool)value)
            return parameter;
        return null;
    }
}
