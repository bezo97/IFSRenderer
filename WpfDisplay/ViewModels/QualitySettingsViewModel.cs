using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IFSEngine.Model;
using IFSEngine.Rendering;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Windows;
using WpfDisplay.Models;
using static OpenTK.Graphics.OpenGL.GL;

namespace WpfDisplay.ViewModels;

public partial class QualitySettingsViewModel : ObservableObject
{
    private readonly Workspace _workspace;
    public RendererGL Renderer => _workspace.Renderer;
    public IFS Ifs => _workspace.Ifs;
    public IReadOnlyDictionary<string, int[]> ResolutionPresets => _workspace.ResolutionPresets;

    [ObservableProperty] private bool _isResolutionLinked;
    private bool _isFinalRenderingMode = false;
    public string MaxFilterRadiusLabel => "Filter Radius" + (_workspace.Renderer.MaxFilterRadius > 0 ? "" : " (Off)");
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
        _workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
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

    private ValueSliderSettings _targetIterationLevel;
    public ValueSliderSettings TargetIterationLevelSlider => _targetIterationLevel ??= new()
    {
        Label = "🎌 Target Iteration Level",
        ToolTip = $"The image is considered finished when the rendering progress reaches this level. It is recommended to animate this value when rendering animations, since certain frames may require a lot more iterations than others. Default value is {IFS.Default.TargetIterationLevel}.",
        AnimationPath = "TargetIterationLevel",
        DefaultValue = IFS.Default.TargetIterationLevel,
        MinValue = 1,
        MaxValue = 50,
        Increment = 1,
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderSettings _deMaxRadius;
    public ValueSliderSettings DEMaxRadiusSlider => _deMaxRadius ??= new()
    {
        Label = "Radius",
        DefaultValue = 0,
        MinValue = 0,
        MaxValue = 20,
        Increment = 1,
        ValueChanged = (v) => _workspace.Renderer.InvalidateDisplay()
    };

    private ValueSliderSettings _dePower;
    public ValueSliderSettings DEPowerSlider => _dePower ??= new()
    {
        Label = "Power",
        DefaultValue = 0.4,
        MinValue = 0,
        MaxValue = 1,
        Increment = 0.01,
        ValueChanged = (v) => _workspace.Renderer.InvalidateDisplay()
    };

    private ValueSliderSettings _deThreshold;
    public ValueSliderSettings DEThresholdSlider => _deThreshold ??= new()
    {
        Label = "Threshold",
        DefaultValue = 0,
        MinValue = 0,
        MaxValue = 1,
        Increment = 0.01,
        ValueChanged = (v) => _workspace.Renderer.InvalidateDisplay()
    };

    public int EntropyInv
    {
        get => (int)(1.0 / _workspace.Ifs.Entropy);
        set => _workspace.Ifs.Entropy = 1.0 / value;
    }

    private ValueSliderSettings _entropyInv;
    public ValueSliderSettings EntropyInvSlider => _entropyInv ??= new()
    {
        Label = "☁︎ 1 / Entropy",
        ToolTip = $"Entropy is the chance to reset the point state in each iteration. This replaces the constant 10 000 iteration depth in Flame. Default value is {(int)(1.0 / IFS.Default.Entropy)}.",
        DefaultValue = (int)(1.0 / IFS.Default.Entropy),
        MinValue = 10,
        MaxValue = 100000,
        Increment = 10,
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (v) => _workspace.Renderer.InvalidateHistogramBuffer()
    };

    private ValueSliderSettings _warmup;
    public ValueSliderSettings WarmupSlider => _warmup ??= new()
    {
        Label = "🌡 Warmup",
        ToolTip = $"A.k.a. 'fuse count', the number of iterations before plotting starts. Default is {IFS.Default.Warmup}.",
        DefaultValue = IFS.Default.Warmup,
        MinValue = 0,
        Increment = 10,
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (v) => _workspace.Renderer.InvalidateHistogramBuffer()
    };

    private ValueSliderSettings _maxFilterRadius;
    public ValueSliderSettings MaxFilterRadiusSlider => _maxFilterRadius ??= new()
    {
        Label = "Filter Radius",
        DefaultValue = 0,
        MinValue = 0,
        MaxValue = 3,
        Increment = 1,
        ValueChanged = (v) =>
        {
            _workspace.Renderer.InvalidateHistogramBuffer();
            OnPropertyChanged(nameof(MaxFilterRadiusLabel));
        }
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

    [RelayCommand]
    private void ApplyResolutionPreset(int[] dims)
    {
        _workspace.Ifs.ImageResolution = new System.Drawing.Size(dims[0], dims[1]);
        _workspace.Renderer.SetHistogramScale(1.0);
        OnPropertyChanged(nameof(ImageWidth));
        OnPropertyChanged(nameof(ImageHeight));
    }

    public void UpdatePreviewRenderSettings()
    {
        if (!IsFinalRenderingMode)
            SetPreviewRenderSettings();
    }

    private void SetPreviewRenderSettings()
    {
        _workspace.Renderer.SetHistogramScaleToDisplay();
        Renderer.MaxFilterRadius = 0;
        OnPropertyChanged(nameof(Renderer));
        OnPropertyChanged(nameof(PreviewResolutionText));
    }

    private void SetFinalRenderSettings()
    {
        Renderer.MaxFilterRadius = 3;
        _workspace.Renderer.SetHistogramScale(1.0);
        OnPropertyChanged(nameof(Renderer));
        OnPropertyChanged(nameof(PreviewResolutionText));
        OnPropertyChanged(nameof(MaxFilterRadiusLabel));
    }
}
