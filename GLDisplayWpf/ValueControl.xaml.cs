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

namespace GLDisplayWpf
{
    /// <summary>
    /// Interaction logic for ValueControl.xaml
    /// </summary>
    public partial class ValueControl : UserControl
    {
        public float Minimum {
            get => (float)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }
        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
            "Minimum", typeof(float), typeof(ValueControl), new PropertyMetadata(0.0f));

        public float Maximum {
            get => (float)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }
        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
            "Maximum", typeof(float), typeof(ValueControl), new PropertyMetadata(1.0f));

        public float ButtonStep {
            get => (float)GetValue(ButtonStepProperty);
            set => SetValue(ButtonStepProperty, value);
        }
        public static readonly DependencyProperty ButtonStepProperty = DependencyProperty.Register(
            "ButtonStep", typeof(float), typeof(ValueControl), new PropertyMetadata(0.05f));

        public string Text {
            get => (string)GetValue(TextProprty);
            set => SetValue(TextProprty, value);
        }
        public static readonly DependencyProperty TextProprty = DependencyProperty.Register(
            "Text", typeof(string), typeof(ValueControl), new PropertyMetadata("Label"));

        public float NativeValue {
            get => (float)GetValue(NativeValueProprty);
            set {
                SetValue(NativeValueProprty, value);
                //InvokeValueChanged();
            }
        }
        public static readonly DependencyProperty NativeValueProprty = DependencyProperty.Register(
            "NativeValue", typeof(float), typeof(ValueControl), new PropertyMetadata(0.0f));

        public float Value {
            get => RangeToValue(mvalue);
            set {
                mvalue = ValueToRange(value);
                NativeValue = mvalue;
            }
        }

        public PropertyChangedEventHandler<float> ValueChanged;

        public Func<float, float> RangeToValue { get; set; } = f => f;
        public Func<float, float> ValueToRange { get; set; } = f => f;


        private float mvalue = 0.0f;

        public ValueControl()
        {
            DataContext = this;
            InitializeComponent();
            NativeValue = mvalue;
        }              

        private void ButtonUp_Click(object sender, RoutedEventArgs e)
        {
            mvalue += ButtonStep;
            NativeValue = mvalue;
        }

        private void ButtonDown_Click(object sender, RoutedEventArgs e)
        {
            mvalue -= ButtonStep;
            NativeValue = mvalue;
        }

        private void InvokeValueChanged()
        {
            ValueChanged?.Invoke(this, new PropertyChangedEventArgs<float>(Value));
        }
    }

    public delegate void PropertyChangedEventHandler<T>(object sender, PropertyChangedEventArgs<T> e);
    public class PropertyChangedEventArgs<T> : EventArgs
    {
        public T Value;
        public PropertyChangedEventArgs(T value)
        {
            Value = value;
        }
    }
}
