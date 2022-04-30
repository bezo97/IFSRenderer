using IFSEngine.Model;
using IFSEngine.Utility;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using WpfDisplay.Helper;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class IteratorViewModel
{
    public readonly Iterator iterator;
    private readonly Workspace _workspace;

    public event EventHandler ConnectingStarted;
    public event EventHandler ConnectingEnded;

    public List<INotifyPropertyChanged> Parameters { get; } = new();

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

    public IRelayCommand RemoveCommand { get; set; }
    public IRelayCommand DuplicateCommand { get; set; }

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

    [ObservableProperty] private bool _isSelected;

    private ValueSliderViewModel _startWeight;
    public ValueSliderViewModel StartWeight => _startWeight ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Start Weight",
        ToolTip = "Controls the probability that the iteration starts in this state.",
        DefaultValue = 1.0,
        GetV = () => iterator.StartWeight,
        SetV = (value) => {
            iterator.StartWeight = value;
            _workspace.Renderer.InvalidateParamsBuffer();
        },
        MinValue = 0,
        Increment = 0.01,
        AnimationPath = $"[{iterator.Id}].{nameof(iterator.StartWeight)}",
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _opacity;
    public ValueSliderViewModel Opacity => _opacity ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Opacity",
        ToolTip = "Setting this to 0 means this iterator will not draw anything.",
        DefaultValue = 1.0,
        GetV = () => iterator.Opacity,
        SetV = (value) => {
            iterator.Opacity = value;
            _workspace.Renderer.InvalidateParamsBuffer();
            OnPropertyChanged(nameof(OpacityColor));
        },
        MinValue = -1,
        MaxValue = 1,
        Increment = 0.01,
        AnimationPath = $"[{iterator.Id}].{nameof(iterator.Opacity)}",
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _colorIndex;
    public ValueSliderViewModel ColorIndex => _colorIndex ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Color Index",
        ToolTip = "Indexes to the color on the palette. 0 -> Left most color, 1 -> Right most color.",
        DefaultValue = 0.0,
        GetV = () => iterator.ColorIndex,
        SetV = (value) => {
            iterator.ColorIndex = value;
            _workspace.Renderer.InvalidateParamsBuffer();
            OnPropertyChanged(nameof(ColorRGB));
        },
        MinValue = 0,
        MaxValue = 1,
        Increment = 0.01,
        AnimationPath = $"[{iterator.Id}].{nameof(iterator.ColorIndex)}",
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _colorSpeed;
    public ValueSliderViewModel ColorSpeed => _colorSpeed ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Color Speed",
        ToolTip = "The Color Speed controls how fast the color state of the IFS progresses towards this iterator's Color Index. 0 -> no effect on colors. 1 -> Draw with the selected color index immediately.",
        DefaultValue = 0.0,
        GetV = () => iterator.ColorSpeed,
        SetV = (value) => {
            iterator.ColorSpeed = value;
            _workspace.Renderer.InvalidateParamsBuffer();
            OnPropertyChanged(nameof(ColorRGB));
        },
        Increment = 0.01,
        AnimationPath = $"[{iterator.Id}].{nameof(iterator.ColorSpeed)}",
        ValueWillChange = _workspace.TakeSnapshot,
    };

    public Color ColorRGB
    {
        get
        {
            var colors = _workspace.Ifs.Palette.Colors;
            var c = colors[(int)(colors.Count * (ColorIndex.Value - Math.Floor(ColorIndex.Value)))];
            return Color.FromRgb((byte)(255 * c.X), (byte)(255 * c.Y), (byte)(255 * c.Z));
        }
    }

    private ValueSliderViewModel _mix;
    public ValueSliderViewModel Mix => _mix ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Mix",
        ToolTip = "Linearly interpolate between the states before/after the transform. 0 -> The transform has no effect on the position. 1 -> Default.",
        DefaultValue = 1.0,
        GetV = () => iterator.Mix,
        SetV = (value) => {
            iterator.Mix = value;
            _workspace.Renderer.InvalidateParamsBuffer();
        },
        Increment = 0.001,
        AnimationPath = $"[{iterator.Id}].{nameof(iterator.Mix)}",
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _add;
    public ValueSliderViewModel Add => _add ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Add",
        ToolTip = "Add up the positions before/after the transform.",
        DefaultValue = 0.0,
        GetV = () => iterator.Add,
        SetV = (value) => {
            iterator.Add = value;
            _workspace.Renderer.InvalidateParamsBuffer();
        },
        Increment = 0.001,
        AnimationPath = $"[{iterator.Id}].{nameof(iterator.Add)}",
        ValueWillChange = _workspace.TakeSnapshot,
    };

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

    public Color OpacityColor
    {
        get
        {
            byte o = (byte)(100 + Math.Clamp(Opacity.Value, 0, 1) * 255 * 0.6);
            return Color.FromRgb(o, o, o);//grayscale
        }
    }

    private ValueSliderViewModel _baseWeight;
    public ValueSliderViewModel BaseWeight => _baseWeight ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Base Weight",
        ToolTip = "Multiplies incoming connection weights for ease of use. 0 base weight means this iterator does not take part in the iteration process, because the IFS never transitions here.",
        DefaultValue = 1.0,
        GetV = () => iterator.BaseWeight,
        SetV = (value) => {
            iterator.BaseWeight = value;
            _workspace.Renderer.InvalidateParamsBuffer();
            OnPropertyChanged(nameof(NodeSize));
            OnPropertyChanged(nameof(RenderTranslateValue));
            RaiseConnectionPropertyChanged();
        },
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
            return 100 + Math.Clamp(10*BaseWeight.Value, 0.0, 50.0) * 2;
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

    [ICommand]
    private void TakeSnapshot() => _workspace.TakeSnapshot();

    [ICommand]
    private void FlipOpacity()
    {
        _workspace.TakeSnapshot();
        if (Opacity.Value > 0.0f)
            Opacity.Value = 0.0f;
        else
            Opacity.Value = 1.0f;

    }

    [ICommand]
    private void FlipWeight()
    {
        _workspace.TakeSnapshot();
        if (BaseWeight.Value > 0.0f)
            BaseWeight.Value = 0.0f;
        else
            BaseWeight.Value = 1.0f;
    }
}
