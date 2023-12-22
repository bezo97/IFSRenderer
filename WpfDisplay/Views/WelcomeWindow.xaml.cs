using System.Windows;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

/// <summary>
/// Interaction logic for WelcomeWindow.xaml
/// </summary>
public partial class WelcomeWindow : Window
{
    private WelcomeViewModel vm => (WelcomeViewModel)DataContext;
    public WelcomeWindow()
    {
        InitializeComponent();
        DataContextChanged += (s, e) =>
        {
            ((WelcomeViewModel)e.NewValue).ContinueCommand = new RelayCommand(() =>
            {
                DialogResult = true;
            });
        };
        ContentRendered += async (s, e) =>
        {
            await vm.Initialize();
        };
    }

    private void Paste_Executed(object sender, ExecutedRoutedEventArgs e) => vm.PasteFromClipboardCommand.Execute(null);

    private void Paste_CanExecute(object sender, CanExecuteRoutedEventArgs e) => e.CanExecute = vm?.PasteFromClipboardCommand.CanExecute(null) ?? false;

}
