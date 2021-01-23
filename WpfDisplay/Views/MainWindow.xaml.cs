using IFSEngine;
using IFSEngine.Model;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using WpfDisplay.Models;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private EditorWindow editorWindow;
        private GeneratorWindow generatorWindow;

        public MainWindow()
        {
            InitializeComponent();
            ContentRendered += (s, e) =>
            {
                var loadedTransforms = Directory.GetFiles(@".\Functions\Transforms")
                    .Select(file => TransformFunction.FromFile(file)).ToList();
                //init workspace 
                RendererGL renderer = new RendererGL(renderDisplay.display1.WindowInfo);
                renderer.SetDisplayResolution(renderDisplay.display1.Width, renderDisplay.display1.Height);
                renderDisplay.display1.Resize += (s2, e2) => renderer.SetDisplayResolution(renderDisplay.display1.Width, renderDisplay.display1.Height);
                IFS ifs = IFS.GenerateRandom(loadedTransforms);
                //TODO: create-workspace view?
                Workspace workspace = new Workspace
                {
                    Renderer = renderer,
                    IFS = ifs
                };
                var mainViewModel = new MainViewModel(workspace);
                this.DataContext = mainViewModel;
                //HACK: binding the DataContext in xaml causes OpenTK IWindowInfo null exception
                renderDisplay.DataContext = mainViewModel.DisplayViewModel;
                renderer.Initialize(loadedTransforms);
            };
        }

        private void GeneratorButton_Click(object sender, RoutedEventArgs e)
        {
            //create window
            if (generatorWindow == null || !generatorWindow.IsLoaded)
            {
                generatorWindow = new GeneratorWindow();
                generatorWindow.DataContext = new GeneratorViewModel((DataContext as MainViewModel));
                //generatorWindow.SetBinding(DataContextProperty, new Binding(".") { Source = (DataContext as MainViewModel).IFSViewModel, Mode = BindingMode.TwoWay });
            }

            if (generatorWindow.ShowActivated)
                generatorWindow.Show();
            //bring to foreground
            if (!generatorWindow.IsActive)
                generatorWindow.Activate();
        }

        private void EditorButton_Click(object sender, RoutedEventArgs e)
        {
            //create window
            if (editorWindow == null || !editorWindow.IsLoaded)
            {
                editorWindow = new EditorWindow();
                editorWindow.SetBinding(DataContextProperty, new Binding(".") { Source = (DataContext as MainViewModel).IFSViewModel, Mode=BindingMode.TwoWay});
            }

            if (editorWindow.ShowActivated)
                editorWindow.Show();
            //bring to foreground
            if (!editorWindow.IsActive)
                editorWindow.Activate();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (generatorWindow != null)
                generatorWindow.Close();
            if (editorWindow != null)
                editorWindow.Close();
            (this.DataContext as MainViewModel).Dispose();
            base.OnClosing(e);
        }

    }
}
