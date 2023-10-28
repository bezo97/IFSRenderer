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
    private AnimationViewModel _vm => DataContext as AnimationViewModel;
    public AnimationPanel()
    {
        InitializeComponent();
    }

    private Point _dragp;

    private void DopeButton_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var dopeButton = (Button)sender;
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            //_vm.Workspace.TakeSnapshot();
            _vm.AddToSelection((KeyframeViewModel)dopeButton.DataContext);
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
            _vm.PreviewRepositionSelectedKeyframes(delta);
        }
    }

    private void DopeButton_MouseUp(object sender, MouseButtonEventArgs e)
    {
        var dopeButton = (Button)sender;
        if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released)
        {
            _vm.ApplyRepositionOfSelectedKeyframes();
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
            _vm.KeyframeInsertPosition = e.GetPosition((IInputElement)sender).X / 50.0f/*view scale*/;
        }

        //TODO: ClickCount not working
        //if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released && e.ClickCount == 2)
        //{//Insert keyframe on double click
        //    _vm.KeyframeInsertPosition = e.GetPosition((IInputElement)sender).X / 50.0f/*view scale*/;
        //    ((ChannelViewModel)((FrameworkElement)sender).DataContext).InsertKeyframe();
        //}
    }

    private void TimeScrubber_MouseMove(object sender, MouseEventArgs e)
    {
        if(e.LeftButton == MouseButtonState.Pressed && e.OriginalSource == sender)
        {
            var t = e.GetPosition((IInputElement)sender).X / 50.0f/*view scale*/;
            _vm.JumpToTime(t);
        }
    }
}
