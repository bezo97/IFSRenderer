using IFSEngine;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using IFSEngine.Rendering;
using WpfDisplay.ViewModels;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

/// <summary>
/// Interaction logic for ValueSlider.xaml
/// </summary>
public partial class ValueSlider : UserControl
{
    [DllImport("User32.dll")]
    private static extern bool SetCursorPos(int X, int Y);

    private ValueSliderViewModel vm => (ValueSliderViewModel)DataContext;

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

    private void Up_Click(object sender, RoutedEventArgs e)
    {
        vm.IncreaseValue();
    }

    private void Down_Click(object sender, RoutedEventArgs e)
    {
        vm.DecreaseValue();
    }

    private void ValueEditor_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            //valueEditor.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            StartEditing();
        }
        else if (e.Key == Key.Up)
        {
            e.Handled = true;
            vm.IncreaseValue();
        }
        else if (e.Key == Key.Down)
        {
            e.Handled = true;
            vm.DecreaseValue();
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            vm.Value = _lastv;//restore
            valueEditor.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            Editing = false;
        }
    }

    private void ValueEditor_LostFocus(object sender, RoutedEventArgs e)
    {
        Editing = false;
    }

    private Point _dragp;
    private double _lastv;//value before editing

    private void DisplayLabel_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            vm.ValueWillChange?.Invoke();
            _dragp = e.GetPosition(Window.GetWindow(this));
            _lastv = vm.Value;
            displayLabel.CaptureMouse();
            Mouse.OverrideCursor = Cursors.None;
        }
    }

    private void DisplayLabel_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && displayLabel.IsMouseCaptured)
        {
            double delta = (e.GetPosition(Window.GetWindow(this)).X - _dragp.X);
            vm.Value = _lastv + delta * vm.Increment * ValueSliderViewModel.IncrementMultiplier;

            //reset position on mouseleave
            if (VisualTreeHelper.HitTest(this, e.GetPosition(displayLabel)) == null)
            {
                _lastv = vm.Value;
                var pos = displayLabel.PointToScreen(new Point(displayLabel.ActualWidth / 2, displayLabel.ActualHeight / 2));
                SetCursorPos((int)pos.X, (int)pos.Y);
            }
        }

    }

    private void DisplayLabel_MouseUp(object sender, MouseButtonEventArgs e)
    {
        double delta = (e.GetPosition(Window.GetWindow(this)).X - _dragp.X);

        if (Math.Abs(delta) < 1)
        {//click
            StartEditing();
        }
        Mouse.OverrideCursor = null;//no override
        displayLabel.ReleaseMouseCapture();
    }

    private void StartEditing()
    {
        valueEditor.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        _lastv = vm.Value;
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

    private void valueEditor_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        vm.ResetValue();
        StartEditing();
    }
}
