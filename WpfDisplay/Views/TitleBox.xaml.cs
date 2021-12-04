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
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

/// <summary>
/// Interaction logic for TitleBox.xaml
/// </summary>
public partial class TitleBox : UserControl
{
    private MainViewModel vm => (MainViewModel)DataContext;
    public TitleBox()
    {
        InitializeComponent();
    }

    private void titleTextBox_GotKeyboardFocus(object sender, RoutedEventArgs e)
    {
        Dispatcher.InvokeAsync(titleTextBox.SelectAll);
    }

    private void titleTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Keyboard.ClearFocus();
            titleTextBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }
        else if (e.Key == Key.Escape)
        {
            vm.IFSTitle = vm.workspace.Ifs.Title;
            Keyboard.ClearFocus();
        }
    }

    private void authorsToggle_Checked(object sender, RoutedEventArgs e)
    {

    }
}
