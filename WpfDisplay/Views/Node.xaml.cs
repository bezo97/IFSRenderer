using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views
{
    /// <summary>
    /// Interaction logic for Node.xaml
    /// </summary>
    public partial class Node : UserControl
    {


        public RelayCommand<IteratorViewModel> SelectCommand
        {
            get { return (RelayCommand<IteratorViewModel>)GetValue(SelectCommandProperty); }
            set { SetValue(SelectCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectCommand.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectCommandProperty =
            DependencyProperty.Register("SelectCommand", typeof(RelayCommand<IteratorViewModel>), typeof(Node), new PropertyMetadata(null));



        public Node()
        {
            InitializeComponent();
        }

        private float tx, ty;

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            e.Handled = true;

            var vm = (IteratorViewModel)DataContext;
            if (e.ChangedButton == MouseButton.Left)
            {
                tx = vm.XCoord - (float)e.GetPosition(null).X;//vm.XCoord - e.GetPosition(Map).X;
                ty = vm.YCoord - (float)e.GetPosition(null).Y;//vm.YCoord - e.GetPosition(Map).Y;
                SelectCommand.Execute(vm);
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                vm.StartConnecting();
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            e.Handled = true;
            if (e.ChangedButton == MouseButton.Right)
            {
                var vm = (IteratorViewModel)DataContext;
                vm.FinishConnecting();
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            e.Handled = true;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var vm = (IteratorViewModel)DataContext;
                e.Handled = true;
                vm.XCoord = /*e.GetPosition(Map).X + */tx + (float)e.GetPosition(null).X;
                vm.YCoord = /*e.GetPosition(Map).Y + */ty + (float)e.GetPosition(null).Y;
                vm.Redraw();//TODO: should call ifsvm.Redraw()
            }
        }

    }
}
