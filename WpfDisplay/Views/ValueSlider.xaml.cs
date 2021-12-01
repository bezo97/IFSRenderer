using IFSEngine;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfDisplay.Views;

/// <summary>
/// Interaction logic for ValueSlider.xaml
/// </summary>
public partial class ValueSlider : UserControl
{

    [DllImport("User32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    public string ValueName
    {
        get { return (string)GetValue(ValueNameProperty); }
        set { SetValue(ValueNameProperty, value); }
    }
    public static readonly DependencyProperty ValueNameProperty =
        DependencyProperty.Register("ValueName", typeof(string), typeof(ValueSlider), new PropertyMetadata(null));

    public double Value
    {
        get { return (double)GetValue(ValueProperty); }
        set
        {
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

    private static double IncrementMultiplier => 1.0
        * (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt) ? 10 : 1)
        * (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? 0.1 : 1);

    public ValueSlider()
    {
        InitializeComponent();
    }

    private void Down_Click(object sender, RoutedEventArgs e)
    {
        ValueChangedCommand?.Execute(null);
        Value = Math.Round(Value - Increment * IncrementMultiplier, 3);
    }

    private void Up_Click(object sender, RoutedEventArgs e)
    {
        ValueChangedCommand?.Execute(null);
        Value = Math.Round(Value + Increment * IncrementMultiplier, 3);
    }

    private void Animate_Click(object sender, RoutedEventArgs e)
    {
        //((RendererGL)DataContext).AnimationManager.AddNewAnimation(SetValue);
    }

    private void ValueEditor_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            //valueEditor.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            StartEditing();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Value = lastv;//restore
            valueEditor.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            Editing = false;
        }
    }

    private void ValueEditor_LostFocus(object sender, RoutedEventArgs e)
    {
        Editing = false;
    }

    Point dragp;
    double lastv;//value before editing

    private void DisplayLabel_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            ValueChangedCommand?.Execute(null);
            dragp = e.GetPosition(Window.GetWindow(this));
            lastv = Value;
            displayLabel.CaptureMouse();
            Mouse.OverrideCursor = Cursors.None;
        }
    }

    private void DisplayLabel_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && displayLabel.IsMouseCaptured)
        {
            double delta = (e.GetPosition(Window.GetWindow(this)).X - dragp.X);
            Value = lastv + delta * Increment * IncrementMultiplier;

            //reset position on mouseleave
            if (VisualTreeHelper.HitTest(this, e.GetPosition(displayLabel)) == null)
            {
                lastv = Value;
                var pos = displayLabel.PointToScreen(new Point(displayLabel.ActualWidth / 2, displayLabel.ActualHeight / 2));
                SetCursorPos((int)pos.X, (int)pos.Y);
            }
        }

    }

    private void DisplayLabel_MouseUp(object sender, MouseButtonEventArgs e)
    {
        double delta = (e.GetPosition(Window.GetWindow(this)).X - dragp.X);

        if (Math.Abs(delta) < 1)
        {//click
            StartEditing();
        }
        Mouse.OverrideCursor = null;//no override
        displayLabel.ReleaseMouseCapture();
    }

    public RelayCommand ValueChangedCommand
    {
        get { return (RelayCommand)GetValue(ValueChangedCommandProperty); }
        set { SetValue(ValueChangedCommandProperty, value); }
    }
    public static readonly DependencyProperty ValueChangedCommandProperty =
        DependencyProperty.Register("ValueChangedCommand", typeof(RelayCommand), typeof(ValueSlider), new PropertyMetadata(null));

    private void StartEditing()
    {
        valueEditor.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        lastv = Value;
        Editing = true;
        valueEditor.Focus();
        valueEditor.SelectAll();
    }

    private void valueSlider_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (e.OldFocus != e.NewFocus)
        {
            StartEditing();
        }
    }
}
