using System.Windows.Controls;

namespace WpfDisplay.Views.Animation;

/// <summary>
/// Interaction logic for AnimationTitle.xaml
/// </summary>
public partial class AnimationTitle : UserControl
{
    public AnimationTitle()
    {
        InitializeComponent();
    }

    public AnimationTitle(string animatedVariableName)
    {
        this.Loaded += (o, args) => AnimatedVariableName.Text = animatedVariableName;
        InitializeComponent();
    }

}
