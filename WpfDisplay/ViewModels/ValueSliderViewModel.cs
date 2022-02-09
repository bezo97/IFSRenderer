using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class ValueSliderViewModel
{
    public ValueSliderViewModel(INotifyPropertyChanged inpc)
    {
        inpc.PropertyChanged += (s, e) => OnPropertyChanged(nameof(Value));
    }

    public double Value {
        get => GetV();
        set
        {
            double constrainedValue = value;
            if (MinValue != null)
                constrainedValue = Math.Max(MinValue ?? 0, constrainedValue);
            if (MaxValue != null)
                constrainedValue = Math.Min(MaxValue ?? 0, constrainedValue);
            SetV(constrainedValue);
            OnPropertyChanged(nameof(Value));
        }
    }
    
    public Func<double> GetV { get; set; }
    public Action<double> SetV { get; set; }
    public double DefaultValue { get; set; }
    public Action? ValueWillChange { get; set; }

    [ObservableProperty] private string _label;
    [ObservableProperty] private string _toolTip;
    public double Increment { get; set; } = 0.1;
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }

    public static double IncrementMultiplier => 1.0
        * (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ? 10 : 1)
        * (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? 0.1 : 1);

    [ICommand]
    public void IncreaseValue()
    {
        ValueWillChange?.Invoke();
        Value += Increment * IncrementMultiplier;
    }

    [ICommand]
    public void DecreaseValue()
    {
        ValueWillChange?.Invoke();
        Value -= Increment * IncrementMultiplier;
    }

    [ICommand]
    public void ResetValue()
    {
        ValueWillChange?.Invoke();
        //Same as in Apophysis: reset to default value, or zero if value is already the default.
        if (Value == DefaultValue)
            Value = 0.0;
        else
            Value = DefaultValue;
    }
}
