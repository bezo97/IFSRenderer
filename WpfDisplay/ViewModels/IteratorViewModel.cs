using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
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
        public static double BaseSize = 100;
        public event EventHandler ViewChanged;
        public event EventHandler Selected;
        public event EventHandler<bool> ConnectEvent;
        public ObservableCollection<ConnectionViewModel> ConnectionViewModels { get; set; } = new ObservableCollection<ConnectionViewModel>();
        public IEnumerable<InstanceProperty> Variables { get; }


        public IteratorViewModel(Iterator iterator)
        {
            this.iterator = iterator;

            Variables = iterator.Transform.GetType().GetProperties()
                    //.Where(prop => Attribute.IsDefined(prop, typeof(FunctionVariable)))
                    .Where(prop => prop.PropertyType == typeof(double))
                    .Select(pi => new InstanceProperty(iterator.Transform, pi)).ToList();

            Variables.ToList().ForEach(v => v.PropertyChanged += (s, e) =>
            {
                RaisePropertyChanged();
                RaisePropertyChanged("InvalidateParams");
            });

        }

        public void Redraw()
        {
            RaisePropertyChanged(() => BaseWeight);
            RaisePropertyChanged(() => WeightedSize);
            RaisePropertyChanged(() => RenderTranslateValue);
        }

        private bool isselected;
        public bool IsSelected
        {
            get => isselected;
            set
            {
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
            set
            {
                Set(ref iterator.Opacity, value);
                RaisePropertyChanged(() => OpacityColor);
                RaisePropertyChanged("InvalidateParams");
            }
        }

        public float ColorIndex
        {
            get => (float)iterator.ColorIndex;
            set
            {
                Set(ref iterator.ColorIndex, value);
                RaisePropertyChanged("InvalidateParams");
            }
        }

        public float ColorSpeed
        {
            get => (float)iterator.ColorSpeed;
            set
            {
                Set(ref iterator.ColorSpeed, value);
                RaisePropertyChanged("InvalidateParams");
            }
        }

        public bool DeltaColoring
        {
            get => iterator.ShadingMode == ShadingMode.DeltaPSpeed;
            set
            {
                Set(ref iterator.ShadingMode, value ? ShadingMode.DeltaPSpeed : ShadingMode.Default);
                RaisePropertyChanged("InvalidateParams");
            }
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
            set
            {
                iterator.BaseWeight = value;
                //ifsvm.ifs.NormalizeBaseWeights();
                //ifsvm.HandleConnectionsChanged(this);
                ViewChanged?.Invoke(this, null);//refresh
                RaisePropertyChanged("InvalidateParams");
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

        public double RenderTranslateValue => -0.5 * WeightedSize;

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

        //private RelayCommand _startConnectingCommand;
        //public RelayCommand StartConnectingCommand
        //{
        //    get => _startConnectingCommand ?? (_startConnectingCommand = new RelayCommand(StartConnecting));
        //}
        public void StartConnecting()
        {
            //
            ConnectEvent?.Invoke(this, false);
        }

        //private RelayCommand _finishConnectingCommand;
        //public RelayCommand FinishConnectingCommand
        //{
        //    get => _finishConnectingCommand ?? (_finishConnectingCommand = new RelayCommand(FinishConnecting));
        //}
        public void FinishConnecting()
        {
            //
            RaisePropertyChanged("InvalidateParams");
            ConnectEvent?.Invoke(this, true);
        }

    }
}
