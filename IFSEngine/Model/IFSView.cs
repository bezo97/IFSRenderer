using IFSEngine.Model.Camera;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;

namespace IFSEngine.Model
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// PropertyChanged:
    /// 0 - no invalidation needed
    /// 1 - invalidate accumulation
    /// 2 - invalidate params (& accumulation)
    /// </remarks>
    public class IFSView : INotifyPropertyChanged
    {
        private double brightness = 1.0;
        private double gamma = 1.0;
        private double gammaThreshold = 0.0;
        private double vibrancy = 1.0;
        private double fogEffect = 0.0;
        private double dof = 0.0;
        private double focusDistance = 2.0;
        private double focusArea = 0.25;
        private CameraBase camera;
        private Color bgColor = Color.Black;

        public IFSView()
        {
            Camera = new QuatCamera();
        }

        public CameraBase Camera {
            get => camera;
            set
            {
                camera = value;
                camera.OnManipulate += () => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("1"));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("1"));
            }
    }
        //public float Width { get; set; } = 1920;
        //public float Height { get; set; } = 1080; //stays in camera for now because of fov
        public double Brightness
        {
            get => brightness;
            set
            {
                brightness = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("0"));
            }
        }
        public double Gamma
        {
            get => gamma;
            set
            {
                gamma = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("0"));
            }
        }
        public double GammaThreshold
        {
            get => gammaThreshold;
            set
            {
                gammaThreshold = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("0"));
            }
        }
        public double Vibrancy
        {
            get => vibrancy;
            set
            {
                vibrancy = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("0"));
            }
        }
        public double FogEffect
        {
            get => fogEffect;
            set
            {
                fogEffect = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("1"));
            }
        }
        public double Dof
        {
            get => dof;
            set
            {
                dof = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("1"));
            }
        }
        public double FocusDistance
        {
            get => focusDistance;
            set
            {
                focusDistance = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("1"));
            }
        }
        public double FocusArea
        {
            get => focusArea;
            set
            {
                focusArea = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("1"));
            }
        }

        public System.Drawing.Color BackgroundColor
        {
            get => bgColor;
            set
            {
                bgColor = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("0"));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void ResetCamera()
        {
            //HACK: remember resolution
            int w = Camera.RenderWidth;
            int h = Camera.RenderHeight;

            Camera = new QuatCamera();

            Camera.RenderWidth = w;
            Camera.RenderHeight = h;
        }
    }
}
