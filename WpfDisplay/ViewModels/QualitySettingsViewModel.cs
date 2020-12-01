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

        private AsyncRelayCommand _startRenderingCommand;
        public AsyncRelayCommand StartRenderingCommand =>
            _startRenderingCommand ??= new AsyncRelayCommand(OnStartRenderingCommand);
        private async Task OnStartRenderingCommand()
        {
            workspace.Renderer.StartRenderLoop();
        }

        public bool EnableDE
        {
            get => workspace.Renderer.EnableDE;
            set
            {
                workspace.Renderer.EnableDE = value;
                OnPropertyChanged(nameof(EnableDE));
                OnPropertyChanged(nameof(DEPanelVisibility));
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
                OnPropertyChanged(nameof(DEMaxRadius));
                workspace.Renderer.UpdateDisplay();
            }
        }

        public double DEThreshold
        {
            get => workspace.Renderer.DEThreshold;
            set
            {
                workspace.Renderer.DEThreshold = value;
                OnPropertyChanged(nameof(DEThreshold));
                workspace.Renderer.UpdateDisplay();
            }
        }

        public double DEPower
        {
            get => workspace.Renderer.DEPower;
            set
            {
                workspace.Renderer.DEPower = value;
                OnPropertyChanged(nameof(DEPower));
                workspace.Renderer.UpdateDisplay();
            }
        }

        public bool EnableTAA
        {
            get => workspace.Renderer.EnableTAA;
            set
            {
                workspace.Renderer.EnableTAA = value;
                OnPropertyChanged(nameof(EnableTAA));
                workspace.Renderer.UpdateDisplay();
            }
        }

        public bool EnablePerceptualUpdates
        {
            get => workspace.Renderer.EnablePerceptualUpdates;
            set
            {
                workspace.Renderer.EnablePerceptualUpdates = value;
                OnPropertyChanged(nameof(EnablePerceptualUpdates));
                workspace.Renderer.UpdateDisplay();
            }
        }

        public int EntropyInv
        {
            get => (int)(1.0 / workspace.Renderer.Entropy);
            set
            {
                workspace.Renderer.Entropy = 1.0 / value;
                OnPropertyChanged(nameof(EntropyInv));
                workspace.Renderer.InvalidateAccumulation();
            }
        }

        public int Warmup
        {
            get => workspace.Renderer.Warmup;
            set
            {
                workspace.Renderer.Warmup = value;
                OnPropertyChanged(nameof(Warmup));
                workspace.Renderer.InvalidateAccumulation();
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
                workspace.Renderer.InvalidateAccumulation();
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
                    //PassIters = 100;
                    //Warmup = 10;
                    EntropyInv = 100;
                    MaxFilterRadius = 0;
        }

    private AsyncRelayCommand _finalPresetCommand;
    public AsyncRelayCommand FinalPresetCommand =>
        _finalPresetCommand ??= new AsyncRelayCommand(OnFinalPresetCommand);
    private async Task OnFinalPresetCommand()
    {
        EnablePerceptualUpdates = true;
                    EnableTAA = false;
                    EnableDE = false;
                    //PassIters = 500;
                    //Warmup = 30;
                    EntropyInv = 10000;
                    MaxFilterRadius = 3;
                    workspace.Renderer.SetHistogramScale(1.0);
        }

    }
}
