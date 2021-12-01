using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class QualitySettingsViewModel
{
    private readonly Workspace _workspace;

    [ObservableProperty] private bool _isResolutionLinked;

    public QualitySettingsViewModel(Workspace workspace)
    {
        _workspace = workspace;
        workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
    }

    public bool EnableDE
    {
        get => _workspace.Renderer.EnableDE;
        set
        {
            _workspace.Renderer.EnableDE = value;
            OnPropertyChanged(nameof(EnableDE));
            OnPropertyChanged(nameof(DEPanelVisibility));
            _workspace.Renderer.InvalidateDisplay();
        }
    }
    public Visibility DEPanelVisibility => EnableDE ? Visibility.Visible : Visibility.Collapsed;

    public string PreviewResolutionText
    {
        get
        {
            if (_workspace.Ifs.ImageResolution.Width == _workspace.Renderer.HistogramWidth)
                return null;
            else
                return $"{_workspace.Renderer.HistogramWidth} x {_workspace.Renderer.HistogramHeight}";
        }
    }

    public int DEMaxRadius
    {
        get => _workspace.Renderer.DEMaxRadius;
        set
        {
            _workspace.Renderer.DEMaxRadius = value;
            OnPropertyChanged(nameof(DEMaxRadius));
            _workspace.Renderer.InvalidateDisplay();
        }
    }

    public double DEThreshold
    {
        get => _workspace.Renderer.DEThreshold;
        set
        {
            _workspace.Renderer.DEThreshold = value;
            OnPropertyChanged(nameof(DEThreshold));
            _workspace.Renderer.InvalidateDisplay();
        }
    }

    public double DEPower
    {
        get => _workspace.Renderer.DEPower;
        set
        {
            _workspace.Renderer.DEPower = value;
            OnPropertyChanged(nameof(DEPower));
            _workspace.Renderer.InvalidateDisplay();
        }
    }

    public bool EnableTAA
    {
        get => _workspace.Renderer.EnableTAA;
        set
        {
            _workspace.Renderer.EnableTAA = value;
            OnPropertyChanged(nameof(EnableTAA));
            _workspace.Renderer.InvalidateDisplay();
        }
    }

    public int EntropyInv
    {
        get => (int)(1.0 / _workspace.Ifs.Entropy);
        set
        {
            _workspace.Ifs.Entropy = 1.0 / value;
            OnPropertyChanged(nameof(EntropyInv));
            _workspace.Renderer.InvalidateHistogramBuffer();
        }
    }

    public int Warmup
    {
        get => _workspace.Ifs.Warmup;
        set
        {
            _workspace.Ifs.Warmup = value;
            OnPropertyChanged(nameof(Warmup));
            _workspace.Renderer.InvalidateHistogramBuffer();
        }
    }

    public int MaxFilterRadius
    {
        get => _workspace.Renderer.MaxFilterRadius;
        set
        {
            _workspace.Renderer.MaxFilterRadius = value;
            OnPropertyChanged(nameof(MaxFilterRadius));
            OnPropertyChanged(nameof(FilterText));
            _workspace.Renderer.InvalidateHistogramBuffer();
        }
    }

    public string FilterText => "Max Filter Radius" + (MaxFilterRadius > 0 ? "" : " (Off)");

    public int ImageWidth
    {
        get
        {
            return _workspace.Ifs.ImageResolution.Width;
        }
        set
        {
            _workspace.TakeSnapshot();
            if (IsResolutionLinked)
            {
                double ratio = _workspace.Ifs.ImageResolution.Width / (double)_workspace.Ifs.ImageResolution.Height;
                _workspace.Ifs.ImageResolution = new System.Drawing.Size(value, (int)(value / ratio));
            }
            else
            {
                _workspace.Ifs.ImageResolution = new System.Drawing.Size(value, _workspace.Ifs.ImageResolution.Height);
            }
            _workspace.Renderer.SetHistogramScale(1.0);
            OnPropertyChanged(nameof(ImageWidth));
            OnPropertyChanged(nameof(ImageHeight));
        }
    }

    public int ImageHeight
    {
        get
        {
            return _workspace.Ifs.ImageResolution.Height;
        }
        set
        {
            _workspace.TakeSnapshot();
            if (IsResolutionLinked)
            {
                double ratio = _workspace.Ifs.ImageResolution.Width / (double)_workspace.Ifs.ImageResolution.Height;
                _workspace.Ifs.ImageResolution = new System.Drawing.Size((int)(value * ratio), value);
            }
            else
            {
                _workspace.Ifs.ImageResolution = new System.Drawing.Size(_workspace.Ifs.ImageResolution.Width, value);
            }
            _workspace.Renderer.SetHistogramScale(1.0);
            OnPropertyChanged(nameof(ImageWidth));
            OnPropertyChanged(nameof(ImageHeight));
        }
    }

    [ICommand]
    private void PreviewPreset()
    {
        _workspace.Renderer.SetHistogramScaleToDisplay();
        //EnableDE = true;
        //EnableTAA = true;
        //EnablePerceptualUpdates = false;
        MaxFilterRadius = 0;
        OnPropertyChanged(nameof(PreviewResolutionText));
    }

    [ICommand]
    private void FinalPreset()
    {
        EnableTAA = false;
        EnableDE = false;
        MaxFilterRadius = 3;
        _workspace.Renderer.SetHistogramScale(1.0);
        OnPropertyChanged(nameof(PreviewResolutionText));
    }
}
