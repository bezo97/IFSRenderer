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
            //vm.ValueWillChange?.Invoke();
            _vm.AddToSelection((KeyframeViewModel)dopeButton.DataContext);
            _dragp = e.GetPosition(Window.GetWindow(this));
            //_lastv = vm.Value;
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
            ((KeyframeViewModel)dopeButton.DataContext).PositionMoveOffset = delta;
        }

    }

    private void DopeButton_MouseUp(object sender, MouseButtonEventArgs e)
    {
        var dopeButton = (Button)sender;
        if (e.ChangedButton == MouseButton.Left && e.LeftButton == MouseButtonState.Released)
        {
            double delta = (e.GetPosition(Window.GetWindow(this)).X - _dragp.X);
            _vm.MoveSelectedKeyframesCommand.Execute(delta);
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

}
