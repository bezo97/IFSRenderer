using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
//using GLDisplay.Leap;
using IFSEngine;
using IFSEngine.Model;
//using OpenTK;
//using OpenTK.Graphics.OpenGL;
using OpenGL;

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

        IFSEngine.RendererGL r;

        //OpenTK.GLControl display1;
        OpenGL.GlControl display1;

        //Leap.Leap leap;

        private uint texID;

        const int w = 1920;
        const int h = 1080;

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
            display1 = new OpenGL.GlControl();
            display1.Width = w;
            display1.Height = h;
            display1.Left = 0;//(this.ClientSize.Width - w) / 2;
            display1.Top = 0; //(this.ClientSize.Height - h) / 2;
            display1.PreviewKeyDown += KeyDown_Custom;
            display1.MouseMove += Display1_MouseMove;
            //display1.Paint += Display1_Paint;
            display1.Render += Display1_Render;
            //display1.MakeCurrent();
            this.Controls.Add(display1);
            initGL();
            OpenTK.Graphics.IGraphicsContextInternal ctx = (OpenTK.Graphics.IGraphicsContextInternal)OpenTK.Graphics.GraphicsContext.CurrentContext;
            IntPtr raw_context_handle = ctx.Context.Handle;

            IFS Params = new IFS();
            r = new IFSEngine.RendererGL(Params, raw_context_handle, texID);
            
            //if leap init
            //leap = new Leap.Leap(/*WindowsFormsSynchronizationContext.Current*/SynchronizationContext.Current, r);

            //Swipe.RightSwiped += (s, e) => UpdateIteratorSelectedText();
            //Swipe.LeftSwiped += (s, e) => UpdateIteratorSelectedText();

            //IteratorManipulator.Renderer = r;
            //IteratorManipulator.Params = Params;
            UpdateIteratorSelectedText();

            r.DisplayFrameCompleted += R_DisplayFrameCompleted;
            r.RenderFrameCompleted += R_RenderFrameCompleted; ;
        }

        private void Display1_Render(object sender, GlControlEventArgs e)
        {
            Gl.Viewport(0, 0, display1.ClientSize.Width, display1.ClientSize.Height);
            //Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Begin(PrimitiveType.Quads);
            Gl.Vertex2(0, 0);//0 0
            Gl.Vertex2(0, 1);//0 1
            Gl.Vertex2(1, 1);//1 1
            Gl.Vertex2(1, 0);//1 0
            Gl.End();
        }

        private void R_RenderFrameCompleted(object sender, EventArgs e)
        {
            
        }

        Stopwatch fps = new Stopwatch();
        private void R_DisplayFrameCompleted(object sender, EventArgs e)
        {
            fps.Stop();
            display1.Invoke((MethodInvoker)delegate { Refresh(); });
            this.Invoke((MethodInvoker)delegate { Text = $"{(fps.ElapsedMilliseconds>0?1000/fps.ElapsedMilliseconds:0)} FPS"; });
            fps.Restart();
        }

        int lastX;
        int lastY;
        private void Display1_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
            {
                r.MutateCamera(c => {
                    c.Theta += (lastY - e.Y) / 100.0f;
                    c.Phi += (lastX - e.X) / 100.0f;
                    return c;
                });
            }
            lastX = e.X;
            lastY = e.Y;
        }

        /*private void Display1_Paint(object sender, PaintEventArgs e)
        {

            Gl.Begin(PrimitiveType.Quads);
                Gl.Vertex2(0, 0);//0 0
                Gl.Vertex2(0, 1);//0 1
                Gl.Vertex2(1, 1);//1 1
                Gl.Vertex2(1, 0);//1 0
            Gl.End();

            display1.SwapBuffers();
            
        }*/
        
        private void initGL()
        {
            texID = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, texID);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMinFilter.Nearest);
            Gl.TexParameter(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba32f/*cl float*/, w, h, 0, PixelFormat.Rgba, PixelType.Float, new IntPtr(0));
            //GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texID, 0);
            //DrawBuffersEnum[] dbe = new DrawBuffersEnum[1];
            //dbe[0] = DrawBuffersEnum.ColorAttachment0;
            //GL.DrawBuffers(1, dbe);
            if (Gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferStatus.FramebufferComplete)
                Console.WriteLine("BAJBJABJABAJ");
            Gl.ActiveTexture(TextureUnit.Texture0);

            var assembly = typeof(Form1).GetTypeInfo().Assembly;

            var vertexShader = Gl.CreateShader(ShaderType.VertexShader);
            Gl.ShaderSource(vertexShader, new string[] { new StreamReader(assembly.GetManifestResourceStream("GLDisplay.Display.vert")).ReadToEnd() });
            Gl.CompileShader(vertexShader);
            Gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int status);
            if (status == 0)
            {
                StringBuilder sb = new StringBuilder();
                Gl.GetShaderInfoLog(vertexShader, 9999, out int dummy, sb);
                throw new Exception(
                    String.Format("Error compiling {0} shader: {1}", ShaderType.VertexShader.ToString(), sb.ToString()));
            }

            var fragmentShader = Gl.CreateShader(ShaderType.FragmentShader);
            string elo = $"#version 450 \n #extension GL_ARB_explicit_attrib_location : enable \n";
            elo += $"uniform int width={w}; \n uniform int height={h};\n";
            Gl.ShaderSource(fragmentShader, new string[] { elo + new StreamReader(assembly.GetManifestResourceStream("GLDisplay.Display.frag")).ReadToEnd() });
            Gl.CompileShader(fragmentShader);
            Gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out status);
            if (status == 0)
            {
                StringBuilder sb = new StringBuilder();
                Gl.GetShaderInfoLog(fragmentShader, 9999, out int dummy, sb);
                throw new Exception(
                    String.Format("Error compiling {0} shader: {1}", ShaderType.FragmentShader.ToString(), sb.ToString()));
            }
            uint _program = Gl.CreateProgram();
            Gl.AttachShader(_program, vertexShader);
            Gl.AttachShader(_program, fragmentShader);
            Gl.LinkProgram(_program);
            Gl.GetProgram(_program, ProgramProperty.LinkStatus, out status);
            if (status == 0)
            {
                StringBuilder sb = new StringBuilder();
                Gl.GetProgramInfoLog(_program, 9999, out int dummy, sb);
                throw new Exception(
                    String.Format("Error linking program: {0}", sb.ToString()));
            }

            Gl.DetachShader(_program, vertexShader);
            Gl.DetachShader(_program, fragmentShader);
            Gl.DeleteShader(vertexShader);
            Gl.DeleteShader(fragmentShader);

            Gl.UseProgram(_program);

            //beallitjuk a texturat
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);//to screen

            Gl.Viewport(0, 0, w, h);
        }

        private void BrightnessOrGamma_ValueChanged(object sender, EventArgs e)
        {
            r.MutateCamera(c =>
            {
                c.Brightness = (float)Convert.ToDouble(numericUpDownBrightness.Value);
                c.Gamma = (float)Convert.ToDouble(numericUpDownGamma.Value);
                return c;
            });
        }


        private void ButtonRender_Click(object sender, EventArgs e)
        {
            /*Task.Run(() =>
            {
                rendering = true;
                while (rendering)
                {
                    r.Render();
                    UpdateImage();
                }
            });*/
            r.StartRendering();
        }

        private void ButtonStop_Click(object sender, EventArgs e)
        {
            r.StopRendering();
        }

        private void ButtonStep_Click(object sender, EventArgs e)
        {
            r.RenderFrame();
            r.UpdateDisplay();
        }

        private void NumericUpDownFocus_ValueChanged(object sender, EventArgs e)
        {
            r.MutateCamera(c => {
                c.FocusDistance = (float)Convert.ToDouble(numericUpDownFocus.Value);
                return c;
            });
        }

        private void ButtonRandomize_Click(object sender, EventArgs e)
        {
            r.MutateParams(p => p
                .RandomizeParams()
                .ResetCamera()
            );            
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
                r.MutateCamera(c => {
                    c.Translate(
                    0.02f * ((IsKeyDown(Keys.W) ? 1 : 0) - (IsKeyDown(Keys.S) ? 1 : 0)),
                    0.02f * ((IsKeyDown(Keys.D) ? 1 : 0) - (IsKeyDown(Keys.A) ? 1 : 0)),
                    0.02f * ((IsKeyDown(Keys.E) ? 1 : 0) - (IsKeyDown(Keys.C) || (IsKeyDown(Keys.Q)) ? 1 : 0)));
                    return c;
                });
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
            r.StopRendering();
            r.Dispose();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            r.MutateCamera(c => {
                c.EnableDepthFog = checkBox1.Checked;
                return c;
            });
        }

        private void numericUpDownDOF_ValueChanged(object sender, EventArgs e)
        {
            r.MutateCamera(c => {
                c.DepthOfField = (float)Convert.ToDouble(numericUpDownDOF.Value);
                return c;
            });
        }

        private void SaveImage_Click(object sender, EventArgs e)
        {
            var p = r.GenerateImage();
            Bitmap b = new Bitmap(r.Width, r.Height);
            for(int y = 0;y<r.Height;y++)
                for (int x = 0; x < r.Width; x++)
                {
                    b.SetPixel(x, r.Height - y - 1, Color.FromArgb((int)(255.0 * p[x, y][3]), (int)(255.0 * p[x, y][0]), (int)(255.0 * p[x, y][1]), (int)(255.0*p[x, y][2])));
                }
            b.Save("Output.bmp");
        }
    }
}
