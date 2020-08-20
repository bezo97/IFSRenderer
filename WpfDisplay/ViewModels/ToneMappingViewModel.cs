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
            get => ifs.Brightness;
            set
            {
                ifs.Brightness = value;
                RaisePropertyChanged();
                RaisePropertyChanged("InvalidateRender");
            }
        }
        public double Gamma
        {
            get => ifs.Gamma;
            set
            {
                ifs.Gamma = value;
                RaisePropertyChanged();
                RaisePropertyChanged("InvalidateRender");
            }
        }
        public double GammaThreshold
        {
            get => ifs.GammaThreshold;
            set
            {
                ifs.GammaThreshold = value;
                RaisePropertyChanged();
                RaisePropertyChanged("InvalidateRender");
            }
        }
        public double Vibrancy
        {
            get => ifs.Vibrancy;
            set
            {
                ifs.Vibrancy = value;
                RaisePropertyChanged();
                RaisePropertyChanged("InvalidateRender");
            }
        }

    }
}
