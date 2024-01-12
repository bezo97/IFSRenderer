#nullable enable
using System;
using System.Windows.Input;

namespace WpfDisplay.ViewModels;

public class ValueSliderSettings
{
    public Action? ValueWillChange { get; init; }
    public Action<double>? ValueChanged { get; init; }
    public Action? ValueDraggingStarted { get; init; }
    public Action<double>? ValueDraggingCompleted { get; init; }
    public ICommand? AnimateCommand { get; init; }
    public string? AnimationPath { get; init; }
    public double? DefaultValue { get; init; }
    public string? Label { get; init; }
    public bool? IsLabelShown { get; init; }
    public string? ToolTip { get; init; }
    public double? Increment { get; init; }
    public double? MinValue { get; init; }
    public double? MaxValue { get; init; }
}
