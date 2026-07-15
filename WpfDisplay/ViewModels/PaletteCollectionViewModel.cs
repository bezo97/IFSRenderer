using System.Collections.Generic;
using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using IFSEngine.Model;

namespace WpfDisplay.ViewModels;

/// <summary>
/// Viewmodel wrapping a <see cref="PaletteCollection"/> for the browser.
/// </summary>
public partial class PaletteCollectionViewModel : ObservableObject
{
    public PaletteCollection Collection { get; }

    [ObservableProperty]
    public partial bool IsExpanded { get; set; } = true;
    public ObservableCollection<ColorPaletteViewModel> Palettes { get; } = [];

    public int PaletteCount => Palettes.Count;

    public PaletteCollectionViewModel(PaletteCollection collection)
    {
        Collection = collection;
    }

    public void RefreshPalettes(IEnumerable<ColorPaletteViewModel> paletteVms)
    {
        Palettes.Clear();
        foreach (var vm in paletteVms)
            Palettes.Add(vm);
    }

    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
    }
}
