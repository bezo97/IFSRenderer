using GalaSoft.MvvmLight;
using IFSEngine.Model;
using IFSEngine.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WpfDisplay.ViewModels
{
    public class IteratorViewModel : ObservableObject
    {
        public readonly Iterator iterator;

        public ObservableCollection<ConnectionViewModel> ConnectionViewModels { get; set; } = new ObservableCollection<ConnectionViewModel>();

        public static double BaseSize = 100;

        public IteratorViewModel(Iterator iterator)
        {
            this.iterator = iterator;
            //ConnectionViewModels = new ObservableCollection<ConnectionViewModel>(iterator.WeightTo.Select(p => new ConnectionViewModel(this, p.)));
        }

        private bool isselected;
        public bool IsSelected { get => isselected; set { Set(ref isselected, value); } }

        public float Opacity
        {
            get => (float)iterator.op;
            set { Set(ref iterator.op, value); RaisePropertyChanged(() => OpacityColor); }
        }

        public Color OpacityColor
        {
            get
            {
                byte o = (byte)(100 + Opacity * 255 * 0.6);
                return Color.FromRgb(o, o, o);//grayscale
            }
        }

        public float BaseWeight
        {
            get => (float)iterator.baseWeight;
            set { Set(ref iterator.baseWeight, value); RaisePropertyChanged(() => WeightedSize); }
        }

        public double WeightedSize
        {
            get
            {
                //if (!EnableWeightedSize)
                //    return BaseSize;
                return (0.5f + Math.Sqrt(BaseWeight < 10 ? BaseWeight : 10)) * BaseSize;
            }
        }

        //TODO: remove reference from TransformFunctions project

        public int TransformId
        {
            get => iterator.Transform.Id;
            //set { Set(ref iterator.Transform.Id, value); }
        }

        //TODO: string TransformName
        //TODO: string IteratorName

        private int xCoord = RandHelper.Next(500);
        public int XCoord
        {
            get => xCoord;
            set { Set(ref xCoord, value); }
        }

        private int yCoord = RandHelper.Next(500);
        public int YCoord
        {
            get => yCoord;
            set { Set(ref yCoord, value); }
        }

    }
}
