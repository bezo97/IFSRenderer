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

        public MainWindow()
        {
            InitializeComponent();

            //renderDisplay.display1.Ready += () =>
            Loaded+=(sss,eee)=>
            {
                var loadedTransforms = Directory.GetFiles(@".\Functions\Transforms").Select(file => TransformFunction.FromString(File.ReadAllText(file))).ToList();
                //init workspace 
                RendererGL renderer = new RendererGL(renderDisplay.WindowInfo, renderDisplay.GraphicsContext);
                renderer.SetDisplayResolution((int)renderDisplay.display1.RenderSize.Width, (int)renderDisplay.display1.RenderSize.Height);
                renderDisplay.display1.SizeChanged += (s2, e2) => renderer.SetDisplayResolution((int)renderDisplay.display1.RenderSize.Width, (int)renderDisplay.display1.RenderSize.Height);
                IFS ifs = IFS.GenerateRandom();
                //TODO: create-workspace view?
                Workspace workspace = new Workspace
                {
                    Renderer = renderer,
                    IFS = ifs
                };
                var mainViewModel = new MainViewModel(workspace);
                this.DataContext = mainViewModel;

                renderDisplay.display1.Visibility = Visibility.Visible;
                renderDisplay.display1.Render += (ss) =>
                {
                    renderer.RenderFrame2();
                };
                renderer.DisplayFrameCompleted += (s, e) =>
                    Dispatcher.Invoke(() => {
                        renderDisplay.display1.InvalidateVisual(); });
                //HACK: binding the DataContext in xaml causes OpenTK IWindowInfo null exception
                renderDisplay.DataContext = mainViewModel.DisplayViewModel;
                renderer.Initialize(loadedTransforms);
            };
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
            if (editorWindow != null)
                editorWindow.Close();
            (this.DataContext as MainViewModel).Dispose();
            base.OnClosing(e);
        }

    }
}
