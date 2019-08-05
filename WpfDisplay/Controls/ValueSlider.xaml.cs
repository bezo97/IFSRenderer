using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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

namespace WpfDisplay.Controls
{
    /// <summary>
    /// Interaction logic for ValueSlider.xaml
    /// </summary>
    public partial class ValueSlider : UserControl
    {
        //TODO: implement optional Min and Max properties

        public string ValueName
        {
            get { return (string)GetValue(ValueNameProperty); }
            set { SetValue(ValueNameProperty, value); }
        }
        public static readonly DependencyProperty ValueNameProperty =
            DependencyProperty.Register("ValueName", typeof(string), typeof(ValueSlider), new PropertyMetadata("ValueName"));



        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(ValueSlider), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        

        public double Increment
        {
            get { return (double)GetValue(IncrementProperty); }
            set { SetValue(IncrementProperty, value); }
        }
        public static readonly DependencyProperty IncrementProperty =
            DependencyProperty.Register("Increment", typeof(double), typeof(ValueSlider), new PropertyMetadata(0.1));


        public bool Editing
        {
            get { return (bool)GetValue(EditingProperty); }
            set { SetValue(EditingProperty, value); }
        }
        public static readonly DependencyProperty EditingProperty =
            DependencyProperty.Register("Editing", typeof(bool), typeof(ValueSlider), new PropertyMetadata(false));





        public ValueSlider()
        {
            InitializeComponent();
        }

        private void Down_Click(object sender, RoutedEventArgs e)
        {
            Value = Math.Round(Value - Increment, 3);
        }

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            Value = Math.Round(Value + Increment, 3);
        }

        private void ValueEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key==Key.Enter)
                Editing = false;
        }

        private void ValueEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            Editing = false;
        }

        bool dragging = false;
        double dragp;
        double lastv;//value before editing

        private void Button_MouseDown(object sender, MouseButtonEventArgs e)
        {
            dragging = true;
            dragp = e.GetPosition(this).X;
            lastv = Value;
        }

        private void Button_MouseMove(object sender, MouseEventArgs e)
        {
            if(dragging)
            {
                double delta = (e.GetPosition(this).X - dragp);
                Value = lastv + delta*Increment;
            }
        }

        private void Button_MouseUp(object sender, MouseButtonEventArgs e)
        {
            dragging = false;
            double delta = (e.GetPosition(this).X - dragp);

            if (Math.Abs(delta)<1)
            {//click
                Editing = true;
                ValueEditor.Focus();
            }
        }

        private void Button_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Value = Math.Round(Value + e.Delta/Math.Abs(e.Delta)*Increment, 3);
        }
    }
}
