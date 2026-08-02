using System;
using System.Windows;

using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

/// <summary>
/// Palette Manager window. Single instance, managed by MainWindow.
/// </summary>
public partial class PaletteManagerWindow : Window
{
    private PaletteManagerViewModel? Vm => DataContext as PaletteManagerViewModel;

    public PaletteManagerWindow()
    {
        InitializeComponent();
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        Loaded += PaletteManagerWindow_Loaded;
    }

    private void PaletteManagerWindow_Loaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= PaletteManagerWindow_Loaded;

        if (Vm is { } vm)
        {
            // Navigate to browser page
            NavigationFrame.Navigate(new PaletteBrowserPage { DataContext = vm.BrowserViewModel });

            vm.BrowserViewModel.OnOpenEditor += OnOpenEditor;
            vm.OnReturnToBrowser += OnReturnToBrowser;
        }
    }

    private void OnReturnToBrowser()
    {
        NavigationFrame.Navigate(new PaletteBrowserPage { DataContext = Vm?.BrowserViewModel });
    }

    private void OnOpenEditor(IFSEngine.Model.ColorPalette palette, IFSEngine.Model.PaletteCollection? collection)
    {
        Vm?.NavigateToEditor(palette, collection);
        if (Vm?.EditorViewModel is { } editorVm)
        {
            NavigationFrame.Navigate(new PaletteEditorPage { DataContext = editorVm });
        }
    }

    protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        base.OnClosing(e);

        // If editor is open with unsaved changes, prompt user
        if (Vm?.EditorViewModel is { HasUnsavedChanges: true } editorVm)
        {
            e.Cancel = true;
            bool proceed = await editorVm.PromptSaveAsync();
            if (proceed)
                Close();
        }
        else
        {
            // Clean up event
            if (Vm?.BrowserViewModel is { } browserVm)
            {
                browserVm.OnOpenEditor -= OnOpenEditor;
            }
        }
    }
}
