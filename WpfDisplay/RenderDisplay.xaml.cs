using IFSEngine;
using IFSEngine.Model;
using OpenTK;
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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfDisplay
{
    /// <summary>
    /// Interaction logic for RenderDisplay.xaml
    /// </summary>
    public partial class RenderDisplay : WindowsFormsHost
    {
        public RendererGL Renderer { get; private set; }

        public int FPS { get; set; }

        private GLControl display1;

        public RenderDisplay()
        {
            InitializeComponent();

            Loaded += (s, e) => {

                Window.GetWindow(this).Closing += (s2, e2) => Renderer.Dispose();
                //vagy this.Unloaded+=

                //Init GL Control
                OpenTK.Toolkit.Init();
                display1 = new OpenTK.GLControl();
                display1.Width = (int)this.Width;
                display1.Height = (int)this.Height;
                display1.Left = 0;
                display1.Top = 0;
                //display1.PreviewKeyDown += KeyDown_Custom;
                display1.MouseMove += Display1_MouseMove;
                display1.MakeCurrent();
                this.Child = display1;

                IFS Params = new IFS();
                Renderer = new RendererGL(Params, display1.Width, display1.Height);//TODO: separate render and view resolutions, make it dynamic

                Renderer.DisplayFrameCompleted += R_DisplayFrameCompleted;

            };

        }

        Stopwatch fpsCounter = new Stopwatch();
        private void R_DisplayFrameCompleted(object sender, EventArgs e)
        {
            display1.MakeCurrent();//render thread gets the context
            display1.SwapBuffers();
            fpsCounter.Stop();
            FPS = (int)(fpsCounter.ElapsedMilliseconds > 0 ? 1000 / (fpsCounter.ElapsedMilliseconds) : 0);
            fpsCounter.Restart();
            //TODO: update fps view: NotifyPropertyChanged mashol, vagy itt Dispatcher.Invoke(()=>{ ... });
        }

        float lastX;
        float lastY;
        private void Display1_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Renderer.Camera.ProcessMouseMovement((e.X - lastX), (lastY - e.Y));
            }
            lastX = e.X;
            lastY = e.Y;
        }

        private void WindowsFormsHost_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            //TODO: replace these with gui
            if (Keyboard.IsKeyDown(Key.R))
                Renderer.Reset();
            if (Keyboard.IsKeyDown(Key.Space))
                Renderer.StartRendering();

            if (Keyboard.IsKeyDown(Key.W) ||
               Keyboard.IsKeyDown(Key.S) ||
               Keyboard.IsKeyDown(Key.D) ||
               Keyboard.IsKeyDown(Key.A) ||
               Keyboard.IsKeyDown(Key.Q) ||
               Keyboard.IsKeyDown(Key.E) ||
               Keyboard.IsKeyDown(Key.C))
            {
                var translateVector = new Vector3(
                    0.02f * ((Keyboard.IsKeyDown(Key.W) ? 1 : 0) - (Keyboard.IsKeyDown(Key.S) ? 1 : 0)),
                    0.02f * ((Keyboard.IsKeyDown(Key.D) ? 1 : 0) - (Keyboard.IsKeyDown(Key.A) ? 1 : 0)),
                    0.02f * ((Keyboard.IsKeyDown(Key.E) ? 1 : 0) - ((Keyboard.IsKeyDown(Key.C) || Keyboard.IsKeyDown(Key.Q)) ? 1 : 0))
                );
                Renderer.Camera.Translate(translateVector);
            }
        }

    }
}
