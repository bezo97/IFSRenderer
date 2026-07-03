#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using IFSEngine.Generation;
using IFSEngine.Model;

namespace WpfDisplay.ViewModels;

public partial class PaletteManagerViewModel : ObservableObject
{
    public List<ColorPaletteViewModel> FavoritePalettes => LibraryPalettes.Where(p => p.IsFavorite).ToList();
    public List<PaletteCollection> PaletteCollections { get; private set; } = [];
    public List<ColorPaletteViewModel> LibraryPalettes { get; private set; } = [];

    [ObservableProperty] private ColorPaletteViewModel? _selectedPalette = null;

    private readonly MainViewModel _mainvm = null!;

    public PaletteManagerViewModel() { } // for design-time use
    public PaletteManagerViewModel(MainViewModel mainvm)
    {
        _mainvm = mainvm;

        //TODO: mock data - will be replaced with library service in Phase 2
        LibraryPalettes = Enumerable.Repeat(0, 10).Select(n => new ColorPaletteViewModel()
        {
            Palette = IqPaletteGenerator.Generate(
                new System.Numerics.Vector4(0.6f, 0.6f, 0.6f, 1f),
                new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1f),
                new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1f),
                new System.Numerics.Vector4((float)Random.Shared.NextDouble(), (float)Random.Shared.NextDouble(), (float)Random.Shared.NextDouble(), 1f),
                InterpolationMode.Mixbox,
                10),
            IsFavorite = Random.Shared.NextDouble() > 0.5
        }).ToList();
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
        var palette = new ColorPaletteViewModel
        {
            Palette = ColorPalette.Default
        };
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
