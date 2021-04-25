using IFSEngine;
using IFSEngine.Animation;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using IFSEngine.Rendering;
using WpfDisplay.ViewModels;
using WpfDisplay.Views;
using System.Threading.Tasks;

namespace WpfDisplay.Controls.Animation
{
    /// <summary>
    /// Interaction logic for AnimationTitles.xaml
    /// </summary>
    public partial class AnimationTitles : UserControl
    {
        public AnimationTitles()
        {
            InitializeComponent();
            //Loaded += (s, e) =>
            //{
            //    Thread.Sleep(100);
            //
            //};

            DataContextChanged += AnimationTitles_DataContextChanged;
        }

        private void AnimationTitles_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var animationManager = ((AnimationViewModel)DataContext).AnimationManager;//((MainWindow)Application.Current.MainWindow).DataContext;//((MainViewModel)DataContext).AnimationManager;

            animationManager.OnAnimationCreated += AnimationManagerOnAnimationCreated;
            void AnimationManagerOnAnimationCreated(PropertyAnimation propertyAnimation)
            {
                var animationTitle = new AnimationTitle(propertyAnimation.AnimatedVariableName);

                Titles.Children.Add(animationTitle);
                animationTitle.Width = Titles.ActualWidth;

                Canvas.SetLeft(animationTitle, 0);
                Canvas.SetTop(animationTitle, 0);
            }
        }
    }
}
