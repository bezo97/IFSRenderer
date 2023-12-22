using System.Windows;

using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

/// <summary>
/// Interaction logic for SettingsWindow.xaml
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            SettingsViewModel vm = (SettingsViewModel)DataContext;
            vm.SettingsSaved += (s2, e2) => DialogResult = true;
            vm.SettingsCanceled += (s2, e2) => DialogResult = false;
        };
    }
}
