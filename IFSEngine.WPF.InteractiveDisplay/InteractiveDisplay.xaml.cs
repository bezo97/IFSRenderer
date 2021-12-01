using IFSEngine.Rendering;
using OpenTK.Windowing.Common;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Timers;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Threading;
using Vortice.XInput;

namespace IFSEngine.WPF.InteractiveDisplay;

/// <summary>
/// Interaction logic for InteractiveDisplay.xaml
/// </summary>
public partial class InteractiveDisplay : WindowsFormsHost
{
    public IGraphicsContext GraphicsContext => GLControl1.Context;
    public RendererGL Renderer { get; private set; }

    public ICommand InteractionStartedCommand
    {
        get { return (ICommand)GetValue(InteractionStartedCommandProperty); }
        set { SetValue(InteractionStartedCommandProperty, value); }
    }
    public static readonly DependencyProperty InteractionStartedCommandProperty =
        DependencyProperty.Register("InteractionStartedCommand", typeof(ICommand), typeof(InteractiveDisplay), new PropertyMetadata(null));

    public ICommand InteractionFinishedCommand
    {
        get { return (ICommand)GetValue(InteractionFinishedCommandProperty); }
        set { SetValue(InteractionFinishedCommandProperty, value); }
    }
    public static readonly DependencyProperty InteractionFinishedCommandProperty =
        DependencyProperty.Register("InteractionFinishedCommand", typeof(ICommand), typeof(InteractiveDisplay), new PropertyMetadata(null));

    private bool _invertX;
    public bool InvertRotationAxisX
    {
        get { return (bool)GetValue(InvertRotationAxisXProperty); }
        set { SetValue(InvertRotationAxisXProperty, value); }
    }
    public static readonly DependencyProperty InvertRotationAxisXProperty =
        DependencyProperty.Register("InvertRotationAxisX", typeof(bool), typeof(InteractiveDisplay),
            new PropertyMetadata(false, (a, b) => { ((InteractiveDisplay)a)._invertX = (bool)b.NewValue; }));

    private bool _invertY;
    public bool InvertRotationAxisY
    {
        get { return (bool)GetValue(InvertRotationAxisYProperty); }
        set { SetValue(InvertRotationAxisYProperty, value); }
    }
    public static readonly DependencyProperty InvertRotationAxisYProperty =
        DependencyProperty.Register("InvertRotationAxisY", typeof(bool), typeof(InteractiveDisplay),
            new PropertyMetadata(false, (a, b) => { ((InteractiveDisplay)a)._invertY = (bool)b.NewValue; }));

    private bool _invertZ;
    public bool InvertRotationAxisZ
    {
        get { return (bool)GetValue(InvertRotationAxisZProperty); }
        set { SetValue(InvertRotationAxisZProperty, value); }
    }
    public static readonly DependencyProperty InvertRotationAxisZProperty =
        DependencyProperty.Register("InvertRotationAxisZ", typeof(bool), typeof(InteractiveDisplay),
            new PropertyMetadata(false, (a, b) => { ((InteractiveDisplay)a)._invertZ = (bool)b.NewValue; }));

    private float _sensitivity = 1.0f;
    public float Sensitivity
    {
        get { return (float)GetValue(SensitivityProperty); }
        set { SetValue(SensitivityProperty, value); }
    }
    public static readonly DependencyProperty SensitivityProperty =
        DependencyProperty.Register("Sensitivity", typeof(float), typeof(InteractiveDisplay),
            new PropertyMetadata(1.0f, (a, b) => { ((InteractiveDisplay)a)._sensitivity = (float)b.NewValue; }));

