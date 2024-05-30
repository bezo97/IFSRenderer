#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using CommunityToolkit.Mvvm.Input;

using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

/// <summary>
/// Interaction logic for ValueSlider.xaml
/// </summary>
public partial class ValueSlider : UserControl
{
    [LibraryImport("User32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetCursorPos(int X, int Y);

    private static double IncrementMultiplier => 1.0
        * (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ? 10 : 1)
        * (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? 0.1 : 1);


    /// <summary>
    /// Hide decimal places for integers, show fix 4 decimal places for double
    /// </summary>
    private void UpdateLabelFormat() => displayLabel.ContentStringFormat = (Increment % 1 == 0) ? "N0" : "N4";

    private void UpdateAnimatedSymbol()
    {
        Dispatcher.Invoke(() =>
        {

            char symbol = ' ';
            Brush symbolColor = Brushes.Gray;
            if (!string.IsNullOrEmpty(AnimationPath))
            {
                var main = (MainViewModel)System.Windows.Application.Current.MainWindow.DataContext;//ugh
                var animatedState = main.AnimationViewModel.GetChannelCurrentState(AnimationPath);
                symbol = animatedState switch
                {
                    null => '◇',
                    false => '◆',
                    true => '◈',
                };

                if (animatedState is not null)
                {
                    var isValueModified = main.AnimationViewModel.IsChannelBeingModified(AnimationPath, Value);
                    symbolColor = isValueModified ? Brushes.Orange : Brushes.White;
                }
            }
            symbolButton1.Content = symbol;
            symbolButton1.Foreground = symbolColor;
            symbolButton2.Content = symbol;
            symbolButton2.Foreground = symbolColor;
        });
    }


    public ValueSliderSettings? SliderSettings
    {
        get => (ValueSliderSettings?)GetValue(SliderSettingsProperty);
        set => SetValue(SliderSettingsProperty, value);
    }
    public static readonly DependencyProperty SliderSettingsProperty =
        DependencyProperty.Register("SliderSettings", typeof(ValueSliderSettings), typeof(ValueSlider), new FrameworkPropertyMetadata((s, e) =>
        {
            ValueSlider slider = (ValueSlider)s;
            ValueSliderSettings sliderSettings = (ValueSliderSettings)e.NewValue;
            if (sliderSettings is not null)
            {
                if (!BindingOperations.IsDataBound(s, LabelProperty))
                    slider.Label = sliderSettings.Label ?? slider.Label;
                if (!BindingOperations.IsDataBound(s, IsLabelShownProperty))
                    slider.IsLabelShown = sliderSettings.IsLabelShown ?? slider.IsLabelShown;
                if (!BindingOperations.IsDataBound(s, DefaultValueProperty))
                    slider.DefaultValue = sliderSettings.DefaultValue ?? slider.DefaultValue;
                if (!BindingOperations.IsDataBound(s, MinValueProperty))
                    slider.MinValue = sliderSettings.MinValue ?? slider.MinValue;
                if (!BindingOperations.IsDataBound(s, MaxValueProperty))
                    slider.MaxValue = sliderSettings.MaxValue ?? slider.MaxValue;
                if (!BindingOperations.IsDataBound(s, IncrementProperty))
                    slider.Increment = sliderSettings.Increment ?? slider.Increment;
                if (!BindingOperations.IsDataBound(s, AnimationPathProperty))
                    slider.AnimationPath = sliderSettings.AnimationPath ?? slider.AnimationPath;
                if (!BindingOperations.IsDataBound(s, ValueWillChangeActionProperty))
                    slider.ValueWillChangeAction = sliderSettings.ValueWillChange ?? slider.ValueWillChangeAction;
                if (!BindingOperations.IsDataBound(s, ValueChangedActionProperty))
                    slider.ValueChangedAction = sliderSettings.ValueChanged ?? slider.ValueChangedAction;
                if (!BindingOperations.IsDataBound(s, ValueDraggingStartedActionProperty))
                    slider.ValueDraggingStartedAction = sliderSettings.ValueDraggingStarted ?? slider.ValueDraggingStartedAction;
                if (!BindingOperations.IsDataBound(s, ValueDraggingCompletedActionProperty))
                    slider.ValueDraggingCompletedAction = sliderSettings.ValueDraggingCompleted ?? slider.ValueDraggingCompletedAction;
                if (!BindingOperations.IsDataBound(s, AnimateCommandProperty))
                    slider.AnimateCommand = sliderSettings.AnimateCommand ?? slider.AnimateCommand;
                //
                if (!BindingOperations.IsDataBound(s, ToolTipProperty))
                    slider.ToolTip = sliderSettings.ToolTip ?? slider.ToolTip;
            }
        }));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register("Label", typeof(string), typeof(ValueSlider), new PropertyMetadata(null));

    public bool IsLabelShown
    {
        get => (bool)GetValue(IsLabelShownProperty);
        set => SetValue(IsLabelShownProperty, value);
    }
    public static readonly DependencyProperty IsLabelShownProperty =
        DependencyProperty.Register("IsLabelShown", typeof(bool), typeof(ValueSlider), new PropertyMetadata(true));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register("Value", typeof(double), typeof(ValueSlider), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (s, e) =>
        {
            ((ValueSlider)s).UpdateAnimatedSymbol();
        }));

    public double DefaultValue
    {
        get => (double)GetValue(DefaultValueProperty);
        set => SetValue(DefaultValueProperty, value);
    }
    public static readonly DependencyProperty DefaultValueProperty =
        DependencyProperty.Register("DefaultValue", typeof(double), typeof(ValueSlider), new PropertyMetadata(0.0));

    public double Increment
    {
        get => (double)GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }
    public static readonly DependencyProperty IncrementProperty =
        DependencyProperty.Register("Increment", typeof(double), typeof(ValueSlider), new FrameworkPropertyMetadata(0.1, (s, e) =>
        {
            ((ValueSlider)s).UpdateLabelFormat();
        }));

    public double? MinValue
    {
        get => (double?)GetValue(MinValueProperty);
        set => SetValue(MinValueProperty, value);
    }
    public static readonly DependencyProperty MinValueProperty =
        DependencyProperty.Register("MinValue", typeof(double?), typeof(ValueSlider), new PropertyMetadata(null));

    public double? MaxValue
    {
        get => (double?)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }
    public static readonly DependencyProperty MaxValueProperty =
        DependencyProperty.Register("MaxValue", typeof(double?), typeof(ValueSlider), new PropertyMetadata(null));

    public bool Editing
    {
        get => (bool)GetValue(EditingProperty);
        set => SetValue(EditingProperty, value);
    }
    public static readonly DependencyProperty EditingProperty =
        DependencyProperty.Register("Editing", typeof(bool), typeof(ValueSlider), new PropertyMetadata(false));

    public string AnimationPath
    {
        get => (string)GetValue(AnimationPathProperty);
        set => SetValue(AnimationPathProperty, value);
    }
    public static readonly DependencyProperty AnimationPathProperty =
        DependencyProperty.Register("AnimationPath", typeof(string), typeof(ValueSlider), new FrameworkPropertyMetadata(null, (s, e) =>
        {
            ((ValueSlider)s).UpdateAnimatedSymbol();
            ((ValueSlider)s).InternalAnimateCommand.NotifyCanExecuteChanged();
        }));

    public Action ValueWillChangeAction
    {
        get => (Action)GetValue(ValueWillChangeActionProperty);
        set => SetValue(ValueWillChangeActionProperty, value);
    }
    public static readonly DependencyProperty ValueWillChangeActionProperty =
        DependencyProperty.Register("ValueWillChangeAction", typeof(Action), typeof(ValueSlider), new PropertyMetadata(null));

    public Action<double> ValueChangedAction
    {
        get => (Action<double>)GetValue(ValueChangedActionProperty);
        set => SetValue(ValueChangedActionProperty, value);
    }
    public static readonly DependencyProperty ValueChangedActionProperty =
        DependencyProperty.Register("ValueChangedAction", typeof(Action<double>), typeof(ValueSlider), new PropertyMetadata(null));

    /// <summary>
    /// Invoked when the user starts changing the value with mouse dragging.
    /// </summary>
    public Action ValueDraggingStartedAction
    {
        get => (Action)GetValue(ValueDraggingStartedActionProperty);
        set => SetValue(ValueDraggingStartedActionProperty, value);
    }
    public static readonly DependencyProperty ValueDraggingStartedActionProperty =
        DependencyProperty.Register("ValueDraggingStartedAction", typeof(Action), typeof(ValueSlider), new PropertyMetadata(null));

    /// <summary>
    /// Invoked after the user has completed changing the value with mouse dragging.
    /// </summary>
    public Action<double> ValueDraggingCompletedAction
    {
        get => (Action<double>)GetValue(ValueDraggingCompletedActionProperty);
        set => SetValue(ValueDraggingCompletedActionProperty, value);
    }
    public static readonly DependencyProperty ValueDraggingCompletedActionProperty =
        DependencyProperty.Register("ValueDraggingCompletedAction", typeof(Action<double>), typeof(ValueSlider), new PropertyMetadata(null));

    private ICommand? _resetValueCommand;
    public ICommand ResetValueCommand => _resetValueCommand ??= new RelayCommand(() =>
    {
        ValueWillChangeAction?.Invoke();
        //Same as in Apophysis: reset to default value, or zero if value is already the default.
        if (Value == DefaultValue)
            Value = 0.0;
        else
            Value = DefaultValue;
        ValueChangedAction?.Invoke(Value);
    });

    public ICommand AnimateCommand
    {
        get => (ICommand)GetValue(AnimateCommandProperty);
        set => SetValue(AnimateCommandProperty, value);
    }
    public static readonly DependencyProperty AnimateCommandProperty =
        DependencyProperty.Register("AnimateCommand", typeof(ICommand), typeof(ValueSlider), new PropertyMetadata(null));

    //little hack for now
    private RelayCommand? _internalAnimateCommand;
    public RelayCommand InternalAnimateCommand => _internalAnimateCommand ??= new RelayCommand(() =>
    {
        AnimateCommand.Execute(new AnimationViewModel.AnimateValueCommandParameters(Label, AnimationPath, Value));
        UpdateAnimatedSymbol();
    }, () => AnimationPath is not null);

    public ValueSlider()
    {
        InitializeComponent();

        //little hack for now
        DataContextChanged += (s, e) =>
        {
            var vm = (MainViewModel)Application.Current.MainWindow.DataContext;
            vm.workspace.PropertyChanged += (s, e) => UpdateAnimatedSymbol();
            AnimateCommand = vm.AnimationViewModel.AnimateValueCommand;
            UpdateLabelFormat();
        };
    }

    private double ConstrainValue(double value)
    {
        if (MinValue != null)
            value = Math.Max((double)MinValue, value);
        if (MaxValue != null)
            value = Math.Min((double)MaxValue, value);
        return value;
    }

    private void Up_Click(object sender, RoutedEventArgs e)
    {
        ValueWillChangeAction?.Invoke();
        Value = ConstrainValue(Value + Increment * IncrementMultiplier);
        ValueChangedAction?.Invoke(Value);
    }

    private void Down_Click(object sender, RoutedEventArgs e)
    {
        ValueWillChangeAction?.Invoke();
        Value = ConstrainValue(Value - Increment * IncrementMultiplier);
        ValueChangedAction?.Invoke(Value);
    }

    private void ValueEditor_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            ValueWillChangeAction?.Invoke();
            StartEditing();
            ValueChangedAction?.Invoke(Value);
        }
        else if (e.Key == Key.Up)
        {
            e.Handled = true;
            Up_Click(sender, e);
        }
        else if (e.Key == Key.Down)
        {
            e.Handled = true;
            Down_Click(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Value = _lastv;//restore
            valueEditor.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
            Editing = false;
            ValueChangedAction?.Invoke(Value);
        }
    }

