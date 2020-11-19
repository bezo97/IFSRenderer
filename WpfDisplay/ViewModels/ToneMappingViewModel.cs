using GalaSoft.MvvmLight;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    public class ToneMappingViewModel : ViewModelBase
    {
        private readonly Workspace workspace;

        public ToneMappingViewModel(Workspace workspace)
        {
            this.workspace = workspace;
            workspace.PropertyChanged += (s, e) => RaisePropertyChanged(string.Empty);
        }

        public double Brightness
        {
            get => workspace.IFS.Brightness;
            set
            {
                workspace.IFS.Brightness = value;
                RaisePropertyChanged(() => Brightness);
                workspace.Renderer.UpdateDisplay();
            }
        }
        public double Gamma
        {
            get => workspace.IFS.Gamma;
            set
            {
                workspace.IFS.Gamma = value;
                RaisePropertyChanged(() => Gamma);
                workspace.Renderer.UpdateDisplay();
            }
        }
        public double GammaThreshold
        {
            get => workspace.IFS.GammaThreshold;
            set
            {
                workspace.IFS.GammaThreshold = value;
                RaisePropertyChanged(() => GammaThreshold);
                workspace.Renderer.UpdateDisplay();
            }
        }
        public double Vibrancy
        {
            get => workspace.IFS.Vibrancy;
            set
            {
                workspace.IFS.Vibrancy = value;
                RaisePropertyChanged(() => Vibrancy);
                workspace.Renderer.UpdateDisplay();
            }
        }

    }
}
