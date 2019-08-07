using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
using IFSEngine;

namespace WpfDisplay.Controls
{
    /// <summary>
    /// Interaction logic for ValueSlider.xaml
    /// </summary>
    public partial class ValueSlider : UserControl
    {
        //TODO: implement optional Min and Max properties

        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

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
            set {
                double constrainedValue = value;
                if (MinValue != null)
                    constrainedValue = Math.Max(MinValue ?? 0, constrainedValue);
                if (MaxValue != null)
                    constrainedValue = Math.Min(MaxValue ?? 0, constrainedValue);
                SetValue(ValueProperty, constrainedValue);
            }
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


        public double? MinValue
        {
            get { return (double?)GetValue(MinValueProperty); }
            set { SetValue(MinValueProperty, value); }
        }
        public static readonly DependencyProperty MinValueProperty =
            DependencyProperty.Register("MinValue", typeof(double?), typeof(ValueSlider), new PropertyMetadata(null));


        public double? MaxValue
        {
            get { return (double?)GetValue(MaxValueProperty); }
            set { SetValue(MaxValueProperty, value); }
        }
        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register("MaxValue", typeof(double?), typeof(ValueSlider), new PropertyMetadata(null));





        public ValueSlider()
        {
            InitializeComponent();
        }

        private void SetValue(float value)
        {
            Value = value;
        }

        private void Down_Click(object sender, RoutedEventArgs e)
        {
            Value = Math.Round(Value - Increment, 3);
        }

        private void Up_Click(object sender, RoutedEventArgs e)
        {
            Value = Math.Round(Value + Increment, 3);
        }

        private void Animate_Click(object sender, RoutedEventArgs e)
        {
           ((RendererGL)DataContext).AnimationManager.AddNewAnimation(SetValue);
        }

        private void ValueEditor_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key==Key.Enter)
                Editing = false;
        }

        private void ValueEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            Editing = false;
            dragging = false;
        }

        bool dragging = false;
        Point dragp;
        double lastv;//value before editing

        private void Button_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                dragging = true;
                dragp = e.GetPosition(App.Current.MainWindow);
                lastv = Value;
                Mouse.OverrideCursor = Cursors.None;
            }
        }

        bool cursorReset = false;
        private void Button_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragging && !cursorReset)
            {
                double delta = (e.GetPosition(App.Current.MainWindow).X - dragp.X);
                Value = lastv + delta * Increment;
            }
            else if (cursorReset)
                cursorReset = false;
        }

        private void Button_MouseUp(object sender, MouseButtonEventArgs e)
        {
            dragging = false;
            double delta = (e.GetPosition(App.Current.MainWindow).X - dragp.X);

            if (Math.Abs(delta)<1)
            {//click
                Editing = true;
                ValueEditor.Focus();
                ValueEditor.SelectAll();
            }

            Mouse.OverrideCursor = Cursors.Arrow;
        }

        private void Button_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Value = Math.Round(Value + (double)e.Delta/Math.Abs(e.Delta)*Increment, 3);
        }

        private void ValueSlider_MouseLeave(object sender, MouseEventArgs e)
        {
            if (dragging)
            {
                cursorReset = true;
                lastv = Value;
                SetCursorPos((int)(App.Current.MainWindow.Left + dragp.X), (int)(App.Current.MainWindow.Top + dragp.Y + 25));
            }
        }
    }
}
