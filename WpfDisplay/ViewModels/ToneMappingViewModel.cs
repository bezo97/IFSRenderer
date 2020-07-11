using GalaSoft.MvvmLight;
using IFSEngine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDisplay.ViewModels
{
    public class ToneMappingViewModel : ViewModelBase
    {
        private readonly IFS ifs;

        public ToneMappingViewModel(IFS ifs)
        {
            this.ifs = ifs;
        }

        public double Brightness
        {
            get => ifs.ViewSettings.Brightness;
            set
            {
                ifs.ViewSettings.Brightness = value;
                RaisePropertyChanged();
            }
        }
        public double Gamma
        {
            get => ifs.ViewSettings.Gamma;
            set
            {
                ifs.ViewSettings.Gamma = value;
                RaisePropertyChanged();
            }
        }
        public double GammaThreshold
        {
            get => ifs.ViewSettings.GammaThreshold;
            set
            {
                ifs.ViewSettings.GammaThreshold = value;
                RaisePropertyChanged();
            }
        }
        public double Vibrancy
        {
            get => ifs.ViewSettings.Vibrancy;
            set
            {
                ifs.ViewSettings.Vibrancy = value;
                RaisePropertyChanged();
            }
        }

    }
}
