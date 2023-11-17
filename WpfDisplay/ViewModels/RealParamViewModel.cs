using IFSEngine.Model;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public partial class RealParamViewModel : ParamViewModelBase<double>
{
    public RealParamViewModel(string name, Iterator iterator, Workspace workspace) : base(name, iterator, workspace) { }

    public double RealParamValue
    {
        get => iterator.RealParams[Name];
        set => iterator.RealParams[Name] = value;
    }

    private ValueSliderSettings _realParamSlider;
    public ValueSliderSettings RealParamSlider => _realParamSlider ??= new()
    {
        Label = Name,
        DefaultValue = iterator.Transform.RealParams[Name],
        Increment = 0.001,
        AnimationPath = $"[{iterator.Id}].RealParams.[{Name}]",
        ValueWillChange = workspace.TakeSnapshot,
        ValueChanged = (Value) => workspace.Renderer.InvalidateParamsBuffer()
    };

}
