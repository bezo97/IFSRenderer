using IFSEngine;
using IFSEngine.Model;
using OpenTK;
using OpenTK.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WpfDisplay.Helper;

namespace WpfDisplay.Controls
{
    /// <summary>
    /// Interaction logic for RenderDisplay.xaml
    /// </summary>
    public partial class RenderDisplay : WindowsFormsHost
    {
        public RendererGL Renderer { get; private set; }

        private GLControl display1;
        private KeyboardController kbc;

        IGraphicsContext ctx;

        public RenderDisplay()
        {
            InitializeComponent();
            
            Loaded += me_Loaded;
            kbc = new KeyboardController(this);
            kbc.KeyboardTick += KeydownHandler;
            //me_Loaded();

        }

        private void me_Loaded(object sender, RoutedEventArgs e)
        {            
            //Init GL Control
            OpenTK.Toolkit.Init();
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
            ctx = new GraphicsContext(GraphicsMode.Default, display1.WindowInfo);
            Renderer = new RendererGL(ctx, display1.WindowInfo);
            Renderer.SetDisplayResolution(display1.Width, display1.Height);

            Renderer.DisplayFrameCompleted += R_DisplayFrameCompleted;
        }

        private void Display1_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            Renderer.CurrentParams.ViewSettings.FocusDistance += e.Delta * Renderer.CurrentParams.ViewSettings.FocusDistance * 0.001;
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
        
        private void R_DisplayFrameCompleted(object sender, EventArgs e)
        {
            //ctx.MakeCurrent(display1.WindowInfo);
            ctx.SwapBuffers();
        }

        float lastX;
        float lastY;
        private void Display1_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Renderer.CurrentParams.ViewSettings.Camera.ProcessMouseMovement((e.X - lastX), (lastY - e.Y));
            }
            lastX = e.X;
            lastY = e.Y;
        }

        private void KeydownHandler(object sender, EventArgs e)
        {
            if (kbc.IsKeyDown(Key.R))
                Renderer.LoadParams(new IFS(true));
            if (kbc.IsKeyDown(Key.Space))
                Renderer.StartRendering();

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
                    (float)Renderer.CurrentParams.ViewSettings.FocusDistance * 0.01f * ((kbc.IsKeyDown(Key.W) ? 1 : 0) - (kbc.IsKeyDown(Key.S) ? 1 : 0)),
                    (float)Renderer.CurrentParams.ViewSettings.FocusDistance * 0.01f * ((kbc.IsKeyDown(Key.D) ? 1 : 0) - (kbc.IsKeyDown(Key.A) ? 1 : 0)),
                    (float)Renderer.CurrentParams.ViewSettings.FocusDistance * 0.01f * ((kbc.IsKeyDown(Key.E) ? 1 : 0) - ((kbc.IsKeyDown(Key.C) || kbc.IsKeyDown(Key.Q)) ? 1 : 0))
                );
                Renderer.CurrentParams.ViewSettings.Camera.Translate(translateVector);

                float pitchd = 0.05f * ((kbc.IsKeyDown(Key.I) ? 1 : 0) - (kbc.IsKeyDown(Key.K) ? 1 : 0));
                float yawd = 0.05f * ((kbc.IsKeyDown(Key.J) ? 1 : 0) - (kbc.IsKeyDown(Key.L) ? 1 : 0));
                float rolld = 0.05f * ((kbc.IsKeyDown(Key.O) ? 1 : 0) - (kbc.IsKeyDown(Key.U) ? 1 : 0));
                (Renderer.CurrentParams.ViewSettings.Camera as IFSEngine.Model.Camera.QuatCamera)?.RotateBy(yawd,pitchd,rolld);//HACK: Camera api tervezni (baseclass / interfacek / ..)

                Renderer.CurrentParams.ViewSettings.Camera.UpdateCamera();//szinten a Mutate-es sztori..
            }
        }

    }
}
