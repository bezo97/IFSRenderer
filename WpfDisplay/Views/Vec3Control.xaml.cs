using System.Windows;
using System.Windows.Controls;

using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

/// <summary>
/// Interaction logic for Vec3Control.xaml
/// </summary>
public partial class Vec3Control : UserControl
{
    public double XValue
    {
        get => (double)GetValue(XValueProperty);
        set => SetValue(XValueProperty, value);
    }
    public static readonly DependencyProperty XValueProperty =
        DependencyProperty.Register("XValue", typeof(double), typeof(Vec3Control), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


    public double YValue
    {
        get => (double)GetValue(YValueProperty);
        set => SetValue(YValueProperty, value);
    }
    public static readonly DependencyProperty YValueProperty =
        DependencyProperty.Register("YValue", typeof(double), typeof(Vec3Control), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));


    public double ZValue
    {
        get => (double)GetValue(ZValueProperty);
        set => SetValue(ZValueProperty, value);
    }
    public static readonly DependencyProperty ZValueProperty =
        DependencyProperty.Register("ZValue", typeof(double), typeof(Vec3Control), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public Vec3ParamViewModel Vec3Settings
    {
        get => (Vec3ParamViewModel)GetValue(Vec3SettingsProperty);
        set => SetValue(Vec3SettingsProperty, value);
    }
    public static readonly DependencyProperty Vec3SettingsProperty =
        DependencyProperty.Register("Vec3Settings", typeof(Vec3ParamViewModel), typeof(Vec3Control), new PropertyMetadata(null));

    public Vec3Control()
    {
        InitializeComponent();
    }
}
