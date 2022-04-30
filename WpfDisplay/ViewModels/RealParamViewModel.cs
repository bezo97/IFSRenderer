using IFSEngine.Model;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class RealParamViewModel : ParamViewModelBase<double>
{
    public RealParamViewModel(string name, Iterator iterator, Workspace workspace) : base(name, iterator, workspace) { }

    private ValueSliderViewModel _sliderViewModel;
    public ValueSliderViewModel SliderViewModel => _sliderViewModel ??= new ValueSliderViewModel(workspace)
    {
        Label = Name,
        DefaultValue = iterator.Transform.RealParams[Name],
        GetV = () => iterator.RealParams[Name],
        SetV = (value) => {
            iterator.RealParams[Name] = value;
            workspace.Renderer.InvalidateParamsBuffer();
        },
        Increment = 0.001,
        AnimationPath = $"[{iterator.Id}].RealParams.[{Name}]",
        ValueWillChange = workspace.TakeSnapshot,
    };

}
