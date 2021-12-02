using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    [ObservableObject]
    public partial class ToneMappingViewModel
    {
        private readonly Workspace workspace;

        public ToneMappingViewModel(Workspace workspace)
        {
            this.workspace = workspace;
            workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
        }

        public double Brightness
        {
            get => workspace.Ifs.Brightness;
            set
            {
                workspace.Ifs.Brightness = value;
                OnPropertyChanged(nameof(Brightness));
                workspace.Renderer.InvalidateDisplay();
            }
        }
        public double Gamma
        {
            get => workspace.Ifs.Gamma;
            set
            {
                workspace.Ifs.Gamma = value;
                OnPropertyChanged(nameof(Gamma));
                workspace.Renderer.InvalidateDisplay();
            }
        }
        public double GammaThreshold
        {
            get => workspace.Ifs.GammaThreshold;
            set
            {
                workspace.Ifs.GammaThreshold = value;
                OnPropertyChanged(nameof(GammaThreshold));
                workspace.Renderer.InvalidateDisplay();
            }
        }
        public double Vibrancy
        {
            get => workspace.Ifs.Vibrancy;
            set
            {
                workspace.Ifs.Vibrancy = value;
                OnPropertyChanged(nameof(Vibrancy));
                workspace.Renderer.InvalidateDisplay();
            }
        }

        [ICommand]
        private void TakeSnapshot() => workspace.TakeSnapshot();
    }
}
