using IFSEngine.Model;
using IFSEngine.Utility;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;
using WpfDisplay.Helper;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public partial class IteratorViewModel : ObservableObject
{
    private readonly Iterator iterator;
    private readonly Workspace _workspace;
    public Iterator Iterator => iterator;

    public event EventHandler ConnectingStarted;
    public event EventHandler ConnectingEnded;

    public List<INotifyPropertyChanged> Parameters { get; } = new();

    public required IRelayCommand<IteratorViewModel> RemoveCommand { get; init; }
    public required IRelayCommand<IteratorViewModel> DuplicateCommand { get; init; }
    public required IRelayCommand<IteratorViewModel> SplitCommand { get; init; }

    public IteratorViewModel(Iterator iterator, Workspace workspace)
    {
        this.iterator = iterator;
        _workspace = workspace;
        workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
        ReloadParameters();
    }

    public void ReloadParameters()
    {
        Parameters.Clear();
        Parameters.AddRange(iterator.RealParams.Select(v => new RealParamViewModel(v.Key, iterator, _workspace)));
        Parameters.AddRange(iterator.Vec3Params.Select(v => new Vec3ParamViewModel(v.Key, iterator, _workspace)));
        foreach (var v in Parameters)
        {
            v.PropertyChanged += (s, e) =>
            {
                OnPropertyChanged(e.PropertyName);
                _workspace.Renderer.InvalidateParamsBuffer();
            };
        }
    }

    public void Redraw()
    {
        OnPropertyChanged(string.Empty);
        OnPropertyChanged(nameof(Position));
        RaiseConnectionPropertyChanged();
    }

    public void RaiseConnectionPropertyChanged()
    {
        OnPropertyChanged("ConnectionProps");
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ForegroundZIndex))]
    private bool _isSelected;

    public int ForegroundZIndex => IsSelected ? 3 : 2;
    private ValueSliderSettings _startWeight;
    public ValueSliderSettings StartWeightSlider => _startWeight ??= new()
    {
        Label = "Start Weight",
        ToolTip = "Controls the probability that the iteration starts in this state.",
        DefaultValue = 1.0,
        MinValue = 0,
        Increment = 0.01,
        AnimationPath = $"[{iterator.Id}].{nameof(iterator.StartWeight)}",
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (v) => _workspace.Renderer.InvalidateParamsBuffer()
    };

    private ValueSliderSettings _opacity;
    public ValueSliderSettings OpacitySlider => _opacity ??= new()
    {
        Label = "Opacity",
        ToolTip = "Setting this to 0 means this iterator will not draw anything.",
        DefaultValue = 1.0,
        MinValue = -1,
        MaxValue = 1,
        Increment = 0.01,
        AnimationPath = $"[{iterator.Id}].{nameof(iterator.Opacity)}",
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderSettings _colorIndex;
    public ValueSliderSettings ColorIndexSlider => _colorIndex ??= new()
    {
        Label = "Color Index",
        ToolTip = "Indexes to the color on the palette. 0 -> Left most color, 1 -> Right most color.",
        DefaultValue = 0.0,
        MinValue = 0,
        MaxValue = 1,
        Increment = 0.01,
        AnimationPath = $"[{iterator.Id}].{nameof(iterator.ColorIndex)}",
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderSettings _colorSpeed;
    public ValueSliderSettings ColorSpeedSlider => _colorSpeed ??= new()
    {
        Label = "Color Speed",
        ToolTip = "The Color Speed controls how fast the color state of the IFS progresses towards this iterator's Color Index. 0 -> no effect on colors. 1 -> Draw with the selected color index immediately.",
        DefaultValue = 0.0,
        Increment = 0.01,
        AnimationPath = $"[{iterator.Id}].{nameof(iterator.ColorSpeed)}",
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (v) => { _workspace.Renderer.InvalidateParamsBuffer(); OnPropertyChanged(nameof(ColorRGB)); }
    };

    private ValueSliderSettings _mix;
    public ValueSliderSettings MixSlider => _mix ??= new()
    {
        Label = "Mix",
        ToolTip = "Linearly interpolate between the states before/after the transform. 0 -> The transform has no effect on the position. 1 -> Default.",
        DefaultValue = 1.0,
        Increment = 0.001,
        AnimationPath = $"[{iterator.Id}].{nameof(iterator.Mix)}",
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (v) => _workspace.Renderer.InvalidateParamsBuffer()
    };

    private ValueSliderSettings _add;
    public ValueSliderSettings AddSlider => _add ??= new()
    {
        Label = "Add",
        ToolTip = "Add up the positions before/after the transform.",
        DefaultValue = 0.0,
        Increment = 0.001,
        AnimationPath = $"[{iterator.Id}].{nameof(iterator.Add)}",
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (v) => _workspace.Renderer.InvalidateParamsBuffer()
    };

    public double ColorIndex
    {
        get => iterator.ColorIndex;
        set
        {
            iterator.ColorIndex = value;
            _workspace.Renderer.InvalidateParamsBuffer();
            OnPropertyChanged(nameof(ColorIndex));
            OnPropertyChanged(nameof(ColorRGB));
        }
    }

    public Color ColorRGB
    {
        get
        {
            var c = _workspace.Ifs.Palette.GetColorLerp((float)ColorIndex);
            return Color.FromRgb((byte)(c.X * 255), (byte)(c.Y * 255), (byte)(c.Z * 255));
        }
    }

    public bool DeltaColoring
    {
        get => iterator.ShadingMode == ShadingMode.DeltaPSpeed;
        set
        {
            iterator.ShadingMode = value ? ShadingMode.DeltaPSpeed : ShadingMode.Default;
            OnPropertyChanged(nameof(DeltaColoring));
            _workspace.Renderer.InvalidateParamsBuffer();
        }
    }

    public double Opacity
    {
        get => iterator.Opacity;
        set
        {
            iterator.Opacity = value;
            _workspace.Renderer.InvalidateParamsBuffer();
            OnPropertyChanged(nameof(Opacity));
            OnPropertyChanged(nameof(OpacityColor));
        }
    }

    public double BaseWeight
    {
        get => iterator.BaseWeight;
        set
        {
            iterator.BaseWeight = value;
            _workspace.Renderer.InvalidateParamsBuffer();
            OnPropertyChanged(nameof(BaseWeight));
            OnPropertyChanged(nameof(NodeSize));
            OnPropertyChanged(nameof(RenderTranslateValue));
            RaiseConnectionPropertyChanged();
        }
    }

    public Color OpacityColor
    {
        get
        {
            byte o = (byte)(100 + Math.Clamp(iterator.Opacity, 0, 1) * 255 * 0.6);
            return Color.FromRgb(o, o, o);//grayscale
        }
    }

    private ValueSliderSettings _baseWeight;
    public ValueSliderSettings BaseWeightSlider => _baseWeight ??= new()
    {
        Label = "Base Weight",
        ToolTip = "Multiplies incoming connection weights for ease of use. 0 base weight means this iterator does not take part in the iteration process, because the IFS never transitions here.",
        DefaultValue = 1.0,
        MinValue = 0,
        Increment = 0.01,
        AnimationPath = $"[{iterator.Id}].{nameof(iterator.BaseWeight)}",
        ValueWillChange = _workspace.TakeSnapshot,
    };

    public double NodeSize
    {
        get
        {
            //if (!EnableWeightedSize)
            //    return BaseSize;
            return 100 + Math.Clamp(10 * iterator.BaseWeight, 0.0, 50.0) * 2;
        }
    }

    public double RenderTranslateValue => -0.5 * NodeSize;

    public string IteratorName
    {
        get => iterator.Name;
        set
        {
            if (string.IsNullOrEmpty(value))
                iterator.Name = null;
            else
                iterator.Name = value;
            OnPropertyChanged(nameof(IteratorName));
            OnPropertyChanged(nameof(NodeLabel));
        }
    }
    public string TransformName => iterator.Transform.Name;
    public string NodeLabel => iterator.Name ?? iterator.Transform.Name;

    public BindablePoint Position
    {
        get => iterator.GetPosition() ?? new BindablePoint(RandHelper.Next(500), RandHelper.Next(500));
        set
        {
            iterator.SetPosition(value);
            OnPropertyChanged(nameof(Position));
            Redraw();
        }
    }

    public void StartConnecting() => ConnectingStarted?.Invoke(this, null);

    public void FinishConnecting() => ConnectingEnded?.Invoke(this, null);

    [RelayCommand]
    private void ConnectSelf()
    {
        StartConnecting();
        FinishConnecting();
    }

    [RelayCommand]
    private void TakeSnapshot() => _workspace.TakeSnapshot();

    [RelayCommand]
    private void FlipOpacity()
    {
        _workspace.TakeSnapshot();
        if (Opacity > 0.0f)
            Opacity = 0.0f;
        else
            Opacity = 1.0f;
    }

    [RelayCommand]
    private void FlipWeight()
    {
        _workspace.TakeSnapshot();
        if (BaseWeight > 0.0f)
            BaseWeight = 0.0f;
        else
            BaseWeight = 1.0f;
    }
}
