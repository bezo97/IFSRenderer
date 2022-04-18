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
    public AnimationPanel()
    {
        InitializeComponent();
    }

    private Point _dragp;

    private void DopeButton_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            //vm.ValueWillChange?.Invoke();
            ((AnimationViewModel)DataContext).AddToSelection((KeyframeViewModel)((UserControl)sender).DataContext);
            _dragp = e.GetPosition(Window.GetWindow(this));
            //_lastv = vm.Value;
            ((UserControl)sender).CaptureMouse();
            Mouse.OverrideCursor = Cursors.None;
        }
    }


    private void DopeButton_MouseMove(object sender, MouseEventArgs e)
    {
        var dopeButton = ((UserControl)sender);
        if (e.LeftButton == MouseButtonState.Pressed && dopeButton.IsMouseCaptured)
        {
            double delta = (e.GetPosition(Window.GetWindow(this)).X - _dragp.X);
            //vm.Value = _lastv + delta * vm.Increment * ValueSliderViewModel.IncrementMultiplier;

            //selected dopeButtons contentpresenter canvas left set
            var container = (ContentPresenter)System.Windows.Media.VisualTreeHelper.GetParent(dopeButton);
            //Canvas.SetLeft(container, /*Canvas.GetLeft(container) + */delta);
            ((KeyframeViewModel)dopeButton.DataContext).PositionMoveOffset = delta;

                //reset position on mouseleave
                //if (VisualTreeHelper.HitTest(this, e.GetPosition(dopeButton) == null)
                //{
                //    //_lastv = vm.Value;
                //    var pos = ((UserControl)sender).PointToScreen(new Point(dopeButton.ActualWidth / 2, dopeButton.ActualHeight / 2));
                //    SetCursorPos((int)pos.X, (int)pos.Y);
                //}
        }
    
    }

    private void DopeButton_MouseUp(object sender, MouseButtonEventArgs e)
    {
        double delta = (e.GetPosition(Window.GetWindow(this)).X - _dragp.X);
        ((AnimationViewModel)DataContext).MoveSelectedKeyframesCommand.Execute(delta);
        Mouse.OverrideCursor = null;//no override
        ((UserControl)sender).ReleaseMouseCapture();
        _dragp = new Point(0, 0);
    }
}
