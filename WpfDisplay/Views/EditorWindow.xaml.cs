using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
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

        //fix laggy ui by disabling wpf hardware rendering
        protected override void OnSourceInitialized(EventArgs e)
        {
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;

            if (hwndSource != null)
                hwndSource.CompositionTarget.RenderMode = RenderMode.SoftwareOnly;

            base.OnSourceInitialized(e);
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
