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
            get => ifs.Camera.FieldOfView;
            set
            {
                ifs.Camera.FieldOfView = value;
                RaisePropertyChanged();
                RaisePropertyChanged("InvalidateAccumulation");
            }
        }

        public double DepthOfField
        {
            get => ifs.DepthOfField;
            set
            {
                ifs.DepthOfField = value;
                RaisePropertyChanged();
                RaisePropertyChanged("InvalidateAccumulation");
            }
        }

        public double FocusDistance
        {
            get => ifs.FocusDistance;
            set
            {
                ifs.FocusDistance = value;
                RaisePropertyChanged();
                RaisePropertyChanged("InvalidateAccumulation");
            }
        }

        public double FocusArea
        {
            get => ifs.FocusArea;
            set
            {
                ifs.FocusArea = value;
                RaisePropertyChanged();
                RaisePropertyChanged("InvalidateAccumulation");
            }
        }

        public double FogEffect
        {
            get => ifs.FogEffect;
            set
            {
                ifs.FogEffect = value;
                RaisePropertyChanged();
                RaisePropertyChanged("InvalidateAccumulation");
            }
        }

    }
}
