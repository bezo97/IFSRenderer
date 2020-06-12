using IFSEngine;
using IFSEngine.Model;
using IFSEngine.Util;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
                performanceView.DataContext = new PerformanceViewModel(renderer);
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

        private void Button_Click_5(object sender, RoutedEventArgs e)
        {
            if (NativeDialogHelper.ShowFileSelectorDialog(DialogSetting.OpenPalette, out string path))
            {
                renderer.CurrentParams.Palette = UFPalette.FromFile(path)[0];//TODO: gradient picker
                renderer.InvalidateParams();
            }
        }

        private async Task SaveImageWithGDI()
        {
            Bitmap b = null;
            Task genTask = Task.Run(async () => {
                b = new Bitmap(renderer.HistogramWidth, renderer.HistogramHeight);

                var bits = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                await renderer.CopyPixelDataToBitmap(bits.Scan0);
                b.UnlockBits(bits);

                b.RotateFlip(RotateFlipType.RotateNoneFlipY);
                //TODO: option to remove alpha channel

            });

            if (NativeDialogHelper.ShowFileSelectorDialog(DialogSetting.SaveImage, out string path))
            {
                await genTask;
                b.Save(path, ImageFormat.Png);
            }
            b.Dispose();

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }

        private async void Button_Click_8(object sender, RoutedEventArgs e)
        {
            WriteableBitmap wbm = null;
            PngBitmapEncoder enc = new PngBitmapEncoder();
            Task copyTask = Task.Run(async () =>
            {
                wbm = new WriteableBitmap(renderer.HistogramWidth, renderer.HistogramHeight, 96, 96, PixelFormats.Bgra32, null);
                await renderer.CopyPixelDataToBitmap(wbm.BackBuffer);
                //TODO: option to remove alpha channel
                //TODO: flip y
                wbm.Freeze();
            });

            if (NativeDialogHelper.ShowFileSelectorDialog(DialogSetting.SaveImage, out string path))
            {
                await copyTask;
                enc.Frames.Add(BitmapFrame.Create(wbm));
                using (FileStream stream = new FileStream(path, FileMode.Create))
                    enc.Save(stream);
            }

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();

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

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            renderer.LoadParams(new IFS());
        }

        private async void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            double fitToDisplayRatio = renderer.DisplayWidth / (double)renderer.CurrentParams.ViewSettings.ImageResolution.Width;
            await renderer.SetHistogramScale(fitToDisplayRatio);
            renderer.EnableDE = true;
            renderer.EnableTAA = true;
            //renderer.EnablePerceptualUpdates = false;
        }

        private async void FinalButton_Click(object sender, RoutedEventArgs e)
        {
            renderer.EnablePerceptualUpdates = true;
            renderer.EnableTAA = false;
            renderer.EnableDE = false;
            await renderer.SetHistogramScale(1.0);
        }
    }
}
