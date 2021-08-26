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
using System.Diagnostics;
using System.Windows.Input;
using IFSEngine.Generation;

namespace WpfDisplay.ViewModels
{
    public class MainViewModel : ObservableObject, IDisposable
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

        private string statusBarText;
        public string StatusBarText { get => statusBarText; set => SetProperty(ref statusBarText, value); }

        public bool IsColorPickerEnabled => !TransparentBackground;

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

        public void LoadParamsToWorkspace(IFS ifs)
        {
            workspace.TakeSnapshot();
            workspace.IFS = ifs;
            if(!workspace.Renderer.IsRendering)
                workspace.Renderer.StartRenderLoop();
        }

        private ICommand _newCommand;
        public ICommand NewCommand =>
            _newCommand ??= new RelayCommand(OnNewCommand);
        private void OnNewCommand()
        {
            LoadParamsToWorkspace(new IFS());
            workspace.UpdateStatusText($"Blank parameters loaded");
        }

        private ICommand _loadRandomCommand;
        public ICommand LoadRandomCommand =>
            _loadRandomCommand ??= new RelayCommand(OnLoadRandomCommand);
        private void OnLoadRandomCommand()
        {
            Generator g = new Generator(workspace.Renderer.RegisteredTransforms);//
            IFS r = g.GenerateOne(new GeneratorOptions());
            r.ImageResolution = new System.Drawing.Size(1920, 1080);
            LoadParamsToWorkspace(r);
            workspace.UpdateStatusText($"Randomly generated parameters loaded");
        }

        private AsyncRelayCommand _saveParamsCommand;
        public AsyncRelayCommand SaveParamsCommand =>
            _saveParamsCommand ??= new AsyncRelayCommand(OnSaveParamsCommand);
        private async Task OnSaveParamsCommand()
        {
            if (DialogHelper.ShowSaveParamsDialog(out string path))
            {
                IfsSerializer.SaveJsonFile(workspace.IFS, path);
                workspace.UpdateStatusText($"Parameters saved to {path}");
            }
        }

        private AsyncRelayCommand _loadParamsCommand;
        public AsyncRelayCommand LoadParamsCommand =>
            _loadParamsCommand ??= new AsyncRelayCommand(OnLoadParamsCommand);
        private async Task OnLoadParamsCommand()
        {
            if (DialogHelper.ShowOpenParamsDialog(out string path))
            {
                var transforms = workspace.Renderer.RegisteredTransforms;
                IFS ifs;
                try
                {
                    ifs = IfsSerializer.LoadJsonFile(path, transforms, false);
                }
                catch (SerializationException)
                {
                    if (MessageBox.Show("Loading params failed. Try again and ignore transform versions?", "Loading failed", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                    {
                        try
                        {
                            ifs = IfsSerializer.LoadJsonFile(path, transforms, true);
                        }
                        catch (SerializationException)
                        {
                            workspace.UpdateStatusText($"ERROR - Unable to load params: {path}");
                            return;
                        }
                    }
                    else
                        return;
                }
                LoadParamsToWorkspace(ifs);
                workspace.UpdateStatusText($"Parameters loaded from {path}");
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
            workspace.UpdateStatusText($"Image exported to clipboard");
        }

        private AsyncRelayCommand _saveImageCommand;
        public AsyncRelayCommand SaveImageCommand =>
            _saveImageCommand ??= new AsyncRelayCommand(OnSaveImageCommand);
        private async Task OnSaveImageCommand()
        {
            workspace.UpdateStatusText($"Exporting...");
            var makeBitmapTask = GetExportBitmapSource();

            if (DialogHelper.ShowExportImageDialog(out string path))
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

        private AsyncRelayCommand _saveExrCommand;
        public AsyncRelayCommand SaveExrCommand =>
            _saveExrCommand ??= new AsyncRelayCommand(OnSaveExrCommand);
        private async Task OnSaveExrCommand()
        {
            workspace.UpdateStatusText($"Exporting...");
            Task<float[,,]> getDataTask = Task.Run(async () =>
            {
                return await workspace.Renderer.ReadHistogramData();
            });

            if (DialogHelper.ShowExportExrDialog(out string path))
            {
                var histogramData = await getDataTask;
                using var fstream = File.Create(path);
                OpenEXR.WriteStream(fstream, histogramData);
                workspace.UpdateStatusText($"Image exported to {path}");
            }

            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect();
        }

        private AsyncRelayCommand _closeWorkspaceCommand;
        public AsyncRelayCommand CloseWorkspaceCommand =>
            _closeWorkspaceCommand ??= new AsyncRelayCommand(OnCloseWorkspaceCommand);

        private RelayCommand _takeSnapshotCommand;
        public RelayCommand TakeSnapshotCommand =>
            _takeSnapshotCommand ??= new RelayCommand(workspace.TakeSnapshot);

        private RelayCommand _interactionFinishedCommand;
        public ICommand InteractionFinishedCommand => _interactionFinishedCommand ??= new RelayCommand(CameraSettingsViewModel.RaisePropertyChanged);

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

        private RelayCommand visitIssuesCommand;
        public ICommand VisitIssuesCommand => visitIssuesCommand ??= new RelayCommand(VisitIssues);
        private void VisitIssues()
        {
            //Open the issue template in user's default browser
            string link = "https://github.com/bezo97/IFSRenderer/issues/new?assignees=&labels=&template=bug_report.md";
            Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
        }

        private RelayCommand visitForumCommand;
        public ICommand VisitForumCommand => visitForumCommand ??= new RelayCommand(VisitForum);
        private void VisitForum()
        {
            //Open the Discussions panel in user's default browser
            string link = "https://github.com/bezo97/IFSRenderer/discussions";
            Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
        }

    }
}
