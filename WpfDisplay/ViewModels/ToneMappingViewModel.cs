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

    public double Brightness
    {
        get => _workspace.Ifs.Brightness;
        set
        {
            _workspace.Ifs.Brightness = value;
            OnPropertyChanged(nameof(Brightness));
            _workspace.Renderer.InvalidateDisplay();
        }
    }
    public double Gamma
    {
        get => _workspace.Ifs.Gamma;
        set
        {
            _workspace.Ifs.Gamma = value;
            OnPropertyChanged(nameof(Gamma));
            _workspace.Renderer.InvalidateDisplay();
        }
    }
    public double GammaThreshold
    {
        get => _workspace.Ifs.GammaThreshold;
        set
        {
            _workspace.Ifs.GammaThreshold = value;
            OnPropertyChanged(nameof(GammaThreshold));
            _workspace.Renderer.InvalidateDisplay();
        }
    }
    public double Vibrancy
    {
        get => _workspace.Ifs.Vibrancy;
        set
        {
            _workspace.Ifs.Vibrancy = value;
            OnPropertyChanged(nameof(Vibrancy));
            _workspace.Renderer.InvalidateDisplay();
        }
    }

    [ICommand]
    private void TakeSnapshot() => _workspace.TakeSnapshot();
}
