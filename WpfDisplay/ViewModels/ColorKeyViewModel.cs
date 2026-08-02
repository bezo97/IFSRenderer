using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using IFSEngine.Model;

namespace WpfDisplay.ViewModels;

/// <summary>
/// Viewmodel wrapping a single color key for the palette editor.
/// </summary>
public partial class ColorKeyViewModel : ObservableObject
{
    private PaletteEditorViewModel? _editorVm;

    [ObservableProperty]
    public partial double Position { get; set; }

    [ObservableProperty]
    public partial Color Color { get; set; }

    public ColorKeyViewModel(double position, System.Numerics.Vector4 color)
    {
        Position = position;
        Color = Color.FromRgb((byte)(color.X * 255), (byte)(color.Y * 255), (byte)(color.Z * 255));
    }

    /// <summary>
    /// Set after construction to avoid modifying the palette during enumeration.
    /// </summary>
    public void SetEditorVm(PaletteEditorViewModel editorVm)
    {
        _editorVm = editorVm;
    }

    partial void OnPositionChanged(double value)
    {
        // Sync back to editor VM (debounced via slider MouseUp handler)
    }

    partial void OnColorChanged(Color value)
    {
        _editorVm?.UpdateKeyColor(this, value);
    }

    [RelayCommand]
    private void Remove()
    {
        _editorVm?.RemoveKey(this);
    }

    [RelayCommand]
    private void Duplicate()
    {
        _editorVm?.DuplicateKey(this);
    }
}
