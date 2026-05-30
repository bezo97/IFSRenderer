using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace WpfDisplay.Converters;

public class BooleanConverter<T> : IValueConverter
{
    public BooleanConverter() { }

    public BooleanConverter(T trueValue, T falseValue)
    {
        True = trueValue;
        False = falseValue;
    }

    public T True { get; set; }
    public T False { get; set; }

    public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is bool boolean && boolean ? True : False;

    public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is T t && EqualityComparer<T>.Default.Equals(t, True);
}

public class BooleanConverter : BooleanConverter<bool>;
