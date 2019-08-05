using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfDisplay.Converters
{
    public class BetterBooleanToVisibilityConverter : BooleanConverter<Visibility>
    {

        public BetterBooleanToVisibilityConverter() :
            base(Visibility.Visible, Visibility.Collapsed)
        { }
    }
}
