using CommunityToolkit.Mvvm.Input;
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
using System.Windows.Shapes;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

/// <summary>
/// Interaction logic for WelcomeWindow.xaml
/// </summary>
public partial class WelcomeWindow : Window
{
    WelcomeViewModel vm => (WelcomeViewModel)DataContext;
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

    private void Paste_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        vm.PasteFromClipboardCommand.Execute(null);
    }

    private void Paste_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = vm?.PasteFromClipboardCommand.CanExecute(null) ?? false;
    }

}
