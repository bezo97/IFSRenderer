using OpenTK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using WpfDisplay.Helper;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views
{
    /// <summary>
    /// Interaction logic for RenderDisplay.xaml
    /// </summary>
    public partial class RenderDisplay : WindowsFormsHost
    {
        //TODO: Viewmodel
        public GLControl display1;
        public MainViewModel MainViewModel;
        private KeyboardController kbc;

        public RenderDisplay()
        {
            InitializeComponent();
            
            Loaded += me_Loaded;
            kbc = new KeyboardController(this);
            kbc.KeyboardTick += KeydownHandler;
        }

        private void me_Loaded(object sender, RoutedEventArgs e)
        {
            //Init GL Control
            display1 = new OpenTK.GLControl();
            display1.VSync = false;
            var displayedResolution = GetElementPixelSize(this);
            display1.Width = (int)displayedResolution.Width;
            display1.Height = (int)displayedResolution.Height;
            display1.Left = 0;
            display1.Top = 0;
            //display1.PreviewKeyDown += KeyDown_Custom;
            display1.MouseMove += Display1_MouseMove;
            display1.MouseWheel += Display1_MouseWheel;
            this.Child = display1;

            display1.MakeCurrent();
        }

        private void Display1_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            MainViewModel.IFSViewModel.ifs.ViewSettings.FocusDistance += e.Delta * MainViewModel.IFSViewModel.ifs.ViewSettings.FocusDistance * 0.001;
        }

        public Size GetElementPixelSize(UIElement element)
        {
            Matrix transformToDevice;
            var source = PresentationSource.FromVisual(element);
            if (source != null)
                transformToDevice = source.CompositionTarget.TransformToDevice;
            else
                using (var src = new HwndSource(new HwndSourceParameters()))
                    transformToDevice = src.CompositionTarget.TransformToDevice;

            if (element.DesiredSize == new Size())
                element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            return (Size)transformToDevice.Transform((Vector)element.DesiredSize);
        }

        float lastX;
        float lastY;
        private void Display1_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Mouse.OverrideCursor = System.Windows.Input.Cursors.None;
                MainViewModel.IFSViewModel.ifs.ViewSettings.Camera.ProcessMouseMovement((e.X - lastX), (lastY - e.Y));
            }
            else
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            lastX = e.X;
            lastY = e.Y;
        }

        private void KeydownHandler(object sender, EventArgs e)
        {
            if (kbc.IsKeyDown(Key.R))
                MainViewModel.LoadRandomCommand.Execute(null);
            if (kbc.IsKeyDown(Key.Space))
                MainViewModel.QualitySettingsViewModel.StartRenderingCommand.Execute(null);
            if (
               //translate
               kbc.IsKeyDown(Key.W) ||
               kbc.IsKeyDown(Key.S) ||
               kbc.IsKeyDown(Key.D) ||
               kbc.IsKeyDown(Key.A) ||
               kbc.IsKeyDown(Key.Q) ||
               kbc.IsKeyDown(Key.E) ||
               kbc.IsKeyDown(Key.C) ||
               //rotate
               kbc.IsKeyDown(Key.I) ||
               kbc.IsKeyDown(Key.K) ||
               kbc.IsKeyDown(Key.J) ||
               kbc.IsKeyDown(Key.L) ||
               kbc.IsKeyDown(Key.U) ||
               kbc.IsKeyDown(Key.O))
            {
                var translateVector = new System.Numerics.Vector3(
                    (float)MainViewModel.IFSViewModel.ifs.ViewSettings.FocusDistance * 0.005f * ((kbc.IsKeyDown(Key.W) ? 1 : 0) - (kbc.IsKeyDown(Key.S) ? 1 : 0)),
                    (float)MainViewModel.IFSViewModel.ifs.ViewSettings.FocusDistance * 0.005f * ((kbc.IsKeyDown(Key.D) ? 1 : 0) - (kbc.IsKeyDown(Key.A) ? 1 : 0)),
                    (float)MainViewModel.IFSViewModel.ifs.ViewSettings.FocusDistance * 0.005f * ((kbc.IsKeyDown(Key.E) ? 1 : 0) - ((kbc.IsKeyDown(Key.C) || kbc.IsKeyDown(Key.Q)) ? 1 : 0))
                );
                MainViewModel.IFSViewModel.ifs.ViewSettings.Camera.Translate(translateVector);

                float pitchd = 0.05f * ((kbc.IsKeyDown(Key.I) ? 1 : 0) - (kbc.IsKeyDown(Key.K) ? 1 : 0));
                float yawd = 0.05f * ((kbc.IsKeyDown(Key.J) ? 1 : 0) - (kbc.IsKeyDown(Key.L) ? 1 : 0));
                float rolld = 0.05f * ((kbc.IsKeyDown(Key.O) ? 1 : 0) - (kbc.IsKeyDown(Key.U) ? 1 : 0));
                (MainViewModel.IFSViewModel.ifs.ViewSettings.Camera as IFSEngine.Model.Camera.QuatCamera)?.RotateBy(yawd,pitchd,rolld);//HACK: review camera interface

                MainViewModel.IFSViewModel.ifs.ViewSettings.Camera.UpdateCamera();
            }
        }

    }
}