    private Point _dragp;
    private double _lastv;//value before editing, used to restore previous value when pressing Esc

    private void DisplayLabel_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            if (!displayLabel.IsMouseCaptured)
            {
                ValueWillChangeAction?.Invoke();
                ValueDraggingStartedAction?.Invoke();
                _dragp = e.GetPosition(Window.GetWindow(this));
                _lastv = Value;
                displayLabel.CaptureMouse();
                Mouse.OverrideCursor = Cursors.None;
            }

            double delta = e.GetPosition(Window.GetWindow(this)).X - _dragp.X;
            Value = ConstrainValue(_lastv + delta * Increment * IncrementMultiplier);
            ValueChangedAction?.Invoke(Value);

            //reset position on mouseleave
            if (VisualTreeHelper.HitTest(this, e.GetPosition(displayLabel)) == null)
            {
                _lastv = Value;
                var pos = Window.GetWindow(this).PointToScreen(_dragp);
                SetCursorPos((int)Math.Round(pos.X), (int)Math.Round(pos.Y));
            }
        }

    }

    private void DisplayLabel_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            if (!displayLabel.IsMouseCaptured)
            {//click
                StartEditing();
            }
            else
            {
                var pos = Window.GetWindow(this).PointToScreen(_dragp);
                SetCursorPos((int)Math.Round(pos.X), (int)Math.Round(pos.Y));
                ValueDraggingCompletedAction?.Invoke(Value);
            }
        }
        Mouse.OverrideCursor = null;//no override
        displayLabel.ReleaseMouseCapture();
    }

    private void StartEditing()
    {
        valueEditor.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        _lastv = Value;
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
        ResetValueCommand.Execute(null);
        StartEditing();
    }

    private void ValueEditor_LostFocus(object sender, RoutedEventArgs e) => Editing = false;

    private void valueEditor_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) => Editing = false;

}
