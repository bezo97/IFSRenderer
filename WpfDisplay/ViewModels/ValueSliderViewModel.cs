using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.Windows.Input;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class ValueSliderViewModel
{
    public ValueSliderViewModel() { }
    public ValueSliderViewModel(INotifyPropertyChanged inpc)
    {
        inpc.PropertyChanged += (s, e) =>
        {
            if ((IsAnimated && e.PropertyName == "AnimationFrame" && AnimationPath != null) || e.PropertyName == "Ifs")
                OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(AnimatedSymbol));
        };
    }

    public double Value
    {
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
    public string AnimationPath { get; set; }
    public double DefaultValue { get; set; }
    public Action ValueWillChange { get; set; }

    [ObservableProperty] private bool _isAnimated;
    [ObservableProperty] private string _label;
    [ObservableProperty] private string _toolTip;
    public double Increment { get; set; } = 0.1;
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }

    public static double IncrementMultiplier => 1.0
        * (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ? 10 : 1)
        * (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? 0.1 : 1);

    /// <summary>
    /// Hide decimal places for integers, show fix 4 decimal places for double
    /// </summary>
    public string ValueLabelFormat => (Increment % 1 == 0) ? "N0" : "N4";

    public char AnimatedSymbol
    {
        get
        {
            if (AnimationPath is null) return ' ';
            var main = (MainViewModel)System.Windows.Application.Current.MainWindow.DataContext;//ugh
            var animatedState = main.AnimationViewModel.GetChannelCurrentState(AnimationPath);
            switch (animatedState)
            {
                case null: return '◇';
                case false: return '◆';
                case true: return '◈';
            }
        }
    }

    public void RaiseValueChanged()
    {
        OnPropertyChanged(nameof(Value));
    }

    [RelayCommand]
    public void IncreaseValue()
    {
        ValueWillChange?.Invoke();
        Value += Increment * IncrementMultiplier;
    }

    [RelayCommand]
    public void DecreaseValue()
    {
        ValueWillChange?.Invoke();
        Value -= Increment * IncrementMultiplier;
    }

    [RelayCommand]
    public void ResetValue()
    {
        ValueWillChange?.Invoke();
        //Same as in Apophysis: reset to default value, or zero if value is already the default.
        if (Value == DefaultValue)
            Value = 0.0;
        else
            Value = DefaultValue;
    }

    private bool isAnimatable => AnimationPath is not null;
    [RelayCommand(CanExecute = nameof(isAnimatable))] //TODO: Bind from outside
    public void Animate()
    {
        var main = (MainViewModel)System.Windows.Application.Current.MainWindow.DataContext;//ugh
        main.workspace.TakeSnapshot();
        main.AnimationViewModel.AddOrUpdateChannel(Label, AnimationPath, Value);
        IsAnimated = true;
        OnPropertyChanged(nameof(AnimatedSymbol));
    }

}
