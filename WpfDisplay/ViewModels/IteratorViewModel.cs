using IFSEngine.Model;
using IFSEngine.Utility;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Media;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels
{
    [ObservableObject]
    public partial class IteratorViewModel
    {
        public readonly Iterator iterator;
        private readonly Workspace workspace;

        public static double BaseSize = 100;
        public event EventHandler ViewChanged;
        public event EventHandler<bool> ConnectEvent;

        public List<INotifyPropertyChanged> Parameters { get; } = new();

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

        public IRelayCommand RemoveCommand { get; set; }
        public IRelayCommand DuplicateCommand { get; set; }

        public void Redraw()
        {
            OnPropertyChanged(string.Empty);
            OnPropertyChanged("NodePosition");
        }

        [ObservableProperty] private bool _isSelected;

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

        public string TransformName => iterator.Transform.Name;

        //TODO: string IteratorName

        [ObservableProperty] private float _xCoord = RandHelper.Next(500);

        [ObservableProperty] private float _yCoord = RandHelper.Next(500);

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
        public void StartConnecting() => ConnectEvent?.Invoke(this, false);

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

        [ICommand]
        private void TakeSnapshot() => workspace.TakeSnapshot();

        [ICommand]
        private void FlipOpacity()
        {
            workspace.TakeSnapshot();
            if (Opacity > 0.0f)
                Opacity = 0.0f;
            else
                Opacity = 1.0f;

        }

        [ICommand]
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
