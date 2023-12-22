using System.Windows;
using System.Windows.Input;

using CommunityToolkit.Mvvm.Input;

namespace WpfDisplay.Views;

public partial class AboutDialogWindow : Window
{
    public AboutDialogWindow()
    {
        InitializeComponent();
    }

    private ICommand _okCommand;
    public ICommand OkCommand =>
        _okCommand ??= new RelayCommand(OnOkCommand);
    private void OnOkCommand() => DialogResult = true;

}
