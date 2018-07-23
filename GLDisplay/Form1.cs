using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GLDisplay
{
    public partial class Form1 : Form
    {
        IFSEngine.Renderer r;

        OpenTK.GLControl display1;

        private int texID;

        const int w = 720;
        const int h = 720;
        float brightness = 1.0f;
        float gamma = 4.0f;


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
            display1.Paint += Display1_Paint;
            display1.MakeCurrent();
            this.Controls.Add(display1);
            initGL();
            OpenTK.Graphics.IGraphicsContextInternal ctx = (OpenTK.Graphics.IGraphicsContextInternal)OpenTK.Graphics.GraphicsContext.CurrentContext;
            IntPtr raw_context_handle = ctx.Context.Handle;
            r = new IFSEngine.Renderer(w, h, 10000, raw_context_handle, texID);
            r.Render();
            UpdateImage();
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

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            brightness = (float)Convert.ToDouble(numericUpDown1.Value);
            gamma = (float)Convert.ToDouble(numericUpDown2.Value);
            UpdateImage();
        }

        bool rendering = false;

        private void button1_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                int timer = 1;
                int timermax = 1;
                rendering = true;
                while (rendering)
                {
                    r.Render();
                    if(timer<=0)
                    {
                        UpdateImage();
                        timermax *= 2;
                        timer = timermax;
                    }
                    timer--;
                }
            });
        }

        private void button2_Click(object sender, EventArgs e)
        {
            rendering = false;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            r.Render();
            UpdateImage();
        }
    }
}
