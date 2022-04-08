using IFSEngine.Animation;
using System.Windows;
using System.Windows.Controls;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views.Animation;

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
        //var animationManager = ((AnimationViewModel)DataContext).AnimationManager;//((MainWindow)Application.Current.MainWindow).DataContext;//((MainViewModel)DataContext).AnimationManager;

        //animationManager.OnAnimationCreated += AnimationManagerOnAnimationCreated;

    }

    private void AnimationManagerOnAnimationCreated(PropertyAnimation propertyAnimation)
    {
        var animationTitle = new AnimationTitle(/*propertyAnimation.Label*/"anim");

        Titles.Children.Add(animationTitle);
        animationTitle.Width = Titles.ActualWidth;

        Canvas.SetLeft(animationTitle, 0);
        Canvas.SetTop(animationTitle, 0);
    }
}
