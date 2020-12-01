using Microsoft.Toolkit.Mvvm.ComponentModel;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    public class ToneMappingViewModel : ObservableObject
    {
        private readonly Workspace workspace;

        public ToneMappingViewModel(Workspace workspace)
        {
            this.workspace = workspace;
            workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
        }

        public double Brightness
        {
            get => workspace.IFS.Brightness;
            set
            {
                workspace.IFS.Brightness = value;
                OnPropertyChanged(nameof(Brightness));
                workspace.Renderer.UpdateDisplay();
            }
        }
        public double Gamma
        {
            get => workspace.IFS.Gamma;
            set
            {
                workspace.IFS.Gamma = value;
                OnPropertyChanged(nameof(Gamma));
                workspace.Renderer.UpdateDisplay();
            }
        }
        public double GammaThreshold
        {
            get => workspace.IFS.GammaThreshold;
            set
            {
                workspace.IFS.GammaThreshold = value;
                OnPropertyChanged(nameof(GammaThreshold));
                workspace.Renderer.UpdateDisplay();
            }
        }
        public double Vibrancy
        {
            get => workspace.IFS.Vibrancy;
            set
            {
                workspace.IFS.Vibrancy = value;
                OnPropertyChanged(nameof(Vibrancy));
                workspace.Renderer.UpdateDisplay();
            }
        }

    }
}
