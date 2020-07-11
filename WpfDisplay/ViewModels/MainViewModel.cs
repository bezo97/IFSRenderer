using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using IFSEngine;
using IFSEngine.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfDisplay.Helper;

namespace WpfDisplay.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly RendererGL renderer;

        //public DisplayViewModel DisplayViewModel { get; }
        public PerformanceViewModel PerformanceViewModel { get; }
        public QualitySettingsViewModel QualitySettingsViewModel { get; }


        private IFSViewModel _ifsViewModel;
        public IFSViewModel IFSViewModel {
            get => _ifsViewModel; 
            set {
                Set(ref _ifsViewModel, value);
                _ifsViewModel.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == "InvalidateParams")
                        renderer.InvalidateParams();
                };
            }
        }

        public MainViewModel(RendererGL renderer)
        {
            this.renderer = renderer;
            PerformanceViewModel = new PerformanceViewModel(renderer);
            QualitySettingsViewModel = new QualitySettingsViewModel(renderer);
            LoadRandomCommand.Execute(null);
        }

        private RelayCommand _newCommand;
        public RelayCommand NewCommand
        {
            get => _newCommand ?? (
                _newCommand = new RelayCommand(() =>
                {
                    var ifs = new IFS();
                    IFSViewModel = new IFSViewModel(ifs);
                    renderer.LoadParams(ifs);
                }));
        }

        private RelayCommand _loadRandomCommand;
        public RelayCommand LoadRandomCommand
        {
            get => _loadRandomCommand ?? (
                _loadRandomCommand = new RelayCommand(() =>
                {
                    var ifs = IFS.GenerateRandom();
                    IFSViewModel = new IFSViewModel(ifs);
                    renderer.LoadParams(ifs);
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
                        IFSViewModel.ifs.SaveJson(path);
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
                        var ifs = IFS.LoadJson(path);
                        IFSViewModel = new IFSViewModel(ifs);
                        renderer.LoadParams(ifs);
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
                }));
        }

    }
}
