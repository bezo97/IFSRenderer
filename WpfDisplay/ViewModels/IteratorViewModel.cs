using GalaSoft.MvvmLight;
using IFSEngine.Model;
using IFSEngine.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

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

        public List<VariableViewModel> Variables { get; }


        public IteratorViewModel(Iterator iterator)
        {
            this.iterator = iterator;

            Variables = new List<VariableViewModel>(iterator.TransformVariables.Select(v=>new VariableViewModel(v.Key, iterator)));
            foreach (var v in Variables)
                v.PropertyChanged += (s, e) => RaisePropertyChanged(e.PropertyName);

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
                iterator.Opacity = value;
                RaisePropertyChanged(() => Opacity);
                RaisePropertyChanged(() => OpacityColor);
                RaisePropertyChanged("InvalidateParams");
            }
        }

        public float ColorIndex
        {
            get => (float)iterator.ColorIndex;
            set
            {
                iterator.ColorIndex = value;
                RaisePropertyChanged(() => ColorIndex);
                RaisePropertyChanged("InvalidateParams");
            }
        }

        public float ColorSpeed
        {
            get => (float)iterator.ColorSpeed;
            set
            {
                iterator.ColorSpeed = value;
                RaisePropertyChanged(() => ColorSpeed);
                RaisePropertyChanged("InvalidateParams");
            }
        }

        public bool DeltaColoring
        {
            get => iterator.ShadingMode == ShadingMode.DeltaPSpeed;
            set
            {
                iterator.ShadingMode = value ? ShadingMode.DeltaPSpeed : ShadingMode.Default;
                RaisePropertyChanged(() => DeltaColoring);
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

        public string TransformName
        {
            get => iterator.TransformFunction.Name;
        }

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
