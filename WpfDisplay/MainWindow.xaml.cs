using IFSEngine;
using IFSEngine.Model;
using IFSEngine.Util;
using System.Drawing;
using System.Linq;
using System.Windows;
namespace WpfDisplay
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private RendererGL renderer;

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
            renderer.Reset();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            renderer.CurrentParams.SaveJson("tmp.json");
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            renderer.CurrentParams = IFS.LoadJson("tmp.json");
            renderer.ActiveView = renderer.CurrentParams.Views.First();
            //renderer.ActiveView.Camera.OnManipulate += renderer.InvalidateAccumulation;//ez igy bena
            renderer.InvalidateParams();
            //szebb lenne pl.
            //renderer.LoadParams(IFS.LoadJson("tmp.json"), [ActiveView=0]);
        }

        int nextviewcnt = 1;
        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            renderer.ActiveView = renderer.CurrentParams.Views[nextviewcnt++ % renderer.CurrentParams.Views.Count];
            renderer.InvalidateAccumulation();
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

        private void Button_Click_8(object sender, RoutedEventArgs e)
        {
            var p = renderer.GenerateImage();
            Bitmap b = new Bitmap(renderer.Width, renderer.Height);
            for (int y = 0; y < renderer.Height; y++)
                for (int x = 0; x < renderer.Width; x++)
                {
                    b.SetPixel(x, renderer.Height - y - 1, System.Drawing.Color.FromArgb((int)(255.0 * p[x, y][3]), (int)(255.0 * p[x, y][0]), (int)(255.0 * p[x, y][1]), (int)(255.0 * p[x, y][2])));
                }
            b.Save("Output.png", System.Drawing.Imaging.ImageFormat.Png);
        }

    }
}
