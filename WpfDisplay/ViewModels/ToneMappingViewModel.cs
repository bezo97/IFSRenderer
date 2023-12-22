using CommunityToolkit.Mvvm.ComponentModel;

using IFSEngine.Model;

using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public partial class ToneMappingViewModel : ObservableObject
{
    private readonly Workspace _workspace;

    public ToneMappingViewModel(Workspace workspace)
    {
        _workspace = workspace;
        _workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
    }

    public IFS Ifs => _workspace.Ifs;

    private ValueSliderSettings _brightness;
    public ValueSliderSettings BrightnessSlider => _brightness ??= new()
    {
        Label = "💡 Brightness",
        DefaultValue = IFS.Default.Brightness,
        MinValue = 0,
        Increment = 0.05,
        AnimationPath = nameof(_workspace.Ifs.Brightness),
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (v) => _workspace.Renderer.InvalidateDisplay()
    };

    private ValueSliderSettings _gamma;
    public ValueSliderSettings GammaSettings => _gamma ??= new()
    {
        Label = "◑ Gamma",
        DefaultValue = IFS.Default.Gamma,
        MinValue = 0,
        Increment = 0.005,
        AnimationPath = nameof(_workspace.Ifs.Gamma),
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (v) => _workspace.Renderer.InvalidateDisplay()
    };

    private ValueSliderSettings _gammaThreshold;
    public ValueSliderSettings GammaThresholdSettings => _gammaThreshold ??= new()
    {
        Label = "◓ Gamma threshold",
        DefaultValue = IFS.Default.GammaThreshold,
        MinValue = 0,
        Increment = 0.0001,
        AnimationPath = nameof(_workspace.Ifs.GammaThreshold),
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (v) => _workspace.Renderer.InvalidateDisplay()
    };

    private ValueSliderSettings _vibrancy;
    public ValueSliderSettings VibrancySettings => _vibrancy ??= new()
    {
        Label = "🌷 Vibrancy",
        DefaultValue = IFS.Default.Vibrancy,
        Increment = 0.005,
        AnimationPath = nameof(_workspace.Ifs.Vibrancy),
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (v) => _workspace.Renderer.InvalidateDisplay()
    };
}
