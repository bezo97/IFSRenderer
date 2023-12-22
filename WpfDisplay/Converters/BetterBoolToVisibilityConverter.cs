using System.Windows;

namespace WpfDisplay.Converters;

public class BetterBooleanToVisibilityConverter : BooleanConverter<Visibility>
{

    public BetterBooleanToVisibilityConverter() :
        base(Visibility.Visible, Visibility.Collapsed)
    { }
}
