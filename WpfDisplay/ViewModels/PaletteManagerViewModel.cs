#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using IFSEngine.Model;

namespace WpfDisplay.ViewModels;

public partial class PaletteManagerViewModel : ObservableObject
{
    public List<ColorPaletteViewModel> FavoritePalettes => LibraryPalettes.Where(p => p.IsFavorite).ToList();
    public List<ColorPaletteViewModel> LibraryPalettes { get; private set; } = [];

    [ObservableProperty] private ColorPaletteViewModel? _selectedPalette = null;

    private readonly MainViewModel _mainvm = null!;

    public PaletteManagerViewModel() { } // for design-time use
    public PaletteManagerViewModel(MainViewModel mainvm)
    {
        _mainvm = mainvm;

        //TODO: mock
        LibraryPalettes = Enumerable.Repeat(0, 10).Select(n => new ColorPaletteViewModel()
        {
            Palette = IFSEngine.Generation.Generator.GenerateRandomIqPalette(),
            IsFavorite = Random.Shared.NextDouble() > 0.5
        }).ToList();
        LibraryPalettes.ForEach(p => p.Palette.ComputeGradientSamples(256));

    }

    [RelayCommand]
    public void ClickPalette(ColorPaletteViewModel palette)
    {
        SelectedPalette = palette;
    }

    [RelayCommand]
    public void ToggleFavorite(ColorPaletteViewModel palette)
    {
        palette.IsFavorite = !palette.IsFavorite;
        OnPropertyChanged(nameof(FavoritePalettes));
    }

    [RelayCommand]
    public void AddPalette()
    {
        var palette = new ColorPaletteViewModel();
        LibraryPalettes.Add(palette);
        SelectedPalette = palette;
        OnPropertyChanged(nameof(LibraryPalettes));
        OnPropertyChanged(nameof(FavoritePalettes));
    }

    [RelayCommand]
    public void RemoveSelectedPalette()
    {
        if (SelectedPalette == null) throw new InvalidOperationException("No palette selected.");
        LibraryPalettes.Remove(SelectedPalette);
        SelectedPalette = null;
        OnPropertyChanged(nameof(LibraryPalettes));
        OnPropertyChanged(nameof(FavoritePalettes));
    }
}
