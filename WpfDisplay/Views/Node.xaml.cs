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

namespace WpfDisplay.Views
{
    /// <summary>
    /// Interaction logic for Node.xaml
    /// </summary>
    public partial class Node : UserControl
    {
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
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                tx = vm.XCoord - (float)e.GetPosition(null).X;//vm.XCoord - e.GetPosition(Map).X;
                ty = vm.YCoord - (float)e.GetPosition(null).Y;//vm.YCoord - e.GetPosition(Map).Y;
                vm.SelectNode();
            }
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                //TODO: start dragging new connection
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            e.Handled = true;

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

        //start/finish connecting nodes

        private void Ellipse_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                var vm = (IteratorViewModel)DataContext;
                vm.StartConnecting();
            }
        }

        private void Ellipse_MouseEnter(object sender, MouseEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                var vm = (IteratorViewModel)DataContext;
                vm.FinishConnecting();
            }
        }


    }
}
