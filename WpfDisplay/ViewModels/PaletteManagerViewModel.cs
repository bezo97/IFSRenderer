using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using IFSEngine.Services;

namespace WpfDisplay.ViewModels;

/// <summary>
/// Main viewmodel for the Palette Manager window.
/// Manages page switching (browser ↔ editor) and provides the library service.
/// </summary>
public partial class PaletteManagerViewModel : ObservableObject
{
    public PaletteBrowserViewModel BrowserViewModel { get; }

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
}
