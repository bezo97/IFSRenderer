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
    private void OnOkCommand()
    {
        DialogResult = true;
    }

}
