using IFSEngine.Model;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class ToneMappingViewModel
{
    private readonly Workspace _workspace;

    public ToneMappingViewModel(Workspace workspace)
    {
        _workspace = workspace;
        _workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
    }

    private ValueSliderViewModel _brightness;
    public ValueSliderViewModel Brightness => _brightness ??= new ValueSliderViewModel(_workspace)
    {
        Label = "💡 Brightness",
        DefaultValue = IFS.Default.Brightness,
        GetV = () => _workspace.Ifs.Brightness,
        SetV = (value) =>
        {
            _workspace.Ifs.Brightness = value;
            _workspace.Renderer.InvalidateDisplay();
        },
        MinValue = 0,
        Increment = 0.05,
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _gamma;
    public ValueSliderViewModel Gamma => _gamma ??= new ValueSliderViewModel(_workspace)
    {
        Label = "◑ Gamma",
        DefaultValue = IFS.Default.Gamma,
        GetV = () => _workspace.Ifs.Gamma,
        SetV = (value) =>
        {
            _workspace.Ifs.Gamma = value;
            _workspace.Renderer.InvalidateDisplay();
        },
        MinValue = 0,
        Increment = 0.005,
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _gammaThreshold;
    public ValueSliderViewModel GammaThreshold => _gammaThreshold ??= new ValueSliderViewModel(_workspace)
    {
        Label = "◓ Gamma threshold",
        DefaultValue = IFS.Default.GammaThreshold,
        GetV = () => _workspace.Ifs.GammaThreshold,
        SetV = (value) =>
        {
            _workspace.Ifs.GammaThreshold = value;
            _workspace.Renderer.InvalidateDisplay();
        },
        MinValue = 0,
        Increment = 0.0001,
        ValueWillChange = _workspace.TakeSnapshot,
    };

    private ValueSliderViewModel _vibrancy;
    public ValueSliderViewModel Vibrancy => _vibrancy ??= new ValueSliderViewModel(_workspace)
    {
        Label = "🌷 Vibrancy",
        DefaultValue = IFS.Default.Vibrancy,
        GetV = () => _workspace.Ifs.Vibrancy,
        SetV = (value) =>
        {
            _workspace.Ifs.Vibrancy = value;
            _workspace.Renderer.InvalidateDisplay();
        },
        Increment = 0.05,
        ValueWillChange = _workspace.TakeSnapshot,
    };

    [ICommand]
    private void TakeSnapshot() => _workspace.TakeSnapshot();
}
