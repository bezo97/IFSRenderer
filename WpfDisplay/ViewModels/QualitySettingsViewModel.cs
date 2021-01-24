using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using System.Windows;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    public class QualitySettingsViewModel : ObservableObject
    {
        private readonly Workspace workspace;

        public QualitySettingsViewModel(Workspace workspace)
        {
            this.workspace = workspace;
            workspace.PropertyChanged += (s, e) => RaisePropertyChanged(string.Empty);
        }

        private RelayCommand _startRenderingCommand;
        public RelayCommand StartRenderingCommand
        {
            get => _startRenderingCommand ?? (
                _startRenderingCommand = new RelayCommand(() =>
                {
                    workspace.Renderer.StartRenderLoop();
                }));
        }

        public bool EnableDE
        {
            get => workspace.Renderer.EnableDE;
            set
            {
                workspace.Renderer.EnableDE = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DEPanelVisibility));
                workspace.Renderer.UpdateDisplay();
            }
        }
        public Visibility DEPanelVisibility => EnableDE ? Visibility.Visible : Visibility.Collapsed;

        public int DEMaxRadius
        {
            get => workspace.Renderer.DEMaxRadius;
            set
            {
                workspace.Renderer.DEMaxRadius = value;
                RaisePropertyChanged();
                workspace.Renderer.UpdateDisplay();
            }
        }

        public double DEThreshold
        {
            get => workspace.Renderer.DEThreshold;
            set
            {
                workspace.Renderer.DEThreshold = value;
                RaisePropertyChanged();
                workspace.Renderer.UpdateDisplay();
            }
        }

        public double DEPower
        {
            get => workspace.Renderer.DEPower;
            set
            {
                workspace.Renderer.DEPower = value;
                RaisePropertyChanged();
                workspace.Renderer.UpdateDisplay();
            }
        }

        public bool EnableTAA
        {
            get => workspace.Renderer.EnableTAA;
            set
            {
                workspace.Renderer.EnableTAA = value;
                RaisePropertyChanged();
                workspace.Renderer.UpdateDisplay();
            }
        }

        public bool EnablePerceptualUpdates
        {
            get => workspace.Renderer.EnablePerceptualUpdates;
            set
            {
                workspace.Renderer.EnablePerceptualUpdates = value;
                RaisePropertyChanged();
                workspace.Renderer.UpdateDisplay();
            }
        }

        public int EntropyInv
        {
            get => (int)(1.0 / workspace.Renderer.Entropy);
            set
            {
                workspace.Renderer.Entropy = 1.0 / value;
                RaisePropertyChanged();
                workspace.Renderer.InvalidateAccumulation();
            }
        }

        public int Warmup
        {
            get => workspace.Renderer.Warmup;
            set
            {
                workspace.Renderer.Warmup = value;
                RaisePropertyChanged();
                workspace.Renderer.InvalidateAccumulation();
            }
        }

        public int MaxFilterRadius
        {
            get => workspace.Renderer.MaxFilterRadius;
            set
            {
                workspace.Renderer.MaxFilterRadius = value;
                RaisePropertyChanged();
                RaisePropertyChanged(()=>FilterText);
                workspace.Renderer.InvalidateAccumulation();
            }
        }

        public string FilterText => "Max Filter Radius" + (MaxFilterRadius > 1 ? "" : " (Off)");

        private bool isResolutionLinked;
        public bool IsResolutionLinked
        {
            get { return isResolutionLinked; }
            set { Set(ref isResolutionLinked, value); }
        }


        public int ImageWidth 
        { 
            get 
            {
                return workspace.IFS.ImageResolution.Width;
            } 
            set 
            {
                if(IsResolutionLinked)
                {
                    double ratio = workspace.IFS.ImageResolution.Width / (double) workspace.IFS.ImageResolution.Height;
                    workspace.IFS.ImageResolution = new System.Drawing.Size(value, (int)(value / ratio));
                }
                else
                {
                    workspace.IFS.ImageResolution = new System.Drawing.Size(value, workspace.IFS.ImageResolution.Height);
                }
                RaisePropertyChanged(() => ImageWidth);
                RaisePropertyChanged(() => ImageHeight);
                workspace.Renderer.SetHistogramScale(1.0);
            }
        }

        public int ImageHeight
        {
            get
            {
                return workspace.IFS.ImageResolution.Height;
            }
            set
            {
                if (IsResolutionLinked)
                {
                    double ratio = workspace.IFS.ImageResolution.Width / (double) workspace.IFS.ImageResolution.Height;
                    workspace.IFS.ImageResolution = new System.Drawing.Size((int)(value * ratio), value);
                }
                else
                {
                    workspace.IFS.ImageResolution = new System.Drawing.Size(workspace.IFS.ImageResolution.Width, value);
                }
                RaisePropertyChanged(() => ImageWidth);
                RaisePropertyChanged(() => ImageHeight);
                workspace.Renderer.SetHistogramScale(1.0);
            }
        }

        private RelayCommand _previewPresetCommand;
        public RelayCommand PreviewPresetCommand
        {
            get => _previewPresetCommand ?? (
                _previewPresetCommand = new RelayCommand(async () =>
                {
                    workspace.Renderer.SetHistogramScaleToDisplay();

                    //EnableDE = true;
                    //EnableTAA = true;
                    //EnablePerceptualUpdates = false;
                    //PassIters = 100;
                    //Warmup = 10;
                    EntropyInv = 100;
                    MaxFilterRadius = 1;
                }));
        }

        private RelayCommand _finalPresetCommand;
        public RelayCommand FinalPresetCommand
        {
            get => _finalPresetCommand ?? (
                _finalPresetCommand = new RelayCommand(async () =>
                {
                    EnablePerceptualUpdates = true;
                    EnableTAA = false;
                    EnableDE = false;
                    //PassIters = 500;
                    //Warmup = 30;
                    EntropyInv = 10000;
                    MaxFilterRadius = 3;
                    workspace.Renderer.SetHistogramScale(1.0);
                }));
        }


    }
}
