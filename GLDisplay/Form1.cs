using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

//using GLDisplay.Leap;
using IFSEngine;
using IFSEngine.Model;
using OpenTK;


namespace GLDisplay
{
    public partial class Form1 : Form
    {
        public static bool IsKeyDown(params Keys[] keys)
        {
            return keys.Any(key => (GetKeyState(Convert.ToInt16(key)) & 0x80) == 0x80);
        }

        [DllImport("user32.dll")]
        public extern static Int16 GetKeyState(Int16 nVirtKey);

        IFSEngine.RendererGL renderer;

        OpenTK.GLControl display1;
        //OpenGL.GlControl display1;

        //Leap.Leap leap;

        const int w = 1280;
        const int h = 720;

        void UpdateIteratorSelectedText()
        {
            //IteratorSelectLabel.Text = $"< ({IteratorManipulator.EditState + 1}) / {IteratorManipulator.IteratorCount} >";
        }


        public Form1()
        {
            InitializeComponent();
            this.Width = w;
            this.Height = h;
            OpenTK.Toolkit.Init();//
            display1 = new OpenTK.GLControl();
            display1.Width = w;
            display1.Height = h;
            display1.Left = 0;//(this.ClientSize.Width - w) / 2;
            display1.Top = 0; //(this.ClientSize.Height - h) / 2;
            display1.PreviewKeyDown += KeyDown_Custom;
            display1.MouseMove += Display1_MouseMove;
            display1.MakeCurrent();
            this.Controls.Add(display1);
            //initGL();
            //OpenTK.Graphics.IGraphicsContextInternal ctx = (OpenTK.Graphics.IGraphicsContextInternal)OpenTK.Graphics.GraphicsContext.CurrentContext;
            //IntPtr raw_context_handle = ctx.Context.Handle;

            IFS Params = new IFS();
            renderer = new IFSEngine.RendererGL(Params, w, h);
            
            //if leap init
            //leap = new Leap.Leap(/*WindowsFormsSynchronizationContext.Current*/SynchronizationContext.Current, r);

            //Swipe.RightSwiped += (s, e) => UpdateIteratorSelectedText();
            //Swipe.LeftSwiped += (s, e) => UpdateIteratorSelectedText();

            //IteratorManipulator.Renderer = r;
            //IteratorManipulator.Params = Params;
            UpdateIteratorSelectedText();

            renderer.DisplayFrameCompleted += R_DisplayFrameCompleted;
            //r.RenderFrameCompleted += R_RenderFrameCompleted;

        }

        Stopwatch fps = new Stopwatch();
        private void R_DisplayFrameCompleted(object sender, EventArgs e)
        {
            display1.MakeCurrent();//render thread gets the context
            display1.SwapBuffers();
            this.Invoke((MethodInvoker)delegate { fps.Stop(); Text = $"{(fps.ElapsedMilliseconds>0?1000/(fps.ElapsedMilliseconds):0)} FPS"; fps.Restart(); });
        }

