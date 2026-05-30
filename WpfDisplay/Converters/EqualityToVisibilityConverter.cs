using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WpfDisplay.Converters;

internal class EqualityToVisibilityConverter : IValueConverter
{
    public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value == parameter ? Visibility.Visible : Visibility.Collapsed;

    public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();

}
