using OpenTK.Graphics.OpenGL;
using OpenTK.Graphics.Wgl;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Wpf;
using System;
using System.Linq;
using System.Numerics;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using WpfDisplay.Helper;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views
{
    /// <summary>
    /// Interaction logic for RenderDisplay.xaml
    /// </summary>
    public partial class RenderDisplay : UserControl
    {
        private InteractiveDisplayViewModel DisplayViewModel { get; set; }

        public IGraphicsContext GraphicsContext { get; private set; }

        private KeyboardController keyboard;
        //last mouse position
        private double lastY;
        private double lastX;

        private readonly Key[] translateKeys = 
        {
            Key.W, Key.S, Key.D, Key.A, Key.Q, Key.E, Key.C
        };

        private readonly Key[] rotateKeys = 
        {
            Key.I, Key.K, Key.J, Key.L, Key.U, Key.O
        };

        public RenderDisplay()
        {
            InitializeComponent();

            //Loaded += (s, e) =>
            {
                //TODO: util call
                var windowHandle = new WindowInteropHelper(System.Windows.Application.Current.MainWindow).Handle;//Window.GetWindow(this);
                var nws = NativeWindowSettings.Default;
                nws.StartFocused = false;
                nws.StartVisible = false;
                nws.NumberOfSamples = 0;
                nws.APIVersion = new Version(4, 5);
                nws.Flags = ContextFlags.Offscreen | ContextFlags.Default;
                nws.Profile = ContextProfile.Compatability;// Any;//TODO: Core
                nws.WindowBorder = WindowBorder.Hidden;
                nws.WindowState = WindowState.Minimized;
                var glfwWindow = new NativeWindow(nws);
                var provider = new GLFWBindingsContext();
                Wgl.LoadBindings(provider);
                GraphicsContext = glfwWindow.Context;
                GraphicsContext.MakeCurrent();



                display1.Render += display1_OnRender;//actually invoked right after Start()
                display1.Start(new GLWpfControlSettings
                {
                    MajorVersion = 4,
                    MinorVersion = 5,
                    ContextToUse = GraphicsContext,
                    RenderContinuously = false
                });

                
                //display1.Ready += () =>
                //{
                //    //tmp
                //    var fi = typeof(GLWpfControl).GetField("_windowInfo", BindingFlags.NonPublic | BindingFlags.Instance);
                //    var oo = fi.GetValue(display1);
                //    WindowInfo = (IWindowInfo)oo;
                //    GraphicsContext = (GraphicsContext)(typeof(GLWpfControl).GetField("_context", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(display1));
                //};

            };

            //Avoid threading problems
            DataContextChanged += (s, e) => DisplayViewModel = DataContext as InteractiveDisplayViewModel;

            keyboard = new KeyboardController(this.display1);
            keyboard.KeyboardTick += KeydownHandler;
        }

        private void Display1_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            DisplayViewModel.FocusDistance += e.Delta * DisplayViewModel.FocusDistance * 0.001;
            DisplayViewModel.InvalidateAccumulation();
        }

        private void Display1_MouseMove(object sender, MouseEventArgs e)
        {
            var mPos = e.GetPosition(this);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Mouse.OverrideCursor = Cursors.None;
                double yawDelta = mPos.X - lastX;
                double pitchDelta = mPos.Y - lastY;
                DisplayViewModel.RotateCommand(new Vector3((float)yawDelta, (float)pitchDelta, 0.0f));
            }
            else
                Mouse.OverrideCursor = System.Windows.Input.Cursors.Arrow;
            lastX = mPos.X;
            lastY = mPos.Y;
        }

        private void KeydownHandler(object sender, EventArgs e)
        {
            if (translateKeys.Any(k => keyboard.IsKeyDown(k)))
            {
                //Experiment: camera speed relates to focus distance
                float magnitude = (float)DisplayViewModel.FocusDistance;
                var direction = new Vector3(
                    ((keyboard.IsKeyDown(Key.D) ? 1 : 0) - (keyboard.IsKeyDown(Key.A) ? 1 : 0)),
                    ((keyboard.IsKeyDown(Key.E) ? 1 : 0) - ((keyboard.IsKeyDown(Key.C) || keyboard.IsKeyDown(Key.Q)) ? 1 : 0)),
                    ((keyboard.IsKeyDown(Key.W) ? 1 : 0) - (keyboard.IsKeyDown(Key.S) ? 1 : 0))
                );
                DisplayViewModel.TranslateCommand(magnitude * direction);
            }

            if (rotateKeys.Any(k => keyboard.IsKeyDown(k)))
            {
                float magnitude = 3.0f;
                var direction = new Vector3(
                    ((keyboard.IsKeyDown(Key.L) ? 1 : 0) - (keyboard.IsKeyDown(Key.J) ? 1 : 0)),
                    ((keyboard.IsKeyDown(Key.K) ? 1 : 0) - (keyboard.IsKeyDown(Key.I) ? 1 : 0)),
                    ((keyboard.IsKeyDown(Key.U) ? 1 : 0) - (keyboard.IsKeyDown(Key.O) ? 1 : 0))
                );
                DisplayViewModel.RotateCommand(magnitude * direction);
            }

        }
       
        private void display1_OnRender(TimeSpan obj)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, display1.Framebuffer);
            var dd = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            /*GL.ClearColor(Color4.Red);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.LoadIdentity();
            GL.Begin(PrimitiveType.Triangles);
            GL.Color4(1.0f, 0.0f, 0.0f, 1.0f); GL.Vertex2(0.0f, 1.0f);
            GL.Color4(0.0f, 1.0f, 0.0f, 1.0f); GL.Vertex2(0.87f, -0.5f);
            GL.Color4(0.0f, 0.0f, 1.0f, 1.0f); GL.Vertex2(-0.87f, -0.5f);
            GL.End();
            GL.Finish();*/
        }
    }
}
