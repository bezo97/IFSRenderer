using IFSEngine.Model;
using IFSEngine.Utility;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    public class IteratorViewModel : ObservableObject
    {
        public readonly Iterator iterator;
        private readonly Workspace workspace;

        public static double BaseSize = 100;
        public event EventHandler ViewChanged;
        public event EventHandler<bool> ConnectEvent;

        public List<IParamViewModel> Parameters { get; } = new List<IParamViewModel>();

        public IteratorViewModel(Iterator iterator, Workspace workspace)
        {
            this.iterator = iterator;
            this.workspace = workspace;
            workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
            ReloadParameters();
        }

        public void ReloadParameters()
        {
            Parameters.Clear();
            Parameters.AddRange(iterator.RealParams.Select(v => new RealParamViewModel(v.Key, iterator, workspace)));
            Parameters.AddRange(iterator.Vec3Params.Select(v => new Vec3ParamViewModel(v.Key, iterator, workspace)));
            foreach (var v in Parameters)
            {
                v.PropertyChanged += (s, e) =>
                {
                    OnPropertyChanged(e.PropertyName);
                    workspace.Renderer.InvalidateParamsBuffer();
                };
            }
        }

        public RelayCommand RemoveCommand { get; set; }
        public RelayCommand DuplicateCommand { get; set; }

        public void Redraw()
        {
            OnPropertyChanged(string.Empty);
            OnPropertyChanged("NodePosition");
        }

        private bool isselected;
        public bool IsSelected
        {
            get => isselected;
            set
            {
                SetProperty(ref isselected, value);
            }
        }

        public float StartWeight
        {
            get => (float)iterator.StartWeight;
            set
            {
                iterator.StartWeight = value;
                OnPropertyChanged(nameof(StartWeight));
                workspace.Renderer.InvalidateParamsBuffer();
            }
        }

        public float Opacity
        {
            get => (float)iterator.Opacity;
            set
            {
                iterator.Opacity = value;
                OnPropertyChanged(nameof(Opacity));
                OnPropertyChanged(nameof(OpacityColor));
                workspace.Renderer.InvalidateParamsBuffer();
            }
        }

        public float ColorIndex
        {
            get => (float)iterator.ColorIndex;
            set
            {
                iterator.ColorIndex = value;
                OnPropertyChanged(nameof(ColorIndex));
                OnPropertyChanged(nameof(ColorRGB));
                workspace.Renderer.InvalidateParamsBuffer();
            }
        }

        public float ColorSpeed
        {
            get => (float)iterator.ColorSpeed;
            set
            {
                iterator.ColorSpeed = value;
                OnPropertyChanged(nameof(ColorSpeed));
                workspace.Renderer.InvalidateParamsBuffer();
            }
        }

        public Color ColorRGB
        {
            get
            {
                var colors = workspace.IFS.Palette.Colors;
                var c = colors[(int)(colors.Count * (ColorIndex - Math.Floor(ColorIndex)))];
                return Color.FromRgb((byte)(255 * c.X), (byte)(255 * c.Y), (byte)(255 * c.Z));
            }
        }

        public float Mix
        {
            get => (float)iterator.Mix;
            set
            {
                iterator.Mix = value;
                OnPropertyChanged(nameof(Mix));
                workspace.Renderer.InvalidateParamsBuffer();
            }
        }

        public float Add
        {
            get => (float)iterator.Add;
            set
            {
                iterator.Add = value;
                OnPropertyChanged(nameof(Add));
                workspace.Renderer.InvalidateParamsBuffer();
            }
        }

        public bool DeltaColoring
        {
            get => iterator.ShadingMode == ShadingMode.DeltaPSpeed;
            set
            {
                iterator.ShadingMode = value ? ShadingMode.DeltaPSpeed : ShadingMode.Default;
                OnPropertyChanged(nameof(DeltaColoring));
                workspace.Renderer.InvalidateParamsBuffer();
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
                workspace.Renderer.InvalidateParamsBuffer();
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
            get => iterator.Transform.Name;
        }

        //TODO: string IteratorName

        private float xCoord = RandHelper.Next(500);
        public float XCoord
        {
            get => xCoord;
            private set { SetProperty(ref xCoord, value); }
        }

        private float yCoord = RandHelper.Next(500);
        public float YCoord
        {
            get => yCoord;
            private set { SetProperty(ref yCoord, value); }
        }

        public void UpdatePosition(float x, float y)
        {
            XCoord = x;
            YCoord = y;
            OnPropertyChanged("NodePosition");
            ViewChanged?.Invoke(this, null);//refresh
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
            ConnectEvent?.Invoke(this, true);
            workspace.Renderer.InvalidateParamsBuffer();
            ViewChanged?.Invoke(this, null);//refresh
        }

        private RelayCommand _takeSnapshotCommand;
        public RelayCommand TakeSnapshotCommand =>
            _takeSnapshotCommand ??= new RelayCommand(workspace.TakeSnapshot);

        private RelayCommand flipOpacityCommand;
        public ICommand FlipOpacityCommand => flipOpacityCommand ??= new RelayCommand(FlipOpacity);
        private void FlipOpacity()
        {
            workspace.TakeSnapshot();
            if (Opacity > 0.0f)
                Opacity = 0.0f;
            else
                Opacity = 1.0f;

        }

        private RelayCommand flipWeightCommand;
        public ICommand FlipWeightCommand => flipWeightCommand ??= new RelayCommand(FlipWeight);

        private void FlipWeight()
        {
            workspace.TakeSnapshot();
            if (BaseWeight > 0.0f)
                BaseWeight = 0.0f;
            else
                BaseWeight = 1.0f;
        }
    }
}
