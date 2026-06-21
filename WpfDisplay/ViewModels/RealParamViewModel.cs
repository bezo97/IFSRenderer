using IFSEngine.Model;

using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public partial class RealParamViewModel : ParamViewModelBase<double>
{
    public RealParamViewModel(string name, IParamSource source, Workspace workspace) : base(name, source, workspace) { }

    public double RealParamValue
    {
        get => source.RealParams[Name];
        set => source.RealParams[Name] = value;
    }

    private ValueSliderSettings _realParamSlider;
    public ValueSliderSettings RealParamSlider => _realParamSlider ??= new()
    {
        Label = Name,
        DefaultValue = source.RealParamDefaults[Name],
        Increment = 0.001,
        AnimationPath = $"{source.ParamPathPrefix}.RealParams.[{Name}]",
        ValueWillChange = workspace.TakeSnapshot,
        ValueChanged = (_) => InvalidateRenderer()
    };

}
