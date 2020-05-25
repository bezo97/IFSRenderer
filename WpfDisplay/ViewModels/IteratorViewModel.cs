using GalaSoft.MvvmLight;
using IFSEngine.Model;
using IFSEngine.TransformFunctions;
using IFSEngine.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using WpfDisplay.Helper;

namespace WpfDisplay.ViewModels
{
    public class IteratorViewModel : ObservableObject
    {
        public readonly Iterator iterator;

        public event EventHandler ViewChanged;
        public event EventHandler Selected;
        public ObservableCollection<ConnectionViewModel> ConnectionViewModels { get; set; } = new ObservableCollection<ConnectionViewModel>();

        public static double BaseSize = 100;

        public IteratorViewModel(Iterator iterator)
        {
            this.iterator = iterator;

            Variables = iterator.Transform.GetType().GetProperties()                    
                    //.Where(prop => Attribute.IsDefined(prop, typeof(FunctionVariable)))
                    .Where(prop => prop.PropertyType == typeof(double))
                    .Select(pi => new InstanceProperty(iterator.Transform, pi)).ToList();

            Variables.ToList().ForEach(v => v.PropertyChanged += (s, e) => {
                RaisePropertyChanged();
            });
            
        }

        public void Redraw()
        {
            RaisePropertyChanged(() => BaseWeight);
            RaisePropertyChanged(() => WeightedSize);
            RaisePropertyChanged(() => RenderTranslateValue);
        }

        private bool isselected;
        public bool IsSelected { get => isselected; set { 
                Set(ref isselected, value);
            }
        }
        public void SelectNode()
        {
            Selected?.Invoke(this, null);
        }

        public float Opacity
        {
            get => (float)iterator.Opacity;
            set { Set(ref iterator.Opacity, value); RaisePropertyChanged(() => OpacityColor); }
        }

        public float ColorIndex
        {
            get => (float)iterator.ColorIndex;
            set { Set(ref iterator.ColorIndex, value); }
        }

        public float ColorSpeed
        {
            get => (float)iterator.ColorSpeed;
            set { Set(ref iterator.ColorSpeed, value); }
        }

        public bool DeltaColoring
        {
            get => iterator.ShadingMode == ShadingMode.DeltaPSpeed;
            set { Set(ref iterator.ShadingMode, value ? ShadingMode.DeltaPSpeed : ShadingMode.Default); }
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
            get => (float)iterator.BaseWeight;
            set {
                iterator.BaseWeight = value;
                //ifsvm.ifs.NormalizeBaseWeights();
                //ifsvm.HandleConnectionsChanged(this);
                ViewChanged?.Invoke(this, null);//refresh
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

        public double RenderTranslateValue => -0.5*WeightedSize;

        //TODO: remove reference from TransformFunctions project

        public string TransformName
        {
            get => iterator.Transform.GetName();
            //set { Set(ref iterator.Transform.Id, value); }
        }

        //TODO: string TransformName
        //TODO: string IteratorName

        private float xCoord = RandHelper.Next(500);
        public float XCoord
        {
            get => xCoord;
            set { Set(ref xCoord, value); }
        }

        private float yCoord = RandHelper.Next(500);
        public float YCoord
        {
            get => yCoord;
            set { Set(ref yCoord, value); }
        }

        public IEnumerable<InstanceProperty> Variables { get; set; }

    }
}
