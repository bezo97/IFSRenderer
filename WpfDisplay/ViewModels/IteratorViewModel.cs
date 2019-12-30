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
        private readonly IFSViewModel ifsvm;
        public readonly Iterator iterator;

        public ObservableCollection<ConnectionViewModel> ConnectionViewModels { get; set; } = new ObservableCollection<ConnectionViewModel>();

        public static double BaseSize = 100;

        public IteratorViewModel(IFSViewModel ifsvm, Iterator iterator)
        {
            this.ifsvm = ifsvm;
            this.iterator = iterator;
        }

        private bool isselected;
        public bool IsSelected { get => isselected; set { Set(ref isselected, value); } }

        public float Opacity
        {
            get => (float)iterator.op;
            set { Set(ref iterator.op, value); RaisePropertyChanged(() => OpacityColor); }
        }

        public float ColorIndex
        {
            get => (float)iterator.ci;
            set { Set(ref iterator.ci, value); }
        }

        public float ColorSpeed
        {
            get => (float)iterator.cs;
            set { Set(ref iterator.cs, value); }
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
            set {
                iterator.baseWeight = value;
                ifsvm.ifs.NormalizeBaseWeights();
                ifsvm.HandleConnectionsChanged(this);
                RaisePropertyChanged(() => BaseWeight);
                RaisePropertyChanged(() => WeightedSize);
            }
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
