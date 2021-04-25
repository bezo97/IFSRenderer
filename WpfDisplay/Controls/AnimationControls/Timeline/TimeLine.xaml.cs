using IFSEngine;
using IFSEngine.Animation;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using IFSEngine.Rendering;
using IFSEngine.Utility;
using WpfDisplay.Controls.Animation.Helper;
using WpfDisplay.Views;

namespace WpfDisplay.Controls.Animation
{
    /// <summary>
    /// Interaction logic for TimeLine.xaml
    /// </summary>
    public partial class TimeLine : UserControl
    {
        public const double activeAreaStart = 0.01;
        public const double activeAreaEnd = 0.99;

        private AnimationManager animationManager;
        private bool isMouseDown = false;
        private float currentMaximumTime = 10f;

        public TimeLine()
        {

            Loaded += (s, e) =>
            {
                animationManager = ((RendererGL)Application.Current.Windows.OfType<MainWindow>().First().DataContext).AnimationManager;
                var lineDrawer = new TimelineDrawer(activeAreaStart,activeAreaEnd);
                lineDrawer.DrawLines(TimeSlider,true);
                lineDrawer.DrawLines(Dopesheet,false);
                ManipulateTime(new Point(0, 0));

                animationManager.OnAnimationCreated += AnimationManagerOnAnimationCreated;
                void AnimationManagerOnAnimationCreated(PropertyAnimation propertyAnimation)
                {
                    var animation = new Animation(propertyAnimation.AnimationCurve);
                    
                    AnimationStack.Children.Add(animation);
                    animation.Width = TimeLineLayout.ActualWidth;
                    Canvas.SetTop(animation, 0);
                }
            };
            InitializeComponent();
        }

        public static double MapToActiveArea(double normalizedOriginalValue) => normalizedOriginalValue.Remap(0, 1, activeAreaStart, activeAreaEnd);
        private double MapFromActiveArea(double normalizedOriginalValue) => normalizedOriginalValue.Remap(activeAreaStart, activeAreaEnd, 0, 1);


        private void TimeSlider_MouseWheel(object sender, MouseWheelEventArgs e)
        {

        }

        private void TimeSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = true;
            ManipulateTime(e.GetPosition(this));
        }

        private void ManipulateTime(Point relativePositon)
        {
            var normalizedT = (relativePositon.X / this.ActualWidth).Clamp(activeAreaStart, activeAreaEnd);
            SetDopesheetLinePosition();
            SetAnimatorTime();

            void SetDopesheetLinePosition()
            {
                var x = normalizedT * this.ActualWidth;
                DopesheetLine.X1= x;
                DopesheetLine.X2 = x;
            }

            void SetAnimatorTime()
            {
                animationManager.EvaluateAt(MapFromActiveArea(normalizedT) * currentMaximumTime);
            }
        }



        private void TimeSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            isMouseDown = false;
        }

        private void TimeSlider_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown)
                ManipulateTime(e.GetPosition(this));
        }



        private void TimeSlider_MouseLeave(object sender, MouseEventArgs e)
        {
            isMouseDown = false;
        }
    }
}
