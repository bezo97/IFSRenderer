using IFSEngine.Animation;
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

namespace WpfDisplay.Controls.Animation
{
    /// <summary>
    /// Interaction logic for DopeButton.xaml
    /// </summary>
    public partial class DopeButton : UserControl
    {
        public ControlPoint ControlPoint { get; private set; }
        public EventHandler<MouseEventArgs> OnDrag;
        private bool isDragging=false;
        public DopeButton()
        {
            InitializeComponent();
        }

        public void SetControlPoint(ControlPoint controlPoint) => this.ControlPoint = controlPoint;
        public void SetTime(double t) => ControlPoint.t.Update(t);

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            isDragging = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && isDragging)
            {
                OnDrag?.Invoke(this,e);
            }

        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
        }
    }
}
