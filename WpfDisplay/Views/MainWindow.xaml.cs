#nullable enable
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using IFSEngine.Model;
using IFSEngine.Rendering;

using WpfDisplay.Helper;
using WpfDisplay.Models;
using WpfDisplay.Properties;
using WpfDisplay.Serialization;
using WpfDisplay.Services;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private EditorWindow _editorWindow;
    private GeneratorWindow _generatorWindow;
    private MainViewModel vm => (MainViewModel)DataContext;
    private PaletteManagerWindow _paletteManagerWindow;

    public MainWindow()
    {
        InitializeComponent();

        MaterialDesignThemes.Wpf.ShadowAssist.SetCacheMode(this, null);//disable gpu cache
        animationsPanel.ToggleAutoHide();
        ContentRendered += MainWindow_ContentRendered;
    }

    private async void MainWindow_ContentRendered(object sender, System.EventArgs e)
    {
        FixAutoDockHeight();


        //init workspace
        var renderer = new RendererGL(mainDisplay.GraphicsContext);
        mainDisplay.AttachRenderer(renderer);
        var workspace = new Workspace(renderer);
        await workspace.Initialize();

        var workflow = WelcomeWorkflow.FromScratch;
        if (App.OpenVerbPath is not null)
        {//handle open verb, no welcome screen
            IFS ifs;
            try
            {
                ifs = IfsNodesSerializer.LoadJsonFile(App.OpenVerbPath, workspace.LoadedTransforms, true);
            }
            catch (SerializationException)
            {
                MessageBox.Show(this, $"Failed to load params from '{App.OpenVerbPath}'");
                ifs = new IFS();
            }
            workspace.LoadParams(ifs, App.OpenVerbPath);
        }
        else if (App.OpenVerbPath is null && Settings.Default.IsWelcomeShownOnStartup)
        {
            var welcomeViewModel = new WelcomeViewModel(workspace.IncludeSources, workspace.LoadedTransforms);
            var welcomeWindow = new WelcomeWindow
            {
                Owner = this,
                DataContext = welcomeViewModel
            };
            welcomeWindow.ShowDialog();
            Focus();

            //TODO: refactor this block below
            workflow = welcomeViewModel.SelectedWorkflow;
            if (workflow == WelcomeWorkflow.Explore)
                workspace.LoadParams(welcomeViewModel.ExploreParams, null);
            else if (workflow == WelcomeWorkflow.LoadRecent)
                workspace.LoadParams(welcomeViewModel.ExploreParams, welcomeViewModel.SelectedFilePath);
            else if (workflow == WelcomeWorkflow.LoadFromFile)
            {
                try
                {
                    var ext = Path.GetExtension(welcomeViewModel.SelectedFilePath);
                    if (ext == ".ifsjson")
                        workspace.LoadParamsFileAsync(welcomeViewModel.SelectedFilePath, false).Wait();
                    else if (ext == ".png")
                    {
                        var pngImge = BitmapFrame.Create(new Uri(welcomeViewModel.SelectedFilePath));
                        if (PngMetadataHelper.TryExtractParamsFromImage(pngImge, workspace.LoadedTransforms, out var ifs))
                            workspace.LoadParams(ifs, null);
                    }
                }
                catch
                {
                    workspace.UpdateStatusText($"Failed to load params - {welcomeViewModel.SelectedFilePath}");
                }
            }

        }

        DataContext = new MainViewModel(workspace, workflow);

        if (workflow == WelcomeWorkflow.BrowseRandoms)
            GeneratorButton_Click(null, null);
        if (workflow == WelcomeWorkflow.VisitSettings)
            SettingsButton_Click(null, null);

        vm.AnimationViewModel.Channels.CollectionChanged += (s, e) =>
        {
            if (vm.workspace.Ifs.Dopesheet.Channels.Count > 0)
                ShowAnimationsPanel();
        };
    }

    /// <summary>
    /// ad hoc fix for issue <a href="https://github.com/Dirkster99/AvalonDock/issues/298"/>
    /// </summary>
    private void FixAutoDockHeight()
    {
        tonemappingPane.DockHeight = GridLength.Auto;
        environmentPane.DockHeight = GridLength.Auto;
        performancePane.DockHeight = GridLength.Auto;
    }

    private void GeneratorButton_Click(object sender, RoutedEventArgs e)
    {
        //create window
        if (_generatorWindow == null || !_generatorWindow.IsLoaded)
        {
            _generatorWindow = new GeneratorWindow
            {
                Owner = this
            };
            var generatorViewModel = new GeneratorViewModel(vm);
            _generatorWindow.DataContext = generatorViewModel;
        }

        if (_generatorWindow.ShowActivated)
            _generatorWindow.Show();
        //bring to foreground
        if (!_generatorWindow.IsActive)
            _generatorWindow.Activate();
    }

    private void EditorButton_Click(object sender, RoutedEventArgs e)
    {
        //create window
        if (_editorWindow == null || !_editorWindow.IsLoaded)
        {
            _editorWindow = new EditorWindow
            {
                Owner = this
            };
            _editorWindow.SetBinding(DataContextProperty, new Binding(".") { Source = vm.IFSViewModel, Mode = BindingMode.TwoWay });
        }

        if (_editorWindow.ShowActivated)
            _editorWindow.Show();
        //bring to foreground
        if (!_editorWindow.IsActive)
            _editorWindow.Activate();
    }

    private void PalettesButton_Click(Object sender, RoutedEventArgs e)
    {
        OpenPaletteManagerWindow(false);
    }

    private ColorPalette? OpenPaletteManagerWindow(bool isSelector)
    {
        //create window
        if (_paletteManagerWindow == null || !_paletteManagerWindow.IsLoaded)
        {
            _paletteManagerWindow = new PaletteManagerWindow
            {
                Owner = this
            };
            var generatorViewModel = new PaletteManagerViewModel(vm);
            _paletteManagerWindow.DataContext = generatorViewModel;
        }


        if (isSelector)
        {
            if (_paletteManagerWindow.ShowDialog() == true)
                return ((PaletteManagerViewModel)_paletteManagerWindow.DataContext).SelectedPalette?.Palette;
        }
        else
        {
            if (_paletteManagerWindow.ShowActivated)
                _paletteManagerWindow.Show();
            //bring to foreground
            if (!_paletteManagerWindow.IsActive)
                _paletteManagerWindow.Activate();
        }

        return null;
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow
        {
            Owner = this,
            DataContext = new SettingsViewModel(vm)
        };
        if (settingsWindow.ShowDialog() == true)
            vm.StatusBarText = "Settings saved.";
    }

    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AboutDialogWindow
        {
            Owner = this
        };
        dialog.ShowDialog();
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        if (vm.workspace.HasUnsavedChanges)
        {
            var dialog = new ExitDialogWindow
            {
                Owner = this
            };
            dialog.ShowDialog();
            if (dialog.ExitDialogResult == ExitDialogWindow.ExitChoice.Cancel)
            {
                e.Cancel = true;
                base.OnClosing(e);
                return;
            }
            else if (dialog.ExitDialogResult == ExitDialogWindow.ExitChoice.Save)
            {
                await vm.SaveParamsAsCommand.ExecuteAsync(null);
            }
        }

        _generatorWindow?.Close();
        _editorWindow?.Close();

        await vm.DisposeAsync();
        Application.Current.Shutdown();

        base.OnClosing(e);
    }

    private void Undo_Executed(object sender, ExecutedRoutedEventArgs e) => vm.IFSViewModel.UndoCommand.Execute(null);

    private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = vm?.IFSViewModel.UndoCommand.CanExecute(null) ?? false;

    private void Redo_Executed(object sender, ExecutedRoutedEventArgs e) => vm.IFSViewModel.RedoCommand.Execute(null);

    private void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = vm?.IFSViewModel.RedoCommand.CanExecute(null) ?? false;

    private void Copy_Executed(object sender, ExecutedRoutedEventArgs e) => vm.CopyClipboardParamsCommand.Execute(null);

    private void Copy_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = vm?.CopyClipboardParamsCommand.CanExecute(null) ?? false;

    private void Paste_Executed(object sender, ExecutedRoutedEventArgs e) => vm.PasteClipboardParamsCommand.Execute(null);

    private void Paste_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = vm?.PasteClipboardParamsCommand.CanExecute(null) ?? false;

    private void mainWindow_DragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
        e.Effects = FileDropHandler.GetDragDropEffect(e.Data);
    }

    private void mainWindow_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        vm.DropFileCommand.Execute(e.Data);
    }

    private void dockManager_DocumentClosing(object sender, AvalonDock.DocumentClosingEventArgs e) => vm.workspace.LoadParams(IFS.Default, null);

    private async void mainDisplay_GamepadConnectionStateChanged(object sender, bool e) => await Dispatcher.InvokeAsync(() => vm.IsGamepadConnected = e);

    private void mainDisplay_DisplayResolutionChanged(object sender, System.EventArgs e) => vm?.QualitySettingsViewModel?.UpdatePreviewRenderSettings();

    private void ShowAnimationsPanel()
    {
        if (animationsPanel.IsAutoHidden)
            animationsPanel.ToggleAutoHide();
    }

    private void animationsPanel_IsActiveChanged(object sender, System.EventArgs e)
    {
        //Avoid leaving the animation panel in a half-open state called "AutoHide" in AvalonDock.
        //This way the user does not need to click the pin button to dock the panel after opening it.
        if (animationsPanel.IsActive)
            ShowAnimationsPanel();
    }
}
