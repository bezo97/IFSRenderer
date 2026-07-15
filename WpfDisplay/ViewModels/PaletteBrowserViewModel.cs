using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using IFSEngine.Model;
using IFSEngine.Services;

using WpfDisplay.Helper;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

/// <summary>
/// Viewmodel for the palette browser page.
/// Manages collections, palettes, favorites, sorting, filtering, grouping, and zoom.
/// </summary>
public partial class PaletteBrowserViewModel : ObservableObject
{
    private readonly PaletteLibraryService _library;
    private readonly MainViewModel _mainVm;
    private readonly Workspace _workspace;
    private HashSet<Guid> _favorites = [];

    [ObservableProperty]
    public partial ObservableCollection<PaletteCollectionViewModel> Collections { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ColorPaletteViewModel> FavoritePalettes { get; set; } = [];

    [ObservableProperty]
    public partial ColorPaletteViewModel? SelectedPalette { get; set; }

    [ObservableProperty]
    public partial PaletteSortMode SortMode { get; set; }

    [ObservableProperty]
    public partial bool GroupByCollection { get; set; } = true;

    [ObservableProperty]
    public partial string SearchText { get; set; } = "";

    [ObservableProperty]
    public partial double Zoom { get; set; } = 200;

    /// <summary>Available sort modes for the UI.</summary>
    public static PaletteSortMode[] AllSortModes { get; } = (PaletteSortMode[])Enum.GetValues(typeof(PaletteSortMode));

    [ObservableProperty]
    public partial ImageSource? FractalPreviewImage { get; set; }

    // Flat list (used when not grouping)
    private ObservableCollection<ColorPaletteViewModel> _allPalettes = [];

    public ObservableCollection<ColorPaletteViewModel> AllPalettes => _allPalettes;

    [ObservableProperty]
    public partial ObservableCollection<PaletteCollectionViewModel> FilteredCollections { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ColorPaletteViewModel> FilteredAllPalettes { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<ColorPaletteViewModel> FilteredFavorites { get; set; } = [];
    public bool HasCollections
    {
        get => Collections.Count > 0;
    }

    public bool HasFavorites
    {
        get => FavoritePalettes.Count > 0;
    }

    public bool HasFilteredFavorites
    {
        get => FilteredFavorites.Count > 0;
    }

    // Preview palette gradient stops
    public GradientStopCollection? PreviewGradientStops
    {
        get => SelectedPalette?.GradientStops;
    }

    public Color? PreviewBackgroundColor
    {
        get => SelectedPalette?.BackgroundColor;
    }

    public PaletteBrowserViewModel(PaletteLibraryService library, MainViewModel mainVm)
    {
        _library = library;
        _mainVm = mainVm;
        _workspace = mainVm.workspace;
    }

    public async Task LoadAsync()
    {
        var collections = await _library.LoadCollectionsAsync();
        _favorites = await _library.GetFavoritesAsync();

        BuildViewModels(collections);
    }

    public async Task ReloadAsync()
    {
        var collections = await _library.ReloadCollectionsAsync();
        BuildViewModels(collections);
    }

    private void BuildViewModels(List<PaletteCollection> collections)
    {
        // Build flat palette list
        var allPaletteVms = collections
            .SelectMany(c => c.Palettes.Select(p => new ColorPaletteViewModel(p)))
            .ToList();

        // Set favorite state
        foreach (var vm in allPaletteVms)
            vm.IsFavorite = _favorites.Contains(vm.Palette.Id);

        // Sort
        allPaletteVms = SortPalettes(allPaletteVms).ToList();

        // Update collections
        Collections.Clear();
        foreach (var collection in collections)
        {
            var cvm = new PaletteCollectionViewModel(collection);
            var collectionPalettes = allPaletteVms.Where(p => collection.Palettes.Any(orig => orig.Id == p.Palette.Id)).ToList();
            cvm.RefreshPalettes(collectionPalettes);
            Collections.Add(cvm);
        }

        // Update favorites
        FavoritePalettes.Clear();
        foreach (var vm in allPaletteVms.Where(p => p.IsFavorite))
            FavoritePalettes.Add(vm);

        // Update flat list
        _allPalettes.Clear();
        foreach (var vm in allPaletteVms)
            _allPalettes.Add(vm);

        OnPropertyChanged(nameof(HasCollections));
        OnPropertyChanged(nameof(HasFavorites));

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        string search = SearchText.ToLowerInvariant().Trim();

        // Filter all palettes
        IEnumerable<ColorPaletteViewModel> filtered = _allPalettes;
        if (!string.IsNullOrEmpty(search))
            filtered = _allPalettes.Where(p =>
                p.Palette.Name.ToLowerInvariant().Contains(search) ||
                p.Palette.Tags.Any(t => t.ToLowerInvariant().Contains(search))
            );

        // Update filtered flat list
        FilteredAllPalettes.Clear();
        foreach (var vm in filtered)
            FilteredAllPalettes.Add(vm);

        // Update filtered collections (only show collections with matching palettes)
        FilteredCollections.Clear();
        foreach (var cvm in Collections)
        {
            var matchingPalettes = filtered.Where(p =>
                cvm.Palettes.Any(orig => orig.Palette.Id == p.Palette.Id)
            ).ToList();

            if (matchingPalettes.Any() || string.IsNullOrEmpty(search))
            {
                var clone = new PaletteCollectionViewModel(cvm.Collection)
                {
                    IsExpanded = cvm.IsExpanded
                };
                clone.RefreshPalettes(matchingPalettes);
                FilteredCollections.Add(clone);
            }
        }

        // Update filtered favorites
        FilteredFavorites.Clear();
        foreach (var vm in FavoritePalettes)
        {
            if (string.IsNullOrEmpty(search) ||
                vm.Palette.Name.ToLowerInvariant().Contains(search) ||
                vm.Palette.Tags.Any(t => t.ToLowerInvariant().Contains(search)))
            {
                FilteredFavorites.Add(vm);
            }
        }

        OnPropertyChanged(nameof(HasFavorites));
        OnPropertyChanged(nameof(HasFilteredFavorites));
    }

    private IEnumerable<ColorPaletteViewModel> SortPalettes(IEnumerable<ColorPaletteViewModel> palettes)
    {
        return SortMode switch
        {
            PaletteSortMode.Name => palettes.OrderBy(p => p.Palette.Name),
            PaletteSortMode.DominantHue => palettes.OrderBy(p => p.DominantHue),
            PaletteSortMode.Warmth => palettes.OrderByDescending(p => p.Warmth),
            PaletteSortMode.Brightness => palettes.OrderBy(p => p.Brightness),
            PaletteSortMode.Saturation => palettes.OrderBy(p => p.Saturation),
            PaletteSortMode.KeyCount => palettes.OrderBy(p => p.KeyCount),
            _ => palettes
        };
    }

    [RelayCommand]
    private void SelectPalette(ColorPaletteViewModel palette)
    {
        SelectedPalette = palette;
        UpdateFractalPreviewAsync();
    }

    private async void UpdateFractalPreviewAsync()
    {
        // Fractal preview is deferred to Phase 2.5 / Phase 5 integration
        // For now, just invalidate the palette buffer so the main render updates
        if (SelectedPalette != null)
        {
            _workspace.Renderer.InvalidatePaletteBuffer();
        }
    }

    [RelayCommand]
    private async Task ToggleFavorite(ColorPaletteViewModel palette)
    {
        palette.IsFavorite = !palette.IsFavorite;
        await _library.SetFavoriteAsync(palette.Palette.Id, palette.IsFavorite);

        if (palette.IsFavorite)
            FavoritePalettes.Add(palette);
        else
            FavoritePalettes.Remove(palette);

        OnPropertyChanged(nameof(HasFavorites));
        ApplyFilter();
    }

    [RelayCommand]
    private async Task CreateCollection()
    {
        var collection = await _library.AddCollectionAsync("New Collection");
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task DeleteCollection(PaletteCollectionViewModel collectionVm)
    {
        var result = MessageBox.Show(
            $"Delete collection \"{collectionVm.Collection.Name}\" and all its palettes?",
            "Delete Collection",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            await _library.RemoveCollectionAsync(collectionVm.Collection.Id);
            SelectedPalette = null;
            await ReloadAsync();
        }
    }

    [RelayCommand]
    private async Task RenameCollection(PaletteCollectionViewModel collectionVm)
    {
        var newName = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter new name:", "Rename Collection", collectionVm.Collection.Name);
        if (!string.IsNullOrWhiteSpace(newName) && newName != collectionVm.Collection.Name)
        {
            collectionVm.Collection.Name = newName;
            await _library.SaveCollectionAsync(collectionVm.Collection);
        }
    }

    [RelayCommand]
    private async Task EditCollectionDescription(PaletteCollectionViewModel collectionVm)
    {
        var newDesc = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter description:", "Edit Description", collectionVm.Collection.Description);
        if (newDesc != collectionVm.Collection.Description)
        {
            collectionVm.Collection.Description = newDesc;
            await _library.SaveCollectionAsync(collectionVm.Collection);
        }
    }

    [RelayCommand]
    private async Task ExportCollection(PaletteCollectionViewModel collectionVm)
    {
        if (collectionVm.PaletteCount == 0)
        {
            MessageBox.Show("Cannot export an empty collection.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DialogHelper.ShowExportGradientDialog(collectionVm.Collection.Name, out string path))
        {
            try
            {
                await _library.ExportCollectionAsync(collectionVm.Collection.Id, path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private async Task ImportFlameFile()
    {
        if (DialogHelper.ShowOpenGradientDialog(out string path))
        {
            try
            {
                var collection = await _library.ImportFlameFileAsync(path);
                if (collection.Palettes.Count == 0)
                {
                    MessageBox.Show("No palettes found in the file.", "Import Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                await ReloadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to import: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private async Task DuplicatePalette(ColorPaletteViewModel paletteVm)
    {
        if (SelectedPalette == null) return;

        // Find the collection this palette belongs to
        Guid? collectionId = null;
        foreach (var cvm in Collections)
        {
            if (cvm.Palettes.Any(p => p.Palette.Id == paletteVm.Palette.Id))
            {
                collectionId = cvm.Collection.Id;
                break;
            }
        }

        if (collectionId == null) return;

        var original = paletteVm.Palette;
        var copy = new ColorPalette
        {
            Name = original.Name + " (Copy)",
            BackgroundColor = original.BackgroundColor,
            InterpolationMode = original.InterpolationMode,
            Author = original.Author,
        };
        foreach (var kvp in original.KeyColors)
            copy.KeyColors[kvp.Key] = kvp.Value;
        copy.Tags = new System.Collections.Generic.List<string>(original.Tags);

        await _library.AddPaletteAsync(collectionId.Value, copy);
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task DeletePalette(ColorPaletteViewModel paletteVm)
    {
        var result = MessageBox.Show(
            $"Delete palette \"{paletteVm.Palette.Name}\"?",
            "Delete Palette",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            await _library.RemovePaletteAsync(paletteVm.Palette.Id);
            if (SelectedPalette?.Palette.Id == paletteVm.Palette.Id)
                SelectedPalette = null;
            await ReloadAsync();
        }
    }

    [RelayCommand]
    private async Task CopyPaletteTo((ColorPaletteViewModel palette, PaletteCollectionViewModel target) args)
    {
        var paletteVm = args.palette;
        var targetCollectionVm = args.target;
        var copy = new ColorPalette
        {
            Name = paletteVm.Palette.Name + " (Copy)",
            BackgroundColor = paletteVm.Palette.BackgroundColor,
            InterpolationMode = paletteVm.Palette.InterpolationMode,
            Author = paletteVm.Palette.Author,
        };
        foreach (var kvp in paletteVm.Palette.KeyColors)
            copy.KeyColors[kvp.Key] = kvp.Value;
        copy.Tags = new System.Collections.Generic.List<string>(paletteVm.Palette.Tags);

        await _library.AddPaletteAsync(targetCollectionVm.Collection.Id, copy);
        await ReloadAsync();
    }

    [RelayCommand]
    private void ApplyToFractal()
    {
        if (SelectedPalette == null) return;

        _workspace.TakeSnapshot();
        _workspace.Ifs.Palette = SelectedPalette.Palette;
        _workspace.Renderer.InvalidatePaletteBuffer();
    }

    [RelayCommand]
    private async Task UpdatePaletteName(string newName)
    {
        if (SelectedPalette == null) return;
        SelectedPalette.Palette.Name = newName;
        await SaveSelectedPaletteCollection();
    }

    private async Task SaveSelectedPaletteCollection()
    {
        if (SelectedPalette == null) return;

        // Find the collection containing this palette
        foreach (var cvm in Collections)
        {
            if (cvm.Palettes.Any(p => p.Palette.Id == SelectedPalette.Palette.Id))
            {
                await _library.SaveCollectionAsync(cvm.Collection);
                return;
            }
        }
    }

    partial void OnZoomChanged(double value)
    {
        // Persist zoom preference
        // TODO: Settings.Default.PaletteZoom = value;
    }

    partial void OnGroupByCollectionChanged(bool value)
    {
        // Triggered when grouping toggle changes
    }

    partial void OnSortModeChanged(PaletteSortMode value)
    {
        RebuildSortedLists();
    }

    private void RebuildSortedLists()
    {
        var sorted = SortPalettes(_allPalettes).ToList();
        _allPalettes.Clear();
        foreach (var vm in sorted)
            _allPalettes.Add(vm);

        // Rebuild collection palette lists
        foreach (var cvm in Collections)
        {
            var collectionPalettes = sorted.Where(p =>
                cvm.Palettes.Any(orig => orig.Palette.Id == p.Palette.Id)
            ).ToList();
            cvm.RefreshPalettes(collectionPalettes);
        }

        // Rebuild favorites
        FavoritePalettes.Clear();
        foreach (var vm in _allPalettes.Where(p => p.IsFavorite))
            FavoritePalettes.Add(vm);

        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }
}
