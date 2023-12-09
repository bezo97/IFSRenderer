#nullable enable
using Cavern;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views.Animation;

/// <summary>
/// Interaction logic for AnimationPanel.xaml
/// </summary>
public partial class AnimationPanel : UserControl
{
    private AnimationViewModel _vm => (AnimationViewModel)DataContext;
    private TileBrush scrubberTicksBrush => (TileBrush)scrubberTicksGrid.Background;
    private CancellationTokenSource _drawingCts = new();

    public AnimationPanel()
    {
        InitializeComponent();

        DataContextChanged += (s, e) =>
        {
            _vm.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName is nameof(_vm.ViewScale) or nameof(_vm.Audio))
                    Dispatcher.InvokeAsync(RedrawScrubberVisuals);
            };
            Dispatcher.InvokeAsync(RedrawScrubberVisuals);
        };
    }

    private Point _dragp;

    private async Task RedrawScrubberVisuals()
    {
        var tilingSize = new Rect(0, 0, _vm.ViewScale, 1);
        scrubberTicksBrush.Viewbox = tilingSize;
        scrubberTicksBrush.Viewport = tilingSize;

        if(_vm.Audio is not null)
        {
            var clip = _vm.Audio.Clip;
            var viewScale = _vm.ViewScale;
            await _drawingCts.CancelAsync();
            _drawingCts = new CancellationTokenSource();
            scrubberAudioBarsImageBrush.ImageSource = await Task.Run(()=>CreateAudioBarsDrawing(clip, viewScale, _drawingCts.Token)) ?? scrubberAudioBarsImageBrush.ImageSource;
        }
    }

    private static DrawingImage? CreateAudioBarsDrawing(Clip audioClip, double viewScale, CancellationToken cancellationToken = default)
    {
        var g = new DrawingGroup();
        double bars_resolution = 5.0 / viewScale;//0.1;
        //const double bars_offset = bars_resolution * viewScale;
        for (double t = 0.0; t < audioClip.Length; t += bars_resolution)
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

            int position = (int)(t * audioClip.SampleRate);
            float[] samples = new float[512];//must be pow of 2
            audioClip.GetData(samples, 0/*left channel*/, position);
            float barHeight = samples.Max();

            var d = new GeometryDrawing
            {
                Brush = new LinearGradientBrush(Color.FromRgb(55, 55, 55), Color.FromRgb(100, 100, 100), 90.0),
                Geometry = new RectangleGeometry(new System.Windows.Rect(t * viewScale - 2.5, 30 - 30 * barHeight, 4.0, 30.0))
            };
            g.Children.Add(d);
        }
        var audioBarsDrawing = new DrawingImage(g);
        audioBarsDrawing.Freeze();
        return audioBarsDrawing;
    }

    private void DopeButton_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var dopeButton = (Button)sender;
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _dragp = e.GetPosition(Window.GetWindow(this));
            dopeButton.CaptureMouse();
            Mouse.OverrideCursor = Cursors.None;
        }
    }


    private void DopeButton_MouseMove(object sender, MouseEventArgs e)
    {
        var dopeButton = (Button)sender;
        if (e.LeftButton == MouseButtonState.Pressed && dopeButton.IsMouseCaptured)
        {
            double delta = (e.GetPosition(Window.GetWindow(this)).X - _dragp.X);
            if (delta != 0.0)
            {
                _vm.AddToSelection((KeyframeViewModel)dopeButton.DataContext);
                _vm.PreviewRepositionSelectedKeyframes(delta);
            }
        }
    }

    private void DopeButton_MouseUp(object sender, MouseButtonEventArgs e)
    {
        var dopeButton = (Button)sender;
        if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released)
        {
            if(_vm.KeyframeRepositionOffset != 0.0)
                _vm.ApplyRepositionOfSelectedKeyframes();
            else
                _vm.FlipSelection((KeyframeViewModel)dopeButton.DataContext);
            Mouse.OverrideCursor = null;//no override
            dopeButton.ReleaseMouseCapture();
            _dragp = new Point(0, 0);
            //trigger click command
            dopeButton.Command.Execute(dopeButton.DataContext);
        }
    }

    private void ChannelHeaderScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        sheetScroller.ScrollToVerticalOffset(channelHeaderScroller.VerticalOffset);
    }

    private void Channel_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Right && e.RightButton == MouseButtonState.Released)
        {//remember the location of the context menu where the keyframe will be inserted
            _vm.KeyframeInsertPosition = e.GetPosition((IInputElement)sender).X / _vm.ViewScale;
        }
    }

    private void TimeScrubber_MouseMove(object sender, MouseEventArgs e)
    {
        if(e.LeftButton == MouseButtonState.Pressed && e.OriginalSource == sender)
        {
            var t = e.GetPosition((IInputElement)sender).X / _vm.ViewScale;
            _vm.JumpToTime(t);
        }
    }

    private void Dopesheet_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Pressed)
        {
            if (e.ClickCount == 1)
            {
                _vm.ClearSelectionCommand.Execute(null);
            }
            else if (e.ClickCount == 2 && ((FrameworkElement)sender).DataContext is ChannelViewModel channel)
            {//Insert keyframe to channel on double click
                _vm.KeyframeInsertPosition = e.GetPosition((IInputElement)sender).X / _vm.ViewScale;
                channel.InsertKeyframe();
            }
        }
    }

    private void TimeScrubber_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        _vm.ViewScale = Math.Clamp(_vm.ViewScale * (e.Delta > 0 ? 1.2f : 1/1.2f), 1.0f, 300.0f);
    }
}
