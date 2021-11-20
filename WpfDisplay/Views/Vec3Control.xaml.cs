using Microsoft.Toolkit.Mvvm.Input;
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

namespace WpfDisplay.Views
{
    /// <summary>
    /// Interaction logic for Vec3Control.xaml
    /// </summary>
    public partial class Vec3Control : UserControl
    {
        public double Increment
        {
            get { return (double)GetValue(IncrementProperty); }
            set { SetValue(IncrementProperty, value); }
        }
        public static readonly DependencyProperty IncrementProperty =
            DependencyProperty.Register("Increment", typeof(double), typeof(Vec3Control), new PropertyMetadata(0.1));


        public RelayCommand ValueChangedCommand
        {
            get { return (RelayCommand)GetValue(ValueChangedCommandProperty); }
            set { SetValue(ValueChangedCommandProperty, value); }
        }
        public static readonly DependencyProperty ValueChangedCommandProperty =
            DependencyProperty.Register("ValueChangedCommand", typeof(RelayCommand), typeof(Vec3Control), new PropertyMetadata(null));

        public Vec3Control()
        {
            InitializeComponent();
        }
    }
}
