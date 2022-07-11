using IFSEngine.Model;
using IFSEngine.Rendering;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using WpfDisplay.Models;
using WpfDisplay.Serialization;
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

    public MainWindow()
    {
        InitializeComponent();
        ContentRendered += MainWindow_ContentRendered;
    }

    private async void MainWindow_ContentRendered(object sender, System.EventArgs e)
    {
        //init workspace
        var renderer = new RendererGL(mainDisplay.GraphicsContext);
        mainDisplay.AttachRenderer(renderer);
        var workspace = new Workspace(renderer);
        await workspace.Initialize();

        //handle open verb
        if (App.OpenVerbPath is not null)
        {
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
            workspace.LoadParams(ifs);
        }

        DataContext = new MainViewModel(workspace);
    }

    private async void GeneratorButton_Click(object sender, RoutedEventArgs e)
    {
        //create window
        if (_generatorWindow == null || !_generatorWindow.IsLoaded)
        {
            _generatorWindow = new GeneratorWindow();
            var generatorViewModel = new GeneratorViewModel(vm);
            _generatorWindow.DataContext = generatorViewModel;

            await generatorViewModel.Initialize();
        }

        if (_generatorWindow.ShowActivated)
            _generatorWindow.Show();
        //bring to foreground
        if (!_generatorWindow.IsActive)
            _generatorWindow.Activate();

        _generatorWindow.WindowState = WindowState.Normal;
    }

    private void EditorButton_Click(object sender, RoutedEventArgs e)
    {
        //create window
        if (_editorWindow == null || !_editorWindow.IsLoaded)
        {
            _editorWindow = new EditorWindow();
            _editorWindow.SetBinding(DataContextProperty, new Binding(".") { Source = vm.IFSViewModel, Mode = BindingMode.TwoWay });
        }

        if (_editorWindow.ShowActivated)
            _editorWindow.Show();
        //bring to foreground
        if (!_editorWindow.IsActive)
            _editorWindow.Activate();

        _editorWindow.WindowState = WindowState.Normal;
    }
    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow
        {
            DataContext = new SettingsViewModel(vm)
        };
        if (settingsWindow.ShowDialog() == true)
            vm.StatusBarText = "Settings saved.";
    }

    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AboutDialogWindow();
        dialog.ShowDialog();
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        if (_generatorWindow != null)
            _generatorWindow.Close();
        if (_editorWindow != null)
            _editorWindow.Close();

        await vm.DisposeAsync();

        base.OnClosing(e);
    }

    private void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        vm.IFSViewModel.UndoCommand.Execute(null);
    }

    private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = vm?.IFSViewModel.UndoCommand.CanExecute(null) ?? false;
    }

    private void Redo_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        vm.IFSViewModel.RedoCommand.Execute(null);
    }

    private void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = vm?.IFSViewModel.RedoCommand.CanExecute(null) ?? false;
    }

    private void Copy_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        vm.CopyClipboardParamsCommand.Execute(null);
    }

    private void Copy_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = vm?.CopyClipboardParamsCommand.CanExecute(null) ?? false;
    }

    private void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        vm.PasteClipboardParamsCommand.Execute(null);
    }

    private void Paste_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = vm?.PasteClipboardParamsCommand.CanExecute(null) ?? false;
    }

    private void mainWindow_DragOver(object sender, DragEventArgs e)
    {
        e.Handled = true;
        var filepath = IsSingleFile(e);
        e.Effects = filepath is not null && Path.GetExtension(filepath) is ".ifsjson" or ".gradient" or ".ugr" ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void mainWindow_Drop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        var fileName = IsSingleFile(e);
        if (fileName is null) 
            return;
        var ext = Path.GetExtension(fileName);
        if (ext is ".ifsjson")
            vm?.DropParamsCommand.Execute(fileName);
        else if (ext is ".gradient" or ".ugr")
            vm.IFSViewModel.DropPaletteCommand.Execute(fileName);
    }

    private static string IsSingleFile(DragEventArgs args)
    {//from MS samples
        if (args.Data.GetDataPresent(DataFormats.FileDrop, true))
        {
            var fileNames = args.Data.GetData(DataFormats.FileDrop, true) as string[];
            if (fileNames?.Length is 1)
            {
                if (File.Exists(fileNames[0]))
                {
                    return fileNames[0];
                }
            }
        }
        return null;
    }

    private void dockManager_DocumentClosing(object sender, AvalonDock.DocumentClosingEventArgs e)
    {
        vm.workspace.LoadParams(IFS.Default);
    }

    private void LayoutRoot_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        //TODO: fix docking panel resizing
        //SetDisplayResolution
        //if preview -> SetHistogramScaleToDisplay
        vm?.workspace.Renderer.InvalidateDisplay();
    }
}
