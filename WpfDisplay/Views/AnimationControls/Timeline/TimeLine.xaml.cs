using IFSEngine.Animation;
using IFSEngine.Utility;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfDisplay.ViewModels;
using WpfDisplay.Views.Animation.Helper;

namespace WpfDisplay.Views.Animation;

/// <summary>
/// Interaction logic for TimeLine.xaml
/// </summary>
public partial class TimeLine : UserControl
{
    public const double ActiveAreaStart = 0.01;
    public const double ActiveAreaEnd = 0.99;

    private bool _isMouseDown = false;

    public TimeLine()
    {

        DataContextChanged += (s, e) =>
        {
            var lineDrawer = new TimelineDrawer(ActiveAreaStart, ActiveAreaEnd);
            lineDrawer.DrawLines(TimeSlider, true);
            lineDrawer.DrawLines(Dopesheet, false);
            ManipulateTime(new Point(0, 0));

            //_animationManager.OnAnimationCreated += AnimationManagerOnAnimationCreated;

        };
        InitializeComponent();
    }

    private void AnimationManagerOnAnimationCreated(PropertyAnimation propertyAnimation)
    {
        var animation = new Animation(propertyAnimation.Channel);

        AnimationStack.Children.Add(animation);
        animation.Width = TimeLineLayout.ActualWidth;
        Canvas.SetTop(animation, 0);
    }

    public static double MapToActiveArea(double normalizedOriginalValue) =>
        normalizedOriginalValue.Remap(0, 1, ActiveAreaStart, ActiveAreaEnd);

    private static double MapFromActiveArea(double normalizedOriginalValue) =>
        normalizedOriginalValue.Remap(ActiveAreaStart, ActiveAreaEnd, 0, 1);


    private void TimeSlider_MouseWheel(object sender, MouseWheelEventArgs e)
    {

    }

    private void TimeSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isMouseDown = true;
        ManipulateTime(e.GetPosition(this));
    }

    private void ManipulateTime(Point relativePositon)
    {
        var normalizedT = (relativePositon.X / this.ActualWidth).Clamp(ActiveAreaStart, ActiveAreaEnd);
        ManipulateTime(normalizedT);
    }

    public void ManipulateTime(double t)
    {
        SetDopesheetLinePosition();
        SetAnimatorTime();

        void SetDopesheetLinePosition()
        {
            var x = t * this.ActualWidth;
            DopesheetLine.X1 = x;
            DopesheetLine.X2 = x;
        }

        void SetAnimatorTime()
        {
            //_animationManager.EvaluateAt(MapFromActiveArea(t));
        }
    }



    private void TimeSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isMouseDown = false;
    }

    private void TimeSlider_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_isMouseDown)
            ManipulateTime(e.GetPosition(this));
    }



    private void TimeSlider_MouseLeave(object sender, MouseEventArgs e)
    {
        _isMouseDown = false;
    }
}
