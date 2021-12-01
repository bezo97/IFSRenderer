using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class QualitySettingsViewModel
{
    private readonly Workspace workspace;

    [ObservableProperty] private bool _isResolutionLinked;

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

    public string PreviewResolutionText
    {
        get
        {
            if (workspace.Ifs.ImageResolution.Width == workspace.Renderer.HistogramWidth)
                return null;
            else
                return $"{workspace.Renderer.HistogramWidth} x {workspace.Renderer.HistogramHeight}";
        }
    }

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
        get => (int)(1.0 / workspace.Ifs.Entropy);
        set
        {
            workspace.Ifs.Entropy = 1.0 / value;
            OnPropertyChanged(nameof(EntropyInv));
            workspace.Renderer.InvalidateHistogramBuffer();
        }
    }

    public int Warmup
    {
        get => workspace.Ifs.Warmup;
        set
        {
            workspace.Ifs.Warmup = value;
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

    public int ImageWidth
    {
        get
        {
            return workspace.Ifs.ImageResolution.Width;
        }
        set
        {
            workspace.TakeSnapshot();
            if (IsResolutionLinked)
            {
                double ratio = workspace.Ifs.ImageResolution.Width / (double)workspace.Ifs.ImageResolution.Height;
                workspace.Ifs.ImageResolution = new System.Drawing.Size(value, (int)(value / ratio));
            }
            else
            {
                workspace.Ifs.ImageResolution = new System.Drawing.Size(value, workspace.Ifs.ImageResolution.Height);
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
            return workspace.Ifs.ImageResolution.Height;
        }
        set
        {
            workspace.TakeSnapshot();
            if (IsResolutionLinked)
            {
                double ratio = workspace.Ifs.ImageResolution.Width / (double)workspace.Ifs.ImageResolution.Height;
                workspace.Ifs.ImageResolution = new System.Drawing.Size((int)(value * ratio), value);
            }
            else
            {
                workspace.Ifs.ImageResolution = new System.Drawing.Size(workspace.Ifs.ImageResolution.Width, value);
            }
            workspace.Renderer.SetHistogramScale(1.0);
            OnPropertyChanged(nameof(ImageWidth));
            OnPropertyChanged(nameof(ImageHeight));
        }
    }

    [ICommand]
    private async Task PreviewPreset()
    {
        workspace.Renderer.SetHistogramScaleToDisplay();
        //EnableDE = true;
        //EnableTAA = true;
        //EnablePerceptualUpdates = false;
        MaxFilterRadius = 0;
        OnPropertyChanged(nameof(PreviewResolutionText));
    }

    [ICommand]
    private async Task FinalPreset()
    {
        EnableTAA = false;
        EnableDE = false;
        MaxFilterRadius = 3;
        workspace.Renderer.SetHistogramScale(1.0);
        OnPropertyChanged(nameof(PreviewResolutionText));
    }
}
