using IFSEngine.Model;
using IFSEngine.Serialization;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using IFSEngine.Utility;
using System;
using System.IO;
using System.Runtime;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfDisplay.Helper;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    public class MainViewModel : ObservableObject, IDisposable
    {
        private readonly Workspace workspace;

        public InteractiveDisplayViewModel DisplayViewModel { get; }
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
                SetProperty(ref transparentBackground, value);
                if (value)
                    IFSViewModel.BackgroundColor = Colors.Black;
                OnPropertyChanged(nameof(IsColorPickerEnabled));
            }
        }

        public bool IsColorPickerEnabled => !TransparentBackground;

        public MainViewModel(Workspace workspace)
        {
            this.workspace = workspace;
            workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
            DisplayViewModel = new InteractiveDisplayViewModel(workspace);
            PerformanceViewModel = new PerformanceViewModel(workspace);
            QualitySettingsViewModel = new QualitySettingsViewModel(workspace);
            IFSViewModel = new IFSViewModel(workspace);
            CameraSettingsViewModel = new CameraSettingsViewModel(workspace);
            CameraSettingsViewModel.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);
            ToneMappingViewModel = new ToneMappingViewModel(workspace);
            ToneMappingViewModel.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);
        }

        public void LoadParamsToWorkspace(IFS ifs)
        {
            workspace.IFS = ifs;
        }

        private AsyncRelayCommand _newCommand;
        public AsyncRelayCommand NewCommand =>
            _newCommand ??= new AsyncRelayCommand(OnNewCommand);
        private async Task OnNewCommand()
        {
            LoadParamsToWorkspace(new IFS());
        }

        private AsyncRelayCommand _loadRandomCommand;
        public AsyncRelayCommand LoadRandomCommand =>
            _loadRandomCommand ??= new AsyncRelayCommand(OnLoadRandomCommand);
        private async Task OnLoadRandomCommand()
        {
            LoadParamsToWorkspace(IFS.GenerateRandom(workspace.Renderer.RegisteredTransforms));
        }

        private AsyncRelayCommand _saveParamsCommand;
        public AsyncRelayCommand SaveParamsCommand =>
            _saveParamsCommand ??= new AsyncRelayCommand(OnSaveParamsCommand);
        private async Task OnSaveParamsCommand()
        {
            if (NativeDialogHelper.ShowFileSelectorDialog(DialogSetting.SaveParams, out string path))
            {
                IfsSerializer.SaveJson(workspace.IFS, path);
            }
        }

        private AsyncRelayCommand _loadParamsCommand;
        public AsyncRelayCommand LoadParamsCommand =>
            _loadParamsCommand ??= new AsyncRelayCommand(OnLoadParamsCommand);
        private async Task OnLoadParamsCommand()
        {
            if (NativeDialogHelper.ShowFileSelectorDialog(DialogSetting.OpenParams, out string path))
            {
                var transforms = workspace.Renderer.RegisteredTransforms;
                IFS ifs;
                try
                {
                    ifs = IfsSerializer.LoadJson(path, transforms, false);
                }
                catch (SerializationException)
                {
                    if (MessageBox.Show("Loading failed. Try again and ignore transform versions?", "Loading failed", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        ifs = IfsSerializer.LoadJson(path, transforms, true);
                    else
                        return;
                }
                LoadParamsToWorkspace(ifs);
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

        private AsyncRelayCommand _exportToClipboardCommand;
        public AsyncRelayCommand ExportToClipboardCommand =>
            _exportToClipboardCommand ??= new AsyncRelayCommand(OnExportToClipboardCommand);
        private async Task OnExportToClipboardCommand()
        {
            BitmapSource bs = await GetExportBitmapSource();
            Clipboard.SetImage(bs);
            //TODO: somehow alpha channel is not copied

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }

        private AsyncRelayCommand _saveImageCommand;
        public AsyncRelayCommand SaveImageCommand =>
            _saveImageCommand ??= new AsyncRelayCommand(OnSaveImageCommand);
        private async Task OnSaveImageCommand()
        {
            var makeBitmapTask = GetExportBitmapSource();

            if (NativeDialogHelper.ShowFileSelectorDialog(DialogSetting.SaveImage, out string path))
            {
                BitmapSource bs = await makeBitmapTask;

                PngBitmapEncoder enc = new PngBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bs));
                using (FileStream stream = new FileStream(path, FileMode.Create))
                    enc.Save(stream);

                //open the image for viewing. TODO: optional..
                System.Diagnostics.Process.Start(path);
            }

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }

        private AsyncRelayCommand _saveExrCommand;
        public AsyncRelayCommand SaveExrCommand =>
            _saveExrCommand ??= new AsyncRelayCommand(OnSaveExrCommand);
        private async Task OnSaveExrCommand()
        {
            Task<float[,,]> getDataTask = Task.Run(async () =>
            {
                return await workspace.Renderer.ReadHistogramData();
            });

            if (NativeDialogHelper.ShowFileSelectorDialog(DialogSetting.SaveExr, out string path))
            {
                var histogramData = await getDataTask;
                using (var fstream = File.Create(path))
                    OpenEXR.WriteStream(fstream, histogramData);
            }

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }

        private AsyncRelayCommand _closeWorkspaceCommand;
        public AsyncRelayCommand CloseWorkspaceCommand =>
            _closeWorkspaceCommand ??= new AsyncRelayCommand(OnCloseWorkspaceCommand);
        private async Task OnCloseWorkspaceCommand()
        {
            //TODO: prompt to save work?
            Dispose();
            Environment.Exit(0);
        }

        public void Dispose()
        {
            workspace.Renderer.Dispose();
        }
    }
}
