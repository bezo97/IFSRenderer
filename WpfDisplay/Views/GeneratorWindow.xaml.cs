using System.Windows;

using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

/// <summary>
/// Interaction logic for GeneratorWindow.xaml
/// </summary>
public partial class GeneratorWindow : Window
{


    public GeneratorWindow()
    {
        InitializeComponent();

        ContentRendered += async (s, e) =>
        {
            await ((GeneratorViewModel)DataContext).Initialize();
        };

    }
}