    [DllImport("User32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    private const float DeadZoneMultiplier = 0.25f;
    private const float ThumbstickValMax = 32768.0f;
    private bool IsInteractionEnabled => Renderer is not null && Renderer.IsRendering && Renderer.UpdateDisplayOnRender;

    private readonly Timer _controlTimer;
    private readonly KeyboardHelper _keyboard;
    //last mouse position
    private float _lastX;
    private float _lastY;

    public InteractiveDisplay()
    {
        InitializeComponent();
        //init keyboard + gamepad input
        _keyboard = new KeyboardHelper(this);
        _controlTimer = new Timer();
        _controlTimer.Elapsed += Control_Tick;
        _controlTimer.Interval = 16;//ms
        _controlTimer.Start();
    }

    private void Control_Tick(object sender, EventArgs e)
    {
        if (IsInteractionEnabled)
        {
            //TODO: InteractionStartedCommand?.Execute(null);
            Vector3 translateVec = new();
            Vector3 rotateVec = new();
            float fdDelta = 0;

            //keyboard input
            translateVec += new Vector3(
                (_keyboard.IsKeyDown(Key.D) ? 1 : 0) - (_keyboard.IsKeyDown(Key.A) ? 1 : 0),
                (_keyboard.IsKeyDown(Key.E) ? 1 : 0) - (_keyboard.IsKeyDown(Key.Q) ? 1 : 0),
                (_keyboard.IsKeyDown(Key.W) ? 1 : 0) - (_keyboard.IsKeyDown(Key.S) ? 1 : 0)) * 0.005f;
            rotateVec += new Vector3(
                (_keyboard.IsKeyDown(Key.L) ? 1 : 0) - (_keyboard.IsKeyDown(Key.J) ? 1 : 0),
                (_keyboard.IsKeyDown(Key.K) ? 1 : 0) - (_keyboard.IsKeyDown(Key.I) ? 1 : 0),
                (_keyboard.IsKeyDown(Key.U) ? 1 : 0) - (_keyboard.IsKeyDown(Key.O) ? 1 : 0)) * 0.03f;
            //keyboard sensitivity modifiers:
            if (_keyboard.IsKeyDown(Key.LeftAlt) || _keyboard.IsKeyDown(Key.RightAlt))
            {//TODO: LeftAlt is not caught (system key)
                translateVec *= 2.0f;
                rotateVec *= 2.0f;
            }
            if (_keyboard.IsKeyDown(Key.LeftShift) || _keyboard.IsKeyDown(Key.RightShift))
            {
                translateVec *= 0.05f;
                rotateVec *= 0.05f;
            }

            //gamepad input
            if (XInput.GetState(0, out State s))
            {
                float sideDelta = 0.0f;
                if (Math.Abs(s.Gamepad.LeftThumbX + 1) > Gamepad.LeftThumbDeadZone * DeadZoneMultiplier)
                    sideDelta = s.Gamepad.LeftThumbX / ThumbstickValMax;
                float forwardDelta = 0.0f;
                if (Math.Abs(s.Gamepad.LeftThumbY + 1) > Gamepad.LeftThumbDeadZone * DeadZoneMultiplier)
                    forwardDelta = s.Gamepad.LeftThumbY / ThumbstickValMax;
                float yawDelta = 0.0f;
                if (Math.Abs(s.Gamepad.RightThumbX + 1) > Gamepad.RightThumbDeadZone * DeadZoneMultiplier)
                    yawDelta = s.Gamepad.RightThumbX / ThumbstickValMax;
                float pitchDelta = 0.0f;
                if (Math.Abs(s.Gamepad.RightThumbY + 1) > Gamepad.RightThumbDeadZone * DeadZoneMultiplier)
                    pitchDelta = s.Gamepad.RightThumbY / ThumbstickValMax;
                float rollDelta = 0.0f;
                if (s.Gamepad.Buttons.HasFlag(GamepadButtons.LeftShoulder))
                    rollDelta -= 1.0f;
                if (s.Gamepad.Buttons.HasFlag(GamepadButtons.RightShoulder))
                    rollDelta += 1.0f;
                translateVec += new Vector3(sideDelta, 0.0f, forwardDelta) * 0.01f;
                rotateVec += new Vector3(yawDelta, pitchDelta, rollDelta * 0.1f) * 0.1f;
                fdDelta += s.Gamepad.RightTrigger / 255.0f - s.Gamepad.LeftTrigger / 255.0f;
            }

            if ((translateVec + rotateVec).Length() + fdDelta == 0.0f)
                return;

            //camera speed relates to focus distance
            float cameraSpeed = (float)Math.Abs(Renderer.LoadedParams.Camera.FocusDistance) * 2;
            translateVec *= cameraSpeed;

            if (_invertX)
                rotateVec.X = -rotateVec.X;
            if (_invertY)
                rotateVec.Y = -rotateVec.Y;
            if (_invertZ)
                rotateVec.Z = -rotateVec.Z;

            //camera rotation speed depends on field of view
            float rotateSpeed = Renderer.LoadedParams.Camera.FieldOfView / 180.0f;
            rotateVec = Vector3.Multiply(rotateVec, new Vector3(rotateSpeed, rotateSpeed, 1.0f));

            Renderer.LoadedParams.Camera.FocusDistance += fdDelta * Renderer.LoadedParams.Camera.FocusDistance * 0.03;
            Renderer.LoadedParams.Camera.Translate(translateVec * _sensitivity);
            Renderer.LoadedParams.Camera.Rotate(rotateVec * _sensitivity);
            Renderer.InvalidateHistogramBuffer();

            Dispatcher.InvokeAsync(() => InteractionFinishedCommand?.Execute(null), DispatcherPriority.Input);

        }
    }

    public void AttachRenderer(RendererGL renderer)
    {
        Renderer = renderer;
        Renderer.SetDisplayResolution(GLControl1.Width, GLControl1.Height);
        GLControl1.Resize += (s2, e2) => renderer.SetDisplayResolution(GLControl1.Width, GLControl1.Height);
    }

    private void GLControl1_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (IsInteractionEnabled)
        {
            InteractionStartedCommand?.Execute(null);
            Renderer.LoadedParams.Camera.FocusDistance += e.Delta * Renderer.LoadedParams.Camera.FocusDistance * 0.001;
            Renderer.InvalidateHistogramBuffer();
            InteractionFinishedCommand?.Execute(null);
        }
    }

    private void GLControl1_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (IsInteractionEnabled)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left && GLControl1.Capture)
            {
                if (Mouse.OverrideCursor == null)
                {
                    Mouse.OverrideCursor = System.Windows.Input.Cursors.None;
                    InteractionStartedCommand?.Execute(null);//Hack
                }

                float yawDelta = e.X - _lastX;
                float pitchDelta = e.Y - _lastY;

                if (_invertX)
                    yawDelta = -yawDelta;
                if (_invertY)
                    pitchDelta = -pitchDelta;

                //camera rotation speed depends on field of view
                float rotateSpeed = Renderer.LoadedParams.Camera.FieldOfView / 180.0f;
                yawDelta *= rotateSpeed;
                pitchDelta *= rotateSpeed;

                Vector3 rotateVec = new(yawDelta, pitchDelta, 0.0f);
                Renderer.LoadedParams.Camera.Rotate(rotateVec * 0.01f * _sensitivity);
                Renderer.InvalidateHistogramBuffer();

                //TODO: Mouse position reset
                //if (VisualTreeHelper.HitTest(this, Mouse.GetPosition(this)) == null)
                //{
                //    var pos = this.PointToScreen(new Point(Width / 2, Height / 2));
                //    SetCursorPos((int)pos.X, (int)pos.Y);
                //}
                InteractionFinishedCommand?.Execute(null);
            }
            else
                Mouse.OverrideCursor = null;//no override
            _lastX = e.X;
            _lastY = e.Y;
        }
    }

}
