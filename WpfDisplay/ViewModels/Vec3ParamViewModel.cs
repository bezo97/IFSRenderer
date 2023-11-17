using IFSEngine.Model;
using System.Numerics;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public partial class Vec3ParamViewModel : ParamViewModelBase<Vector3>
{
    public Vec3ParamViewModel(string name, Iterator iterator, Workspace workspace) : base(name, iterator, workspace) { }

    public double XValue
    {
        get => iterator.Vec3Params[Name].X;
        set => iterator.Vec3Params[Name] = new Vector3((float)value, iterator.Vec3Params[Name].Y, iterator.Vec3Params[Name].Z);
    }

    public double YValue
    {
        get => iterator.Vec3Params[Name].Y;
        set => iterator.Vec3Params[Name] = new Vector3(iterator.Vec3Params[Name].X, (float)value, iterator.Vec3Params[Name].Z);
    }

    public double ZValue
    {
        get => iterator.Vec3Params[Name].Z;
        set => iterator.Vec3Params[Name] = new Vector3(iterator.Vec3Params[Name].X, iterator.Vec3Params[Name].Y, (float)value);
    }

    private ValueSliderSettings _xSlider;
    public ValueSliderSettings XSlider => _xSlider ??= new()
    {
        Label = $"{Name}-X",
        IsLabelShown = false,
        DefaultValue = iterator.Transform.Vec3Params[Name].X,
        Increment = 0.001,
        AnimationPath = $"[{iterator.Id}].Vec3Params.[{Name}].X",
        ValueWillChange = workspace.TakeSnapshot,
        ValueChanged = (v) => workspace.Renderer.InvalidateParamsBuffer()
    };

    private ValueSliderSettings _ySlider;
    public ValueSliderSettings YSlider => _ySlider ??= new()
    {
        Label = $"{Name}-Y",
        IsLabelShown = false,
        DefaultValue = iterator.Transform.Vec3Params[Name].Y,
        Increment = 0.001,
        AnimationPath = $"[{iterator.Id}].Vec3Params.[{Name}].Y",
        ValueWillChange = workspace.TakeSnapshot,
        ValueChanged = (v) => workspace.Renderer.InvalidateParamsBuffer()
    };

    private ValueSliderSettings _zSlider;
    public ValueSliderSettings ZSlider => _zSlider ??= new()
    {
        Label = $"{Name}-Z",
        IsLabelShown = false,
        DefaultValue = iterator.Transform.Vec3Params[Name].Z,
        Increment = 0.001,
        AnimationPath = $"[{iterator.Id}].Vec3Params.[{Name}].Z",
        ValueWillChange = workspace.TakeSnapshot,
        ValueChanged = (v) => workspace.Renderer.InvalidateParamsBuffer()
    };

}
