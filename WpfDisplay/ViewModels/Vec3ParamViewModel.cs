using System.Numerics;

using IFSEngine.Model;

using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public partial class Vec3ParamViewModel : ParamViewModelBase<Vector3>
{
    public Vec3ParamViewModel(string name, IParamSource source, Workspace workspace) : base(name, source, workspace) { }

    public double XValue
    {
        get => source.Vec3Params[Name].X;
        set => source.Vec3Params[Name] = new Vector3((float)value, source.Vec3Params[Name].Y, source.Vec3Params[Name].Z);
    }

    public double YValue
    {
        get => source.Vec3Params[Name].Y;
        set => source.Vec3Params[Name] = new Vector3(source.Vec3Params[Name].X, (float)value, source.Vec3Params[Name].Z);
    }

    public double ZValue
    {
        get => source.Vec3Params[Name].Z;
        set => source.Vec3Params[Name] = new Vector3(source.Vec3Params[Name].X, source.Vec3Params[Name].Y, (float)value);
    }

    private ValueSliderSettings _xSlider;
    public ValueSliderSettings XSlider => _xSlider ??= new()
    {
        Label = $"{Name}-X",
        IsLabelShown = false,
        DefaultValue = source.Vec3ParamDefaults[Name].X,
        Increment = 0.001,
        AnimationPath = $"{source.ParamPathPrefix}.Vec3Params.[{Name}].X",
        ValueWillChange = workspace.TakeSnapshot,
        ValueChanged = (v) => workspace.Renderer.InvalidateParamsBuffer()
    };

    private ValueSliderSettings _ySlider;
    public ValueSliderSettings YSlider => _ySlider ??= new()
    {
        Label = $"{Name}-Y",
        IsLabelShown = false,
        DefaultValue = source.Vec3ParamDefaults[Name].Y,
        Increment = 0.001,
        AnimationPath = $"{source.ParamPathPrefix}.Vec3Params.[{Name}].Y",
        ValueWillChange = workspace.TakeSnapshot,
        ValueChanged = (v) => workspace.Renderer.InvalidateParamsBuffer()
    };

    private ValueSliderSettings _zSlider;
    public ValueSliderSettings ZSlider => _zSlider ??= new()
    {
        Label = $"{Name}-Z",
        IsLabelShown = false,
        DefaultValue = source.Vec3ParamDefaults[Name].Z,
        Increment = 0.001,
        AnimationPath = $"{source.ParamPathPrefix}.Vec3Params.[{Name}].Z",
        ValueWillChange = workspace.TakeSnapshot,
        ValueChanged = (v) => workspace.Renderer.InvalidateParamsBuffer()
    };

}
