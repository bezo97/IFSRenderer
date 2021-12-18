using IFSEngine.Animation;
using IFSEngine.Utility;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfDisplay.Views.Animation;

/// <summary>
/// Interaction logic for Animation.xaml
/// </summary>
public partial class Animation : UserControl
{
    private readonly AnimationCurve _animationCurve;
    public Animation(AnimationCurve animationCurve)
    {
        this.Loaded += Animation_Loaded;
        this._animationCurve = animationCurve;
        InitializeComponent();
    }

    private void Animation_Loaded(object s, RoutedEventArgs e)
    {

        _animationCurve.OnControlPointCreated += OnControlPointCreatedHandler;
        OnControlPointCreatedHandler(_animationCurve.GetLastControlPoint(), AnimationCurve.GetDuration());

        void OnControlPointCreatedHandler(ControlPoint cp, double animationDuration)
        {
            var dpPoint = new DopeButton();
            dpPoint.SetControlPoint(cp);
            dpPoint.OnDrag += OnDopePointDrag;
            Canvas.SetTop(dpPoint, 0);
            DopeSheet.Children.Add(dpPoint);

            dpPoint.Loaded += (e2, v) =>
            {
                Canvas.SetLeft(dpPoint, TimeLine.MapToActiveArea(cp.t / animationDuration) * DopeSheet.ActualWidth - dpPoint.ActualWidth / 2);

            };

            void OnDopePointDrag(object sender, MouseEventArgs ea)
            {
                var dopeButton = (DopeButton)sender;

                Point point = ea.GetPosition(this);
                var newHorizontalPosition = MathExtensions.Clamp(point.X, DopeSheet.ActualWidth * TimeLine.ActiveAreaStart, DopeSheet.ActualWidth * TimeLine.ActiveAreaEnd);
                Canvas.SetLeft(dopeButton, newHorizontalPosition - (dopeButton.ActualWidth / 2));
                dopeButton.SetTime(((newHorizontalPosition / DopeSheet.ActualWidth - TimeLine.ActiveAreaStart) / (TimeLine.ActiveAreaEnd - TimeLine.ActiveAreaStart)) * animationDuration);
            }
        }
    }
}
