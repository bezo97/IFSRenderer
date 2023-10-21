using IFSEngine.Model;
using System.Numerics;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public partial class Vec3ParamViewModel : ParamViewModelBase<Vector3>
{
    public Vec3ParamViewModel(string name, Iterator iterator, Workspace workspace) : base(name, iterator, workspace) { }

    private ValueSliderViewModel _xSliderViewModel;
    public ValueSliderViewModel XSliderViewModel => _xSliderViewModel ??= new ValueSliderViewModel(workspace)
    {
        Label = $"{Name}-X",
        IsLabelShown = false,
        DefaultValue = iterator.Transform.Vec3Params[Name].X,
        GetV = () => iterator.Vec3Params[Name].X,
        SetV = (value) =>
        {
            iterator.Vec3Params[Name] = new Vector3((float)value, iterator.Vec3Params[Name].Y, iterator.Vec3Params[Name].Z);
            workspace.Renderer.InvalidateParamsBuffer();
        },
        Increment = 0.001,
        AnimationPath = $"[{iterator.Id}].Vec3Params.[{Name}].X",
        ValueWillChange = workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _ySliderViewModel;
    public ValueSliderViewModel YSliderViewModel => _ySliderViewModel ??= new ValueSliderViewModel(workspace)
    {
        Label = $"{Name}-Y",
        IsLabelShown = false,
        DefaultValue = iterator.Transform.Vec3Params[Name].Y,
        GetV = () => iterator.Vec3Params[Name].Y,
        SetV = (value) =>
        {
            iterator.Vec3Params[Name] = new Vector3(iterator.Vec3Params[Name].X, (float)value, iterator.Vec3Params[Name].Z);
            workspace.Renderer.InvalidateParamsBuffer();
        },
        Increment = 0.001,
        AnimationPath = $"[{iterator.Id}].Vec3Params.[{Name}].Y",
        ValueWillChange = workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _zSliderViewModel;
    public ValueSliderViewModel ZSliderViewModel => _zSliderViewModel ??= new ValueSliderViewModel(workspace)
    {
        Label = $"{Name}-Z",
        IsLabelShown = false,
        DefaultValue = iterator.Transform.Vec3Params[Name].Z,
        GetV = () => iterator.Vec3Params[Name].Z,
        SetV = (value) =>
        {
            iterator.Vec3Params[Name] = new Vector3(iterator.Vec3Params[Name].X, iterator.Vec3Params[Name].Y, (float)value);
            workspace.Renderer.InvalidateParamsBuffer();
        },
        Increment = 0.001,
        AnimationPath = $"[{iterator.Id}].Vec3Params.[{Name}].Z",
        ValueWillChange = workspace.TakeSnapshot,
    };

}
