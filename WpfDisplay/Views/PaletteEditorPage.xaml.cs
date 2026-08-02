#nullable enable
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

public partial class PaletteEditorPage : Page
{
    private PaletteEditorViewModel? Vm => DataContext as PaletteEditorViewModel;

    public PaletteEditorPage()
    {
        InitializeComponent();
    }

    // Sync slider value back to VM on mouse up (avoid continuous updates during drag)
    private void KeyPositionSlider_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (Vm?.SelectedKey is { } key && sender is Slider slider)
        {
            Vm.UpdateKeyPosition(key, slider.Value);
        }
        e.Handled = true;
    }
}
