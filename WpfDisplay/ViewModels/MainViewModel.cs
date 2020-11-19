using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using IFSEngine.Model;
using IFSEngine.Serialization;
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
    public class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly Workspace workspace;

        public InteractiveDisplayViewModel DisplayViewModel { get; }
        public ToneMappingViewModel ToneMappingViewModel { get; }
        public CameraSettingsViewModel CameraSettingsViewModel { get; }
        public PerformanceViewModel PerformanceViewModel { get; }
        public QualitySettingsViewModel QualitySettingsViewModel { get; }
        public IFSViewModel IFSViewModel { get; }

        public MainViewModel(Workspace workspace)
        {
            this.workspace = workspace;
            workspace.PropertyChanged += (s, e) => RaisePropertyChanged(string.Empty);
            DisplayViewModel = new InteractiveDisplayViewModel(workspace);
            PerformanceViewModel = new PerformanceViewModel(workspace);
            QualitySettingsViewModel = new QualitySettingsViewModel(workspace);
            IFSViewModel = new IFSViewModel(workspace);
            CameraSettingsViewModel = new CameraSettingsViewModel(workspace);
            CameraSettingsViewModel.PropertyChanged += (s, e) => RaisePropertyChanged(e.PropertyName);
            ToneMappingViewModel = new ToneMappingViewModel(workspace);
            ToneMappingViewModel.PropertyChanged += (s, e) => RaisePropertyChanged(e.PropertyName);
        }

        private RelayCommand _newCommand;
        public RelayCommand NewCommand
        {
            get => _newCommand ?? (
                _newCommand = new RelayCommand(() =>
                {
                    workspace.IFS = new IFS();
                }));
        }

        private RelayCommand _loadRandomCommand;
        public RelayCommand LoadRandomCommand
        {
            get => _loadRandomCommand ?? (
                _loadRandomCommand = new RelayCommand(() =>
                {
                    workspace.IFS = IFS.GenerateRandom();
                }));
        }

        private RelayCommand _saveParamsCommand;
        public RelayCommand SaveParamsCommand
        {
            get => _saveParamsCommand ?? (
                _saveParamsCommand = new RelayCommand(() =>
                {
                    if (NativeDialogHelper.ShowFileSelectorDialog(DialogSetting.SaveParams, out string path))
                    {
                        IfsSerializer.SaveJson(workspace.IFS, path);
                    }
                }));
        }

        private RelayCommand _loadParamsCommand;
        public RelayCommand LoadParamsCommand
        {
            get => _loadParamsCommand ?? (
                _loadParamsCommand = new RelayCommand(() =>
                {
                    if (NativeDialogHelper.ShowFileSelectorDialog(DialogSetting.OpenParams, out string path))
                    {
                        IFS ifs;
                        try
                        {
                            ifs = IfsSerializer.LoadJson(path, false);
                        }
                        catch(SerializationException)
                        {
                            if (MessageBox.Show("Loading failed. Try again and ignore transform versions?", "Loading failed", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                                ifs = IfsSerializer.LoadJson(path, true);
                            else 
                                return;
                        }
                        workspace.IFS = ifs;
                    }
                }));
        }

        private RelayCommand _saveImageCommand;
        public RelayCommand SaveImageCommand
        {
            get => _saveImageCommand ?? (
                _saveImageCommand = new RelayCommand(async () =>
                {
                    WriteableBitmap wbm = null;
                    PngBitmapEncoder enc = new PngBitmapEncoder();
                    Task copyTask = Task.Run(async () =>
                    {
                        wbm = new WriteableBitmap(workspace.Renderer.HistogramWidth, workspace.Renderer.HistogramHeight, 96, 96, PixelFormats.Bgra32, null);
                        await workspace.Renderer.CopyPixelDataToBitmap(wbm.BackBuffer);
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
                }));
        }

        private RelayCommand _closeWorkspace;
        public RelayCommand CloseWorkspace
        {
            get => _closeWorkspace ?? (
                _closeWorkspace = new RelayCommand(() =>
                {
                    //TODO: prompt to save work?
                    Dispose();
                }));
        }

        public void Dispose()
        {
            workspace.Renderer.Dispose();
        }
    }
}
