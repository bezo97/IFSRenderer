using System;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using IFSEngine.Model;
using WpfDisplay.Services;

namespace WpfDisplay.ViewModels;

/// <summary>
/// Main viewmodel for the Palette Manager window.
/// Manages page switching (browser ↔ editor) and provides the library service.
/// </summary>
public partial class PaletteManagerViewModel : ObservableObject
{
    public PaletteBrowserViewModel BrowserViewModel { get; }
    public PaletteEditorViewModel? EditorViewModel { get; private set; }

    [ObservableProperty]
    public partial bool IsEditorPage { get; set; }

    public PaletteManagerViewModel(PaletteLibraryService library, MainViewModel mainVm)
    {
        BrowserViewModel = new PaletteBrowserViewModel(library, mainVm);
    }

    public async Task InitializeAsync()
    {
        await BrowserViewModel.LoadAsync();
    }

    [RelayCommand]
    private void NavigateToBrowser()
    {
        IsEditorPage = false;
    }

    public void NavigateToEditor(ColorPalette palette, PaletteCollection? collection, bool isNew = false)
    {
        EditorViewModel = new PaletteEditorViewModel(BrowserViewModel.Library, BrowserViewModel.MainVm, palette, collection, isNew);
        EditorViewModel.OnReturnToBrowser = async () =>
        {
            // After returning, reload browser to reflect changes
            await BrowserViewModel.ReloadAsync();
            // Navigation back to browser is handled by the window
            OnReturnToBrowser?.Invoke();
        };

        // Build collection palette list for left column
        if (collection != null)
        {
            foreach (var p in collection.Palettes)
                EditorViewModel.CollectionPalettes.Add(new ColorPaletteViewModel(p));
        }
    }

    public event Action? OnReturnToBrowser;
}
