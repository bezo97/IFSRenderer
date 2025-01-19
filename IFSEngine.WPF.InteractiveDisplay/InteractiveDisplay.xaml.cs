using System;
using System.Numerics;
using System.Threading;
using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Threading;

using IFSEngine.Rendering;

using OpenTK.GLControl;
using OpenTK.Windowing.Common;

using Vortice.XInput;

using Timer = System.Timers.Timer;

namespace IFSEngine.WPF.InteractiveDisplay;

/// <summary>
/// Interaction logic for InteractiveDisplay.xaml
/// </summary>
public partial class InteractiveDisplay : WindowsFormsHost
{
    public IGraphicsContext? GraphicsContext => _glControl1.Context;
    public RendererGL? Renderer { get; private set; }

    public ICommand InteractionStartedCommand
    {
        get => (ICommand)GetValue(InteractionStartedCommandProperty);
        set => SetValue(InteractionStartedCommandProperty, value);
    }
    public static readonly DependencyProperty InteractionStartedCommandProperty =
        DependencyProperty.Register("InteractionStartedCommand", typeof(ICommand), typeof(InteractiveDisplay), new PropertyMetadata(null));

    public ICommand InteractionFinishedCommand
    {
        get => (ICommand)GetValue(InteractionFinishedCommandProperty);
        set => SetValue(InteractionFinishedCommandProperty, value);
    }
    public static readonly DependencyProperty InteractionFinishedCommandProperty =
        DependencyProperty.Register("InteractionFinishedCommand", typeof(ICommand), typeof(InteractiveDisplay), new PropertyMetadata(null));

    private bool _invertY;
    public bool InvertRotationAxisY
    {
        get => (bool)GetValue(InvertRotationAxisYProperty);
        set => SetValue(InvertRotationAxisYProperty, value);
    }
    public static readonly DependencyProperty InvertRotationAxisYProperty =
        DependencyProperty.Register("InvertRotationAxisY", typeof(bool), typeof(InteractiveDisplay),
            new PropertyMetadata(false, (a, b) => { ((InteractiveDisplay)a)._invertY = (bool)b.NewValue; }));

    private float _sensitivity = 1.0f;
    public float Sensitivity
    {
        get => (float)GetValue(SensitivityProperty);
        set => SetValue(SensitivityProperty, value);
    }
    public static readonly DependencyProperty SensitivityProperty =
        DependencyProperty.Register("Sensitivity", typeof(float), typeof(InteractiveDisplay),
            new PropertyMetadata(1.0f, (a, b) => { ((InteractiveDisplay)a)._sensitivity = (float)b.NewValue; }));
    private bool _isGamepadConnected = false;
    public event EventHandler<bool>? GamepadConnectionStateChanged;
    public event EventHandler? DisplayResolutionChanged;

    //[LibraryImport("User32.dll")]
    //[return: MarshalAs(UnmanagedType.Bool)]
    //private static partial bool SetCursorPos(int X, int Y);

    private const float DeadZoneMultiplier = 1.0f;
    private const float ThumbstickValMax = 32768.0f;
    private bool IsInteractionEnabled => Renderer is not null && Renderer.IsRendering && Renderer.UpdateDisplayOnRender;

    private readonly Timer _controlTimer;
    private readonly KeyboardHelper _keyboard;
    private Vector2 _lastMousePos;
    private Vector2 _centerPivot;

    private readonly GLControl _glControl1;

    public InteractiveDisplay()
    {
        InitializeComponent();

        //Setup GLControl in the WinFormsHost. This could be done in XAML, but we need to configure it with a constructor parameter.
        var glControlSettings = GLControlSettings.Default.Clone();
        glControlSettings.AlphaBits = 0; //Disable alpha channel of the display, so the background remains black after .net9 bringing in the w11 system theme
        _glControl1 = new GLControl(glControlSettings);
        _glControl1.MouseDown += GLControl1_MouseDown;
        _glControl1.MouseMove += GLControl1_MouseMove;
        _glControl1.MouseWheel += GLControl1_MouseWheel;
        winformsHost.Child = _glControl1;

        //init keyboard + gamepad input
        _keyboard = new KeyboardHelper(this);
        _controlTimer = new Timer();
        _controlTimer.Elapsed += Control_Tick;
        _controlTimer.Interval = 16;//ms
        _controlTimer.Start();
    }

