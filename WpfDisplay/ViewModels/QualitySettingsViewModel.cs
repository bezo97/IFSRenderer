using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Threading.Tasks;
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
            workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
        }

        public bool EnableDE
        {
            get => workspace.Renderer.EnableDE;
            set
            {
                workspace.Renderer.EnableDE = value;
                OnPropertyChanged(nameof(EnableDE));
                OnPropertyChanged(nameof(DEPanelVisibility));
                workspace.Renderer.InvalidateDisplay();
            }
        }
        public Visibility DEPanelVisibility => EnableDE ? Visibility.Visible : Visibility.Collapsed;

        public int DEMaxRadius
        {
            get => workspace.Renderer.DEMaxRadius;
            set
            {
                workspace.Renderer.DEMaxRadius = value;
                OnPropertyChanged(nameof(DEMaxRadius));
                workspace.Renderer.InvalidateDisplay();
            }
        }

        public double DEThreshold
        {
            get => workspace.Renderer.DEThreshold;
            set
            {
                workspace.Renderer.DEThreshold = value;
                OnPropertyChanged(nameof(DEThreshold));
                workspace.Renderer.InvalidateDisplay();
            }
        }

        public double DEPower
        {
            get => workspace.Renderer.DEPower;
            set
            {
                workspace.Renderer.DEPower = value;
                OnPropertyChanged(nameof(DEPower));
                workspace.Renderer.InvalidateDisplay();
            }
        }

        public bool EnableTAA
        {
            get => workspace.Renderer.EnableTAA;
            set
            {
                workspace.Renderer.EnableTAA = value;
                OnPropertyChanged(nameof(EnableTAA));
                workspace.Renderer.InvalidateDisplay();
            }
        }

        public int EntropyInv
        {
            get => (int)(1.0 / workspace.Renderer.Entropy);
            set
            {
                workspace.Renderer.Entropy = 1.0 / value;
                OnPropertyChanged(nameof(EntropyInv));
                workspace.Renderer.InvalidateHistogramBuffer();
            }
        }

        public int Warmup
        {
            get => workspace.Renderer.Warmup;
            set
            {
                workspace.Renderer.Warmup = value;
                OnPropertyChanged(nameof(Warmup));
                workspace.Renderer.InvalidateHistogramBuffer();
            }
        }

        public int MaxFilterRadius
        {
            get => workspace.Renderer.MaxFilterRadius;
            set
            {
                workspace.Renderer.MaxFilterRadius = value;
                OnPropertyChanged(nameof(MaxFilterRadius));
                OnPropertyChanged(nameof(FilterText));
                workspace.Renderer.InvalidateHistogramBuffer();
            }
        }

        public string FilterText => "Max Filter Radius" + (MaxFilterRadius > 0 ? "" : " (Off)");

        private bool isResolutionLinked;
        public bool IsResolutionLinked
        {
            get { return isResolutionLinked; }
            set { SetProperty(ref isResolutionLinked, value); }
        }


        public int ImageWidth 
        { 
            get 
            {
                return workspace.IFS.ImageResolution.Width;
            } 
            set 
            {
                workspace.TakeSnapshot();
                if(IsResolutionLinked)
                {
                    double ratio = workspace.IFS.ImageResolution.Width / (double) workspace.IFS.ImageResolution.Height;
                    workspace.IFS.ImageResolution = new System.Drawing.Size(value, (int)(value / ratio));
                }
                else
                {
                    workspace.IFS.ImageResolution = new System.Drawing.Size(value, workspace.IFS.ImageResolution.Height);
                }
                workspace.Renderer.SetHistogramScale(1.0);
                OnPropertyChanged(nameof(ImageWidth));
                OnPropertyChanged(nameof(ImageHeight));
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
                workspace.TakeSnapshot();
                if (IsResolutionLinked)
                {
                    double ratio = workspace.IFS.ImageResolution.Width / (double) workspace.IFS.ImageResolution.Height;
                    workspace.IFS.ImageResolution = new System.Drawing.Size((int)(value * ratio), value);
                }
                else
                {
                    workspace.IFS.ImageResolution = new System.Drawing.Size(workspace.IFS.ImageResolution.Width, value);
                }
                workspace.Renderer.SetHistogramScale(1.0);
                OnPropertyChanged(nameof(ImageWidth));
                OnPropertyChanged(nameof(ImageHeight));
            }
        }

        private AsyncRelayCommand _previewPresetCommand;
        public AsyncRelayCommand PreviewPresetCommand =>
            _previewPresetCommand ??= new AsyncRelayCommand(OnPreviewPresetCommand);
        private async Task OnPreviewPresetCommand()
        {
            workspace.Renderer.SetHistogramScaleToDisplay();
            //EnableDE = true;
            //EnableTAA = true;
            //EnablePerceptualUpdates = false;
            MaxFilterRadius = 0;
        }

        private AsyncRelayCommand _finalPresetCommand;
        public AsyncRelayCommand FinalPresetCommand =>
            _finalPresetCommand ??= new AsyncRelayCommand(OnFinalPresetCommand);
        private async Task OnFinalPresetCommand()
        {
            EnableTAA = false;
            EnableDE = false;
            MaxFilterRadius = 3;
            workspace.Renderer.SetHistogramScale(1.0);
        }

    }
}
