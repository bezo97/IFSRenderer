using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace WpfDisplay.Converters
{
    public class ColorTypeConverter : IValueConverter
    {
        //drawing -> media
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var c = (System.Drawing.Color)value;
            return System.Windows.Media.Color.FromRgb(c.R, c.G, c.B);
        }

        //media -> convert
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var c = (System.Windows.Media.Color)value;
            return System.Drawing.Color.FromArgb(255, c.R, c.G, c.B);
        }
    }
}
