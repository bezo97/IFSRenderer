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

namespace WpfDisplay.Views;

public partial class ExitDialogWindow : Window
{
    public ExitChoice ExitDialogResult { get; set; } = ExitChoice.Cancel;

    public ExitDialogWindow()
    {
        InitializeComponent();
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        ExitDialogResult = ExitChoice.Save;
        DialogResult = true;
        Close();
    }

    private void QuitButton_Click(object sender, RoutedEventArgs e)
    {
        ExitDialogResult = ExitChoice.Quit;
        DialogResult = true;
        Close();
    }

    public enum ExitChoice
    {
        Quit,
        Save,
        Cancel
    }
}