    private void Control_Tick(object? sender, EventArgs e)
    {
        if (IsInteractionEnabled && Renderer is not null)
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
            if (_keyboard.IsKeyDown(Key.LeftCtrl) || _keyboard.IsKeyDown(Key.RightCtrl))
            {
                translateVec *= 2.0f;
                rotateVec *= 2.0f;
            }
            if (_keyboard.IsKeyDown(Key.LeftShift) || _keyboard.IsKeyDown(Key.RightShift))
            {
                translateVec *= 0.05f;
                rotateVec *= 0.05f;
            }

            //gamepad input
            var connected = XInput.GetState(0, out State s);
            if (connected)
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
                    pitchDelta = -s.Gamepad.RightThumbY / ThumbstickValMax;
                float rollDelta = 0.0f;
                if (s.Gamepad.Buttons.HasFlag(GamepadButtons.LeftShoulder))
                    rollDelta -= 1.0f;
                if (s.Gamepad.Buttons.HasFlag(GamepadButtons.RightShoulder))
                    rollDelta += 1.0f;
                translateVec += new Vector3(sideDelta, 0.0f, forwardDelta) * 0.01f;
                rotateVec += new Vector3(yawDelta, pitchDelta, rollDelta * 0.1f) * 0.1f;
                fdDelta += s.Gamepad.RightTrigger / 255.0f - s.Gamepad.LeftTrigger / 255.0f;
            }

            if (_isGamepadConnected != connected)
            {
                _isGamepadConnected = connected;
                GamepadConnectionStateChanged?.Invoke(this, _isGamepadConnected);
            }

            if ((translateVec + rotateVec).Length() + fdDelta == 0.0f)
                return;

            //camera speed relates to focus distance
            float cameraSpeed = (float)Math.Abs(Renderer.LoadedParams.Camera.FocusDistance) * 2;
            translateVec *= cameraSpeed;

            if (_invertY)
                rotateVec.Y = -rotateVec.Y;

            //camera rotation speed depends on field of view
            float rotateSpeed = (float)Renderer.LoadedParams.Camera.FieldOfView / 180.0f;
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
        Renderer.SetDisplayResolution(_glControl1.Width, _glControl1.Height);
    }

    private void GLControl1_MouseWheel(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (IsInteractionEnabled && Renderer is not null)
        {
            InteractionStartedCommand?.Execute(null);
            Renderer.LoadedParams.Camera.FocusDistance += e.Delta / Mouse.MouseWheelDeltaForOneLine * Renderer.LoadedParams.Camera.FocusDistance * 0.1;
            Renderer.InvalidateHistogramBuffer();
            InteractionFinishedCommand?.Execute(null);
        }
    }

    private void GLControl1_MouseMove(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (IsInteractionEnabled && Renderer is not null)
        {
            var mousePos = new Vector2(e.X, e.Y);
            if (_glControl1.Capture)
            {
                Vector3 rotateVec = Vector3.Zero;
                if (e.Button == System.Windows.Forms.MouseButtons.Left)//yaw, pitch
                    rotateVec = new(e.X - _lastMousePos.X, e.Y - _lastMousePos.Y, 0.0f);
                else if (e.Button == System.Windows.Forms.MouseButtons.Right)//fov, roll
                {
                    var lastCenterOffset = _lastMousePos - _centerPivot;
                    var lastDir = Vector2.Normalize(lastCenterOffset);
                    var moveDelta = mousePos - _lastMousePos;
                    rotateVec = new(0.0f, 0.0f, lastDir.X * moveDelta.Y - lastDir.Y * moveDelta.X);

                    var centerOffset = mousePos - _centerPivot;
                    Renderer.LoadedParams.Camera.FieldOfView *= lastCenterOffset.Length() / centerOffset.Length();
                }

                if (rotateVec.Length() > 0.0f)
                {
                    if (Mouse.OverrideCursor == null)
                    {
                        Mouse.OverrideCursor = rotateVec.Z != 0.0 ? Cursors.Cross : Cursors.None;
                        InteractionStartedCommand?.Execute(null);//Hack
                    }

                    if (_invertY)
                        rotateVec.Y = -rotateVec.Y;

                    //camera rotation speed (only yaw and pitch) depends on field of view
                    float rotateSpeed = (float)Renderer.LoadedParams.Camera.FieldOfView / 180.0f;
                    rotateVec.X *= rotateSpeed;
                    rotateVec.Y *= rotateSpeed;

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
            }
            else
                Mouse.OverrideCursor = null;//no override
            _lastMousePos = mousePos;
        }
    }

    /// <summary>
    /// Forward the winforms event to wpf
    /// </summary>
    private void GLControl1_MouseDown(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (e.Button != System.Windows.Forms.MouseButtons.Left)
            return;

        RaiseEvent(new System.Windows.Input.MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
        {
            RoutedEvent = Mouse.MouseDownEvent,
            Source = this,
        });
    }

    private CancellationTokenSource _cts = new();
    private void WindowsFormsHost_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        _cts.Cancel();
        _cts = new CancellationTokenSource();
        var c = _cts.Token;
        Renderer?.SetDisplayResolution(_glControl1.Width, _glControl1.Height);
        System.Threading.Tasks.Task.Run(async () =>
        {
            await System.Threading.Tasks.Task.Delay(250);
            if (c.IsCancellationRequested) return;
            Dispatcher.Invoke(() => DisplayResolutionChanged?.Invoke(this, e));
        }, c);
        _centerPivot = new Vector2((float)(ActualWidth / 2.0f), (float)(ActualHeight / 2.0f));
    }
}
