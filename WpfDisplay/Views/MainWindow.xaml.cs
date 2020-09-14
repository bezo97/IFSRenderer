using IFSEngine;
using IFSEngine.Model;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private EditorWindow editorWindow;
        private RendererGL renderer;

        public MainWindow()
        {
            InitializeComponent();
            OpenTK.Toolkit.Init();
            var loadedTransforms = Directory.GetFiles(@".\Functions\Transforms").Select(file => TransformFunction.FromString(File.ReadAllText(file))).ToList();
            renderDisplay.Loaded += (s, e) =>
            {
                renderer = new RendererGL(renderDisplay.display1.WindowInfo);
                renderer.Initialize(loadedTransforms);
                renderer.SetDisplayResolution(renderDisplay.display1.Width, renderDisplay.display1.Height);
                renderDisplay.display1.Resize += (s2, e2) => renderer.SetDisplayResolution(renderDisplay.display1.Width, renderDisplay.display1.Height);
                var mainViewModel = new MainViewModel(renderer);
                this.DataContext = mainViewModel;
            };
        }

        private void EditorButton_Click(object sender, RoutedEventArgs e)
        {
            //create window
            if (editorWindow == null || !editorWindow.IsLoaded)
            {
                editorWindow = new EditorWindow();
                editorWindow.SetBinding(DataContextProperty, new Binding("IFSViewModel") { Source = DataContext, Mode=BindingMode.TwoWay});
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
            renderer.Dispose();
            base.OnClosing(e);
        }

    }
}
