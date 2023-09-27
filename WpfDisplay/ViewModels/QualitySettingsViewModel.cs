using CommunityToolkit.Mvvm.ComponentModel;
using IFSEngine.Model;
using System;
using System.Numerics;
using System.Windows;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public partial class QualitySettingsViewModel : ObservableObject
{
    private readonly Workspace _workspace;

    [ObservableProperty] private bool _isResolutionLinked;
    private bool _isFinalRenderingMode = false;
    public bool IsFinalRenderingMode
    {
        get => _isFinalRenderingMode;
        set
        {
            if (value)
                SetFinalRenderSettings();
            else
                SetPreviewRenderSettings();
            SetProperty(ref _isFinalRenderingMode, value);
        }
    }

    public QualitySettingsViewModel(Workspace workspace)
    {
        _workspace = workspace;
        workspace.LoadedParamsChanged += (s, e) => OnPropertyChanged(string.Empty);
        workspace.Renderer.DisplayFramebufferUpdated += (s, e) =>
        {
            OnPropertyChanged(nameof(IterationLevel));
            OnPropertyChanged(nameof(IterationProgressPercent));
        };
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

    private ValueSliderViewModel _targetIterationLevel;
    public ValueSliderViewModel TargetIterationLevel => _targetIterationLevel ??= new ValueSliderViewModel(_workspace)
    {
        Label = "🎌 Target Iteration Level",
        ToolTip = $"The image is considered finished when the rendering progress reaches this level. It is recommended to animate this value when rendering animations, since certain frames may require a lot more iterations than others. Default value is {IFS.Default.TargetIterationLevel}.",
        AnimationPath = "TargetIterationLevel",
        DefaultValue = IFS.Default.TargetIterationLevel,
        GetV = () => _workspace.Ifs.TargetIterationLevel,
        SetV = (value) =>
        {
            _workspace.Ifs.TargetIterationLevel = (int)value;
        },
        MinValue = 1,
        MaxValue = 50,
        Increment = 1,
        ValueWillChange = _workspace.TakeSnapshot,
    };

    public string IterationLevel => BitOperations.Log2(1 + _workspace.Renderer.TotalIterations / (ulong)(_workspace.Renderer.HistogramWidth * _workspace.Renderer.HistogramHeight)).ToString();
    public double IterationProgressPercent => 100.0*(_workspace.Renderer.TotalIterations / (double)(_workspace.Renderer.HistogramWidth * _workspace.Renderer.HistogramHeight)) / (Math.Pow(2, _workspace.Ifs.TargetIterationLevel)-1);

    private ValueSliderViewModel _deMaxRadius;
    public ValueSliderViewModel DEMaxRadius => _deMaxRadius ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Radius",
        DefaultValue = 0,
        GetV = () => _workspace.Renderer.DEMaxRadius,
        SetV = (value) =>
        {
            _workspace.Renderer.DEMaxRadius = (int)value;
            _workspace.Renderer.InvalidateDisplay();
        },
        MinValue = 0,
        MaxValue = 20,
        Increment = 1,
    };

    private ValueSliderViewModel _dePower;
    public ValueSliderViewModel DEPower => _dePower ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Power",
        DefaultValue = 0.4,
        GetV = () => _workspace.Renderer.DEPower,
        SetV = (value) =>
        {
            _workspace.Renderer.DEPower = value;
            _workspace.Renderer.InvalidateDisplay();
        },
        MinValue = 0,
        MaxValue = 1,
        Increment = 0.01,
    };

    private ValueSliderViewModel _deThreshold;
    public ValueSliderViewModel DEThreshold => _deThreshold ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Threshold",
        DefaultValue = 0,
        GetV = () => _workspace.Renderer.DEThreshold,
        SetV = (value) =>
        {
            _workspace.Renderer.DEThreshold = value;
            _workspace.Renderer.InvalidateDisplay();
        },
        MinValue = 0,
        MaxValue = 1,
        Increment = 0.01,
    };

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

    private ValueSliderViewModel _entropyInv;
    public ValueSliderViewModel EntropyInv => _entropyInv ??= new ValueSliderViewModel(_workspace)
    {
        Label = "🔬 1 / Entropy",
        ToolTip = $"Entropy is the chance to reset the point state in each iteration. This replaces the constant 10 000 iteration depth in Flame. Default value is {(int)(1.0 / IFS.Default.Entropy)}.",
        DefaultValue = (int)(1.0 / IFS.Default.Entropy),
        GetV = () => (int)(1.0 / _workspace.Ifs.Entropy),
        SetV = (value) =>
        {
            _workspace.Ifs.Entropy = 1.0 / value;
            _workspace.Renderer.InvalidateHistogramBuffer();
        },
        MinValue = 10,
        MaxValue = 10000,//TODO: increase
        Increment = 10,
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _warmup;
    public ValueSliderViewModel Warmup => _warmup ??= new ValueSliderViewModel(_workspace)
    {
        Label = "🌡 Warmup",
        ToolTip = $"A.k.a. 'fuse count', the number of iterations before plotting starts. Default is {IFS.Default.Warmup}.",
        DefaultValue = IFS.Default.Warmup,
        GetV = () => _workspace.Ifs.Warmup,
        SetV = (value) =>
        {
            _workspace.Ifs.Warmup = (int)value;
            _workspace.Renderer.InvalidateHistogramBuffer();
        },
        MinValue = 0,
        Increment = 10,
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _maxFilterRadius;
    public ValueSliderViewModel MaxFilterRadius => _maxFilterRadius ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Filter Radius (Off)",
        DefaultValue = 0,
        GetV = () => _workspace.Renderer.MaxFilterRadius,
        SetV = (value) =>
        {
            _workspace.Renderer.MaxFilterRadius = (int)value;
            _workspace.Renderer.InvalidateHistogramBuffer();
            MaxFilterRadius.Label = "Filter Radius" + (MaxFilterRadius.Value > 0 ? "" : " (Off)");
            OnPropertyChanged("Label");//TODO: MaxFilterRadius.Raise.. 
        },
        MinValue = 0,
        MaxValue = 3,
        Increment = 1,
    };

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

    private void SetPreviewRenderSettings()
    {
        _workspace.Renderer.SetHistogramScaleToDisplay();
        MaxFilterRadius.Value = 0;
        OnPropertyChanged(nameof(PreviewResolutionText));
    }

    private void SetFinalRenderSettings()
    {
        EnableTAA = false;
        MaxFilterRadius.Value = 3;
        _workspace.Renderer.SetHistogramScale(1.0);
        OnPropertyChanged(nameof(PreviewResolutionText));
    }
}
