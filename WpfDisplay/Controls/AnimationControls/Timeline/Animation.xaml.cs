using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using IFSEngine;
using IFSEngine.Animation;
using IFSEngine.Utility;

namespace WpfDisplay.Controls.Animation
{
    /// <summary>
    /// Interaction logic for Animation.xaml
    /// </summary>
    public partial class Animation : UserControl
    {
        private AnimationCurve animationCurve;
        public Animation(AnimationCurve animationCurve)
        {
            this.Loaded += Animation_Loaded;
            this.animationCurve = animationCurve;
            InitializeComponent();
        }

        private void Animation_Loaded(object s, RoutedEventArgs e)
        {

            animationCurve.OnControlPointCreated += OnControlPointCreatedHandler;
            OnControlPointCreatedHandler(animationCurve.GetLastControlPoint(), animationCurve.GetDuration());

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
                    var newHorizontalPosition = MathExtensions.Clamp(point.X, DopeSheet.ActualWidth * TimeLine.activeAreaStart, DopeSheet.ActualWidth * TimeLine.activeAreaEnd);
                    Canvas.SetLeft(dopeButton, newHorizontalPosition - (dopeButton.ActualWidth / 2));
                    dopeButton.SetTime(((newHorizontalPosition / DopeSheet.ActualWidth - TimeLine.activeAreaStart) / (TimeLine.activeAreaEnd - TimeLine.activeAreaStart)) * animationDuration);
                }
            }
        }
    }
}
