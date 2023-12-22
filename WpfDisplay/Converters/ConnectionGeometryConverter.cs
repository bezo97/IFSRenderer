using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WpfDisplay.Converters;

[ValueConversion(typeof(object), typeof(Geometry))]
public class ConnectionGeometryConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        PathGeometry g1 = (PathGeometry)values[0];
        EllipseGeometry g2 = (EllipseGeometry)values[1];
        bool isLoopbackConnection = (bool)values[2];//parameter;
        if (isLoopbackConnection)
            return g2;
        else
            return g1;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