        int lastX;
        int lastY;
        private void Display1_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                renderer.Camera.ProcessMouseMovement((e.X - lastX), (lastY - e.Y));
            }
            lastX = e.X;
            lastY = e.Y;
        }       

        private void BrightnessOrGamma_ValueChanged(object sender, EventArgs e)
        {
            renderer.Brightness = (float)Convert.ToDouble(numericUpDownBrightness.Value);
            renderer.Gamma = (float)Convert.ToDouble(numericUpDownGamma.Value);
        }


        private void ButtonRender_Click(object sender, EventArgs e)
        {
            renderer.StartRendering();
        }

        private void ButtonStop_Click(object sender, EventArgs e)
        {
            renderer.StopRendering();
        }

        private void NumericUpDownFocus_ValueChanged(object sender, EventArgs e)
        {
            //renderer.MutateCamera(c => {
            //    //c.FocusDistance = (float)Convert.ToDouble(numericUpDownFocus.Value);
            //});
        }

        private void ButtonRandomize_Click(object sender, EventArgs e)
        {
            renderer.Reset();           
            UpdateIteratorSelectedText();
        }

        private void KeyDown_Custom(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.F10)
            {
                SetControlVisibility(!ControlsVisible);
            }
            else if (e.KeyCode == Keys.F11 || (FullScreen && e.KeyCode == Keys.Escape))
            {
                //TODO: refactor
                if (!FullScreen)
                {
                    restore.location = Location;
                    restore.width = Width;
                    restore.height = Height;
                    Activate();
                    Location = new Point(0, 0);
                    FormBorderStyle = FormBorderStyle.None;
                    Width = Screen.PrimaryScreen.Bounds.Width;
                    Height = Screen.PrimaryScreen.Bounds.Height;
                    FullScreen = true;
                }
                else
                {                   
                   TopMost = false;
                   WindowState = FormWindowState.Normal;
                   FormBorderStyle = FormBorderStyle.Sizable;
                   Width = restore.width;
                   Height = restore.height;
                   Location = restore.location;
                   FullScreen = false;
                }
            }
            else if (IsKeyDown(Keys.R))
            {
                ButtonRandomize_Click(this, e);
            }
            else if (IsKeyDown(Keys.W, Keys.A, Keys.S, Keys.D, Keys.Q, Keys.E, Keys.C))
            {
                    var translateVector = new Vector3(0.02f * ((IsKeyDown(Keys.W) ? 1 : 0) - (IsKeyDown(Keys.S) ? 1 : 0)),
                        0.02f * ((IsKeyDown(Keys.D) ? 1 : 0) - (IsKeyDown(Keys.A) ? 1 : 0)),
                        0.02f * ((IsKeyDown(Keys.E) ? 1 : 0) - (IsKeyDown(Keys.C) || (IsKeyDown(Keys.Q)) ? 1 : 0)));
                    renderer.Camera.Translate(translateVector);
                
            }
        }

        //TODO: refactor. Fullscreenhez: uj ablak, uj renderer, parametereket atadjuk
        struct clientRect
        {
            public Point location;
            public int width;
            public int height;
        };
        clientRect restore;
        private bool FullScreen { get; set; } = false;
        private bool ControlsVisible { get; set; } = true;
        private void SetControlVisibility(bool visible)
        {
            ControlsVisible = visible;
            labelBrightness.Visible = visible;
            labelGamma.Visible = visible;
            numericUpDownBrightness.Visible = visible;
            numericUpDownGamma.Visible = visible;
            numericUpDownFocus.Visible = visible;
            buttonRender.Visible = visible;
            buttonStop.Visible = visible;
            buttonRandomize.Visible = visible;

            IteratorSelectLabel.Visible = visible;
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            renderer.StopRendering();
            renderer.Dispose();
        }

        private void NumericUpDownFog_ValueChanged(object sender, EventArgs e)
        {
            //renderer.MutateCamera(c => {
            //   // c.EnableDepthFog = checkBox1.Checked;
            //});
        }

        private void numericUpDownDOF_ValueChanged(object sender, EventArgs e)
        {
            //renderer.MutateCamera(c => {
            //    //c.DepthOfField = (float)Convert.ToDouble(numericUpDownDOF.Value);
            //});
        }

        private void SaveImage_Click(object sender, EventArgs e)
        {
            var p = renderer.GenerateImage();
            Bitmap b = new Bitmap(renderer.Width, renderer.Height);
            for(int y = 0;y<renderer.Height;y++)
                for (int x = 0; x < renderer.Width; x++)
                {
                    b.SetPixel(x, renderer.Height - y - 1, Color.FromArgb((int)(255.0 * p[x, y][3]), (int)(255.0 * p[x, y][0]), (int)(255.0 * p[x, y][1]), (int)(255.0*p[x, y][2])));
                }
            b.Save("Output.bmp");
        }

    }
}
