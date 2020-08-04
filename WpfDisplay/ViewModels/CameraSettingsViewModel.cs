using GalaSoft.MvvmLight;
using IFSEngine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDisplay.ViewModels
{
    public class CameraSettingsViewModel : ViewModelBase
    {
        private readonly IFS ifs;

        public CameraSettingsViewModel(IFS ifs)
        {
            this.ifs = ifs;
        }

        public float FieldOfView {
            get => ifs.ViewSettings.Camera.FieldOfView;
            set
            {
                ifs.ViewSettings.Camera.FieldOfView = value;
                RaisePropertyChanged();
            }
        }

        public double DepthOfField
        {
            get => ifs.ViewSettings.Dof;
            set
            {
                ifs.ViewSettings.Dof = value;
                RaisePropertyChanged();
            }
        }

        public double FocusDistance
        {
            get => ifs.ViewSettings.FocusDistance;
            set
            {
                ifs.ViewSettings.FocusDistance = value;
                RaisePropertyChanged();
            }
        }

        public double FocusArea
        {
            get => ifs.ViewSettings.FocusArea;
            set
            {
                ifs.ViewSettings.FocusArea = value;
                RaisePropertyChanged();
            }
        }

        public double FogEffect
        {
            get => ifs.ViewSettings.FogEffect;
            set
            {
                ifs.ViewSettings.FogEffect = value;
                RaisePropertyChanged();
            }
        }

    }
}
