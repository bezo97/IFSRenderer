using IFSEngine.Model;
using IFSEngine.Utility;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfDisplay.Helper;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    [ObservableObject]
    public partial class MainViewModel : IDisposable
    {
        internal readonly Workspace workspace;

        public ToneMappingViewModel ToneMappingViewModel { get; }
        public CameraSettingsViewModel CameraSettingsViewModel { get; }
        public PerformanceViewModel PerformanceViewModel { get; }
        public QualitySettingsViewModel QualitySettingsViewModel { get; }
        public IFSViewModel IFSViewModel { get; }

        private bool transparentBackground;
        public bool TransparentBackground
        {
            get => transparentBackground;
            set
            {
                workspace.TakeSnapshot();
                if (value)
                    IFSViewModel.BackgroundColor = Colors.Black;
                SetProperty(ref transparentBackground, value);
                OnPropertyChanged(nameof(IsColorPickerEnabled));
            }
        }

        [ObservableProperty] private string statusBarText;

        public bool IsColorPickerEnabled => !TransparentBackground;
        //Main display settings:
        public bool InvertAxisX => workspace.InvertAxisX;
        public bool InvertAxisY => workspace.InvertAxisY;
        public bool InvertAxisZ => workspace.InvertAxisZ;
        public float Sensitivity => (float)workspace.Sensitivity;


        public string WindowTitle => workspace.IFS is null ? "IFSRenderer" : $"{workspace.IFS.Title} - IFSRenderer";
        public string IFSTitle
        {
            get => workspace.IFS.Title;
            set
            {
                workspace.IFS.Title = value;
                OnPropertyChanged(nameof(IFSTitle));
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
        public IEnumerable<Author> AuthorList => workspace.IFS.Authors;

        public MainViewModel(Workspace workspace)
        {
            this.workspace = workspace;
            workspace.StatusTextChanged += (s, e) => StatusBarText = e;
            workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
            PerformanceViewModel = new PerformanceViewModel(workspace);
            QualitySettingsViewModel = new QualitySettingsViewModel(workspace);
            IFSViewModel = new IFSViewModel(workspace);
            CameraSettingsViewModel = new CameraSettingsViewModel(workspace);
            CameraSettingsViewModel.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);
            ToneMappingViewModel = new ToneMappingViewModel(workspace);
            ToneMappingViewModel.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);
            workspace.UpdateStatusText($"Initialized");
        }

        [ICommand]
        private void New()
        {
            workspace.LoadBlankParams();
            workspace.UpdateStatusText($"Blank parameters loaded");
        }

        [ICommand]
        private void LoadRandom()
        {
            workspace.LoadRandomParams();
            workspace.UpdateStatusText($"Randomly generated parameters loaded");
        }

        [ICommand]
        private async Task SaveParams()
        {
            if (DialogHelper.ShowSaveParamsDialog(workspace.IFS.Title, out string path))
            {
                if (IFSTitle == "Untitled")//Set the file name as title
                    IFSTitle = Path.GetFileNameWithoutExtension(path);

                try
                {
                    await workspace.SaveParamsFileAsync(path);
                    workspace.UpdateStatusText($"Parameters saved to {path}");
                }
                catch (Exception)
                {
                    workspace.UpdateStatusText($"ERROR - Failed to save params.");
                }
            }
        }

        [ICommand]
        private async Task LoadParams()
        {
            if (DialogHelper.ShowOpenParamsDialog(out string path))
            {
                try
                {
                    await workspace.LoadParamsFileAsync(path);
                    workspace.UpdateStatusText($"Parameters loaded from {path}");
                }
                catch (SerializationException ex)
                {
                    string logFilePath = App.LogException(ex);
                    workspace.UpdateStatusText($"ERROR - Failed to load params: {path}. See log: {logFilePath}");
                }
            }
        }

        /// <summary>
        /// Converts the raw pixel data into a BitmapSource with WPF specific calls.
        /// The Alpha channel is optionally removed and the image is flipped vertically, as required by CopyPixelDataToBitmap()
        /// </summary>
        /// <returns></returns>
        private async Task<BitmapSource> GetExportBitmapSource()
        {
            BitmapSource bs;
            WriteableBitmap wbm = new WriteableBitmap(workspace.Renderer.HistogramWidth, workspace.Renderer.HistogramHeight, 96, 96, PixelFormats.Bgra32, null);
            await workspace.Renderer.CopyPixelDataToBitmap(wbm.BackBuffer);
            wbm.Freeze();
            bs = wbm;
            if (!TransparentBackground)
            {//option to remove alpha channel
                var fcb = new FormatConvertedBitmap(wbm, PixelFormats.Bgr32, null, 0);
                fcb.Freeze();
                bs = fcb;
            }
            //flip vertically
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {//This transformation must happen on ui thread
                var tb = new TransformedBitmap();
                tb.BeginInit();
                tb.Source = bs;
                tb.Transform = new ScaleTransform(1, -1, 0, 0);
                tb.EndInit();
                tb.Freeze();
                bs = tb;
            });
            return bs;
        }

        [ICommand]
        private async Task ExportToClipboard()
        {
            BitmapSource bs = await GetExportBitmapSource();
            Clipboard.SetImage(bs);
            //TODO: somehow alpha channel is not copied

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
            workspace.UpdateStatusText($"Image exported to clipboard");
        }

        [ICommand]
        private async Task SaveImage()
        {
            workspace.UpdateStatusText($"Exporting...");
            var makeBitmapTask = GetExportBitmapSource();

            if (DialogHelper.ShowExportImageDialog(workspace.IFS.Title, out string path))
            {
                BitmapSource bs = await makeBitmapTask;

                PngBitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bs));
                using (FileStream stream = new FileStream(path, FileMode.Create))
                    enc.Save(stream);

                //open the image for viewing. TODO: optional..
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                workspace.UpdateStatusText($"Image exported to {path}");
            }

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }

        [ICommand]
        private async Task SaveExr()
        {
            workspace.UpdateStatusText($"Exporting...");
            Task<float[,,]> getDataTask = Task.Run(async () =>
            {
                return await workspace.Renderer.ReadHistogramData();
            });

            if (DialogHelper.ShowExportExrDialog(workspace.IFS.Title, out string path))
            {
                var histogramData = await getDataTask;
                using var fstream = File.Create(path);
                OpenEXR.WriteStream(fstream, histogramData);
                workspace.UpdateStatusText($"Image exported to {path}");
            }

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }

        [ICommand]
        private void TakeSnapshot() => workspace.TakeSnapshot();

        [ICommand]
        private void InteractionFinished() => CameraSettingsViewModel.RaisePropertyChanged();

        [ICommand]
        private async Task CloseWorkspace()
        {
            //TODO: prompt to save work?
            Dispose();
            Environment.Exit(0);
        }

        public void Dispose()
        {
            workspace.Renderer.Dispose();
        }

        [ICommand]
        private void VisitIssues()
        {
            //Open the Issues page in user's default browser
            string link = "https://github.com/bezo97/IFSRenderer/issues/";
            Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
        }

        [ICommand]
        private void VisitForum()
        {
            //Open the Discussions page in user's default browser
            string link = "https://github.com/bezo97/IFSRenderer/discussions";
            Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
        }

        [ICommand]
        private void ReportBug()
        {
            //Open the bug report template in user's default browser
            string link = "https://github.com/bezo97/IFSRenderer/issues/new?assignees=&labels=&template=bug_report.md";
            Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
        }

        [ICommand]
        private void CheckUpdates()
        {
            //Open the Releases page in user's default browser
            string link = "https://github.com/bezo97/IFSRenderer/releases";
            Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
        }

        [ICommand]
        private void VisitWiki()
        {
            //Open the Wiki page in user's default browser
            string link = "https://github.com/bezo97/IFSRenderer/wiki";
            Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
        }
    }
}
