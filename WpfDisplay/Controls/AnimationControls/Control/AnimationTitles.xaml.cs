using IFSEngine;
using IFSEngine.Animation;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using IFSEngine.Rendering;
using WpfDisplay.Views;

namespace WpfDisplay.Controls.Animation
{
    /// <summary>
    /// Interaction logic for AnimationTitles.xaml
    /// </summary>
    public partial class AnimationTitles : UserControl
    {
        public AnimationTitles()
        {
            Loaded += (s, e) =>
            {
                var animationManager = ((RendererGL)Application.Current.Windows.OfType<MainWindow>().First().DataContext).AnimationManager;

                animationManager.OnAnimationCreated += AnimationManagerOnAnimationCreated;
                void AnimationManagerOnAnimationCreated(PropertyAnimation propertyAnimation)
                {
                    var animationTitle = new AnimationTitle(propertyAnimation.AnimatedVariableName);

                    Titles.Children.Add(animationTitle);
                    animationTitle.Width = Titles.ActualWidth;
                    
                    Canvas.SetLeft(animationTitle,0);
                    Canvas.SetTop(animationTitle,0);
                }
            };
            InitializeComponent();
        }
    }
}
