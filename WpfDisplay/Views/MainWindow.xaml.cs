using IFSEngine;
using IFSEngine.Model;
using IFSEngine.Util;
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RendererGL renderer;

        private EditorWindow editorWindow;

        public MainWindow()
        {
            InitializeComponent();
            Host.Loaded += (s, e) =>
            {
                renderer = Host.Renderer;
                this.DataContext = renderer;

            };
            this.Closing += (s2, e2) => renderer.Dispose();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            renderer.StartRendering();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            renderer.LoadParams(new IFS(true));
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            renderer.CurrentParams.SaveJson("tmp.json");
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            renderer.LoadParams(IFS.LoadJson("tmp.json"));
        }

        private void Button_Preview(object sender, RoutedEventArgs e)
        {
            double fitToDisplayRatio = renderer.DisplayWidth / (double)renderer.CurrentParams.ViewSettings.ImageResolution.Width;
            renderer.SetHistogramScale(fitToDisplayRatio);
        }

        private void Button_Final(object sender, RoutedEventArgs e)
        {
            renderer.SetHistogramScale(1.0);
        }

        private void Button_2x(object sender, RoutedEventArgs e)
        {
            renderer.SetHistogramScale(2.0);
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            renderer.CurrentParams.Palette = UFPalette.FromFile(@"Resources\example.gradient")[RandHelper.Next(8)];
            renderer.InvalidateParams();
        }

        private void Button_Click_6(object sender, RoutedEventArgs e)
        {
            renderer.CurrentParams.AddIterator(Iterator.RandomIterator, true);
            renderer.InvalidateParams();
        }

        private void Button_Click_7(object sender, RoutedEventArgs e)
        {
            renderer.CurrentParams.RemoveIterator(renderer.CurrentParams.Iterators.Last());
            renderer.InvalidateParams();
        }

        private async void Button_Click_8(object sender, RoutedEventArgs e)
        {
            var p = await renderer.GenerateImage();
            Bitmap b = new Bitmap(renderer.HistogramWidth, renderer.HistogramHeight);
            await Task.Run(() =>
            {
                for (int y = 0; y < renderer.HistogramHeight; y++)
                    for (int x = 0; x < renderer.HistogramWidth; x++)
                    {
                        b.SetPixel(x, renderer.HistogramHeight - y - 1, System.Drawing.Color.FromArgb((int)(255.0 * p[x, y][3]), (int)(255.0 * p[x, y][0]), (int)(255.0 * p[x, y][1]), (int)(255.0 * p[x, y][2])));
                    }
                b.Save("Output.png", System.Drawing.Imaging.ImageFormat.Png);
            });
        }

        private void EditorButton_Click(object sender, RoutedEventArgs e)
        {
            if(editorWindow!=null)
            {
                editorWindow.DataContext = new IFSViewModel(renderer.CurrentParams);
                return;
            }

            editorWindow = new EditorWindow { DataContext = new IFSViewModel(renderer.CurrentParams) };
            editorWindow.Show();

        }

        protected override void OnClosed(EventArgs e)
        {
            if (editorWindow != null)
                editorWindow.Close();
            base.OnClosed(e);
        }
    }
}
