using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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

        RendererGL renderer;

        GLControl display1;

        const int w = 1080;
        const int h = 1080;

        public Form1()
        {
            InitializeComponent();

            //Init GL Control
            this.Width = w;
            this.Height = h;
            OpenTK.Toolkit.Init();
            display1 = new OpenTK.GLControl();
            display1.Width = w;
            display1.Height = h;
            display1.Left = 0;
            display1.Top = 0;
            display1.PreviewKeyDown += KeyDown_Custom;
            display1.MouseMove += Display1_MouseMove;
            display1.MakeCurrent();
            this.Controls.Add(display1);


            IFS Params = new IFS();
            renderer = new RendererGL(Params, w, h);

            renderer.DisplayFrameCompleted += R_DisplayFrameCompleted;

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
                renderer.ActiveView.Camera.ProcessMouseMovement((e.X - lastX), (lastY - e.Y));
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
            renderer.ActiveView.FocusDistance = (float)numericUpDownFocus.Value;
            renderer.InvalidateAccumulation();
        }

        private void ButtonRandomize_Click(object sender, EventArgs e)
        {
            renderer.Reset();
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
            else if (IsKeyDown(Keys.W, Keys.A, Keys.S, Keys.D, Keys.Q, Keys.E, Keys.C, Keys.I, Keys.K, Keys.J, Keys.L, Keys.U, Keys.O))
            {
                var translateVector = new Vector3(0.02f * ((IsKeyDown(Keys.W) ? 1 : 0) - (IsKeyDown(Keys.S) ? 1 : 0)),
                    0.02f * ((IsKeyDown(Keys.D) ? 1 : 0) - (IsKeyDown(Keys.A) ? 1 : 0)),
                    0.02f * ((IsKeyDown(Keys.E) ? 1 : 0) - (IsKeyDown(Keys.C) || (IsKeyDown(Keys.Q)) ? 1 : 0)));
                renderer.ActiveView.Camera.Translate(translateVector);

                float pitchd = 0.05f * ((IsKeyDown(Keys.I) ? 1 : 0) - (IsKeyDown(Keys.K) ? 1 : 0));
                float yawd = 0.05f * ((IsKeyDown(Keys.J) ? 1 : 0) - (IsKeyDown(Keys.L) ? 1 : 0));
                float rolld = 0.05f * ((IsKeyDown(Keys.U) ? 1 : 0) - (IsKeyDown(Keys.O) ? 1 : 0));
                (renderer.ActiveView.Camera as IFSEngine.Model.Camera.QuatCamera)?.RotateBy(yawd, pitchd, rolld);//HACK: Camera api tervezni (baseclass / interfacek / ..)

                renderer.ActiveView.Camera.UpdateCamera();
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
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            renderer.StopRendering();
            renderer.Dispose();
        }

        private void NumericUpDownFog_ValueChanged(object sender, EventArgs e)
        {
            renderer.ActiveView.FogEffect = (float)numericUpDownFog.Value;
            renderer.InvalidateAccumulation();
        }

        private void numericUpDownDOF_ValueChanged(object sender, EventArgs e)
        {
            renderer.ActiveView.Dof = (float)numericUpDownDOF.Value;
            renderer.InvalidateAccumulation();
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
