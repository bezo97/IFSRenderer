using IFSEngine.Model;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public partial class ToneMappingViewModel : ObservableObject
{
    private readonly Workspace _workspace;

    public ToneMappingViewModel(Workspace workspace)
    {
        _workspace = workspace;
        _workspace.LoadedParamsChanged += (s, e) => OnPropertyChanged(string.Empty);
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
        AnimationPath = nameof(_workspace.Ifs.Brightness),
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
        AnimationPath = nameof(_workspace.Ifs.Gamma),
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
        AnimationPath = nameof(_workspace.Ifs.GammaThreshold),
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
        Increment = 0.005,
        AnimationPath = nameof(_workspace.Ifs.Vibrancy),
        ValueWillChange = _workspace.TakeSnapshot,
    };

    [RelayCommand]
    private void TakeSnapshot() => _workspace.TakeSnapshot();
}
