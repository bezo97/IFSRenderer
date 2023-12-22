using System.Windows;

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
