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

namespace WpfDisplay.Views
{
    /// <summary>
    /// Interaction logic for EditorWindow.xaml
    /// </summary>
    public partial class EditorWindow : Window
    {
        private IFSViewModel vm => (IFSViewModel)DataContext;

        public EditorWindow()
        {
            InitializeComponent();
        }

        private void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            vm.UndoCommand.Execute(null);
        }

        private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = vm?.UndoCommand.CanExecute(null) ?? false;
        }

        private void Redo_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            vm.RedoCommand.Execute(null);
        }

        private void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = vm?.RedoCommand.CanExecute(null) ?? false;
        }

    }
}
