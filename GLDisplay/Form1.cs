using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using GLDisplay.Leap;
using IFSEngine;
using OpenTK;
using OpenTK.Graphics.OpenGL;

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

        IFSEngine.Renderer r;

        OpenTK.GLControl display1;

        Leap.Leap leap;

        private int texID;

        const int w = 720;
        const int h = 720;
        float brightness = 1.0f;
        float gamma = 4.0f;

        void UpdateIteratorSelectedText()
        {
            IteratorSelectLabel.Text = $"< ({IteratorManipulator.EditState + 1}) / {IteratorManipulator.IteratorCount} >";
        }


        public Form1()
        {
            InitializeComponent();
            this.Width = 1280;
            this.Height = 720;
            OpenTK.Toolkit.Init();//
            display1 = new OpenTK.GLControl();
            display1.Width = w;
            display1.Height = h;
            display1.Left = (this.ClientSize.Width - w) / 2;
            display1.Top = (this.ClientSize.Height - h) / 2;
            display1.PreviewKeyDown += KeyDown_Custom;
            display1.MouseMove += Display1_MouseMove;
            display1.Paint += Display1_Paint;
            display1.MakeCurrent();
            this.Controls.Add(display1);
            initGL();
            OpenTK.Graphics.IGraphicsContextInternal ctx = (OpenTK.Graphics.IGraphicsContextInternal)OpenTK.Graphics.GraphicsContext.CurrentContext;
            IntPtr raw_context_handle = ctx.Context.Handle;
            r = new IFSEngine.Renderer(w, h, raw_context_handle, texID);
            
            //leap init
            leap = new Leap.Leap(/*WindowsFormsSynchronizationContext.Current*/SynchronizationContext.Current, r);

            Swipe.RightSwiped += (s, e) => UpdateIteratorSelectedText();
            Swipe.LeftSwiped += (s, e) => UpdateIteratorSelectedText();
        }

        int lastX;
        int lastY;
        private void Display1_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                r.Camera.Theta += (lastY-e.Y) / 100.0f;
                r.Camera.Phi += (lastX-e.X) / 100.0f;
                r.InvalidateAccumulation();
            }
            lastX = e.X;
            lastY = e.Y;
        }

        public void UpdateImage()
        {
            r.UpdateDisplay(brightness, gamma);
            //display1.Invoke((MethodInvoker)delegate
            //{
                display1.Invalidate();
                //display1.Update();
            //});
        }

        private void Display1_Paint(object sender, PaintEventArgs e)
        {

            GL.Begin(PrimitiveType.Quads);
                GL.Vertex2(0, 0);//0 0
                GL.Vertex2(0, 1);//0 1
                GL.Vertex2(1, 1);//1 1
                GL.Vertex2(1, 0);//1 0
            GL.End();

            display1.SwapBuffers();
        }

        private void initGL()
        {
            texID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texID);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb32f/*cl float*/, w, h, 0, PixelFormat.Rgba, PixelType.Float, new IntPtr(0));
            //GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texID, 0);
            //DrawBuffersEnum[] dbe = new DrawBuffersEnum[1];
            //dbe[0] = DrawBuffersEnum.ColorAttachment0;
            //GL.DrawBuffers(1, dbe);
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
                Console.WriteLine("BAJBJABJABAJ");
            GL.ActiveTexture(TextureUnit.Texture0);

            var assembly = typeof(Form1).GetTypeInfo().Assembly;

            var vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, new StreamReader(assembly.GetManifestResourceStream("GLDisplay.Display.vert")).ReadToEnd());
            GL.CompileShader(vertexShader);
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int status);
            if (status == 0)
                throw new GraphicsException(
                    String.Format("Error compiling {0} shader: {1}", ShaderType.VertexShader.ToString(), GL.GetShaderInfoLog(vertexShader)));

            var fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            string elo = $"#version 450 \n #extension GL_ARB_explicit_attrib_location : enable \n";
            elo += $"uniform int width={w}; \n uniform int height={h};\n";
            GL.ShaderSource(fragmentShader, elo + new StreamReader(assembly.GetManifestResourceStream("GLDisplay.Display.frag")).ReadToEnd());
            GL.CompileShader(fragmentShader);
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out status);
            if (status == 0)
                throw new GraphicsException(
                    String.Format("Error compiling {0} shader: {1}", ShaderType.FragmentShader.ToString(), GL.GetShaderInfoLog(fragmentShader)));
            int _program = GL.CreateProgram();
            GL.AttachShader(_program, vertexShader);
            GL.AttachShader(_program, fragmentShader);
            GL.LinkProgram(_program);
            GL.GetProgram(_program, ProgramParameter.LinkStatus, out status);
            if (status == 0)
                throw new GraphicsException(
                    String.Format("Error linking program: {0}", GL.GetProgramInfoLog(_program)));

            GL.DetachShader(_program, vertexShader);
            GL.DetachShader(_program, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            GL.UseProgram(_program);

            //beallitjuk a texturat
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);//to screen

            GL.Viewport(0, 0, w, h);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void BrightnessOrGamma_ValueChanged(object sender, EventArgs e)
        {
            if (hack <= 0)
            {
                brightness = (float)Convert.ToDouble(numericUpDownBrightness.Value);
                gamma = (float)Convert.ToDouble(numericUpDownGamma.Value);
                //UpdateImage();
                hack = 3;
            }
            hack--;
        }

        bool rendering = false;
        int timermax = 1;

        private void ButtonRender_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                rendering = true;
                while (rendering)
                {
                    r.Render();
                    UpdateImage();
                }
            });
        }

        private void ButtonStop_Click(object sender, EventArgs e)
        {
            rendering = false;
        }

        private void ButtonStep_Click(object sender, EventArgs e)
        {
            r.Render();
            rendering = false;
            UpdateImage();
        }

        int hack = 0;

        private void NumericUpDownFocus_ValueChanged(object sender, EventArgs e)
        {
            if (hack <= 0)
            {
                r.Camera.FocusDistance = (float)Convert.ToDouble(numericUpDownFocus.Value);
                r.InvalidateAccumulation();
                timermax = 1;
                hack = 3;
            }
            hack--;
        }

        private void ButtonRandomize_Click(object sender, EventArgs e)
        {
            IteratorManipulator.Randomize(r);
            timermax = 1;
            UpdateIteratorSelectedText();
        }

        private void KeyDown_Custom(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.F10)
            {
  
                SetControlVisibility(!ControlsVisible);
            } else if (e.KeyCode == Keys.F11)
            {
                if (!FullScreen)
                {
                    this.restore.location = this.Location;
                    this.restore.width = this.Width;
                    this.restore.height = this.Height;
                    Activate();
                    this.Location = new Point(0, 0);
                    this.FormBorderStyle = FormBorderStyle.None;
                    this.Width = Screen.PrimaryScreen.Bounds.Width;
                    this.Height = Screen.PrimaryScreen.Bounds.Height;
                }
                else
                {
                    MessageBox.Show("hmm");
                    this.TopMost = false;
                    this.Location = this.restore.location;
                    this.Width = this.restore.width;
                    this.Height = this.restore.height;
                    this.WindowState = FormWindowState.Normal;
                    this.FormBorderStyle = FormBorderStyle.Sizable;
                }
            }
            else if (IsKeyDown(Keys.W, Keys.A, Keys.S, Keys.D, Keys.Q, Keys.E, Keys.C))
            {
                r.Camera.Translate(
                    0.02f * ((IsKeyDown(Keys.W) ? 1 : 0) - (IsKeyDown(Keys.S) ? 1 : 0)),
                    0.02f * ((IsKeyDown(Keys.D) ? 1 : 0) - (IsKeyDown(Keys.A) ? 1 : 0)),
                    0.02f * ((IsKeyDown(Keys.E) ? 1 : 0) - (IsKeyDown(Keys.C) || (IsKeyDown(Keys.Q)) ? 1 : 0)));
                r.InvalidateAccumulation();
            }
        }

        struct clientRect
        {
            public Point location;
            public int width;
            public int height;
        };
        // this should be in the scope your class
        clientRect restore;
        bool fullscreen = false;
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
            buttonStep.Visible = visible;
            buttonRandomize.Visible = visible;
        }
    }
}
