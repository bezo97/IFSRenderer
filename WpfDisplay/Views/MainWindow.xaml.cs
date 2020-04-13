using IFSEngine;
using IFSEngine.Model;
using IFSEngine.Util;
using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using WpfDisplay.Helper;
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
                performanceView.DataContext = new PerformanceViewModel(renderer);

            };
            this.Closing += (s2, e2) => renderer.Dispose();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            renderer.StartRendering();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            renderer.LoadParams(IFS.GenerateRandom());
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (NativeDialogHelper.ShowFileSelectorDialog(DialogSetting.SaveParams, out string path))
                renderer.CurrentParams.SaveJson(path);
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (NativeDialogHelper.ShowFileSelectorDialog(DialogSetting.OpenParams, out string path))
                renderer.LoadParams(IFS.LoadJson(path));
        }

        private void Button_2x(object sender, RoutedEventArgs e)
        {
            renderer.SetHistogramScale(2.0);
        }

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            if (NativeDialogHelper.ShowFileSelectorDialog(DialogSetting.OpenPalette, out string path))
            {
                renderer.CurrentParams.Palette = UFPalette.FromFile(path)[RandHelper.Next(8)];//TODO: gradient picker
                renderer.InvalidateParams();
            }
        }

        private async void Button_Click_8(object sender, RoutedEventArgs e)
        {
            Bitmap b = null;

            Task genTask = Task.Run(async () => {
                b = new Bitmap(renderer.HistogramWidth, renderer.HistogramHeight);
                var p = await renderer.GenerateImage();
                await Task.Run(() =>
                {
                    for (int y = 0; y < renderer.HistogramHeight; y++)
                        for (int x = 0; x < renderer.HistogramWidth; x++)
                        {
                            b.SetPixel(x, renderer.HistogramHeight - y - 1, System.Drawing.Color.FromArgb((int)(255.0 * p[x, y][3]), (int)(255.0 * p[x, y][0]), (int)(255.0 * p[x, y][1]), (int)(255.0 * p[x, y][2])));
                        }
                });
            });

            if (NativeDialogHelper.ShowFileSelectorDialog(DialogSetting.SaveImage, out string path))
            {
                await genTask;
                b.Save(path, System.Drawing.Imaging.ImageFormat.Png);
            }
            b.Dispose();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (ExplorationCheckBox.IsChecked ?? false)
            {
                renderer.EnablePerceptualUpdates = false;
                renderer.EnableTAA = true;
            }
            else
            {
                renderer.EnablePerceptualUpdates = true;
                renderer.EnableTAA = false;
            }
        }

        private void PreviewResolutionCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (PreviewResolutionCheckBox.IsChecked ?? false)
            {
                double fitToDisplayRatio = renderer.DisplayWidth / (double)renderer.CurrentParams.ViewSettings.ImageResolution.Width;
                renderer.SetHistogramScale(fitToDisplayRatio);
            }
            else
                renderer.SetHistogramScale(1.0);
        }

        private void EditorButton_Click(object sender, RoutedEventArgs e)
        {
            var ifsvm = new IFSViewModel(renderer.CurrentParams);
            ifsvm.PropertyChanged += (s, e2) =>
            {
                if (e2.PropertyName == "InvalidateParams")
                    renderer.InvalidateParams();
            };

            if (editorWindow==null || !editorWindow.IsLoaded)
                editorWindow = new EditorWindow();

            if (editorWindow.ShowActivated)
                editorWindow.Show();

            if(!editorWindow.IsActive)
                editorWindow.Activate();

            editorWindow.DataContext = ifsvm;

        }

        protected override void OnClosed(EventArgs e)
        {
            if (editorWindow != null)
                editorWindow.Close();
            base.OnClosed(e);
        }
    }
}
