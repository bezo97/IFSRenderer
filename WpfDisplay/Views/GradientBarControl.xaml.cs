#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

/// <summary>
/// Interactive gradient bar with draggable color keys.
/// Click on bar to insert key, drag keys to reposition, click key to select.
/// </summary>
public partial class GradientBarControl : UserControl
{
    public static readonly DependencyProperty EditorViewModelProperty =
        DependencyProperty.Register(nameof(EditorViewModel), typeof(PaletteEditorViewModel), typeof(GradientBarControl),
            new PropertyMetadata(null, OnEditorViewModelChanged));

    public PaletteEditorViewModel? EditorViewModel
    {
        get => (PaletteEditorViewModel?)GetValue(EditorViewModelProperty);
        set => SetValue(EditorViewModelProperty, value);
    }

    private static void OnEditorViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GradientBarControl control)
        {
            // Unsubscribe from old
            if (e.OldValue is PaletteEditorViewModel oldVm)
                oldVm.PropertyChanged -= control.OnViewModelPropertyChanged;

            // Subscribe to new
            if (e.NewValue is PaletteEditorViewModel newVm)
                newVm.PropertyChanged += control.OnViewModelPropertyChanged;

            control.RebuildHandles();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PaletteEditorViewModel.SelectedKey) ||
            e.PropertyName == nameof(PaletteEditorViewModel.Keys))
        {
            RebuildHandles();
        }
    }

    // Drag state
    private bool _isDragging;
    private Rectangle? _dragHandle;
    private ColorKeyViewModel? _dragKey;

    public GradientBarControl()
    {
        InitializeComponent();
    }

    private void RebuildHandles()
    {
        var vm = EditorViewModel;
        if (vm == null) return;

        // Update gradient brush
        GradientBrush.GradientStops.Clear();
        foreach (var kvp in vm.Palette.KeyColors)
        {
            var color = kvp.Value;
            GradientBrush.GradientStops.Add(new GradientStop(
                Color.FromRgb((byte)(color.X * 255), (byte)(color.Y * 255), (byte)(color.Z * 255)),
                kvp.Key));
        }

        // Rebuild key handles
        KeysCanvas.Children.Clear();
        double barWidth = BarBorder.ActualWidth;
        if (barWidth < 10)
            barWidth = 400; // fallback for initial measure

        foreach (var key in vm.Keys)
        {
            var rect = new Rectangle
            {
                Width = 6,
                Height = 36,
                Fill = new SolidColorBrush(key.Color),
                Stroke = key == vm.SelectedKey ? Brushes.White : Brushes.Black,
                StrokeThickness = key == vm.SelectedKey ? 2 : 1,
                RadiusX = 2,
                RadiusY = 2,
                Tag = key,
                IsHitTestVisible = false // hit testing is on the parent border
            };

            double x = key.Position * barWidth - rect.Width / 2;
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, (48 - rect.Height) / 2);
            KeysCanvas.Children.Add(rect);
        }

        // Update ruler
        BuildRuler(barWidth);
    }

    private void BuildRuler(double barWidth)
    {
        RulerCanvas.Children.Clear();
        for (double pos = 0; pos <= 1.0; pos += 0.1)
        {
            var tick = new Line
            {
                X1 = pos * barWidth,
                Y1 = 0,
                X2 = pos * barWidth,
                Y2 = pos % 0.5 == 0 ? 8 : 4,
                Stroke = Brushes.Gray,
                StrokeThickness = 1
            };
            RulerCanvas.Children.Add(tick);

            if (pos % 0.5 == 0)
            {
                var label = new TextBlock
                {
                    Text = pos.ToString("F1"),
                    Foreground = Brushes.Gray,
                    FontSize = 9,
                    Margin = new Thickness(-8, 8, 0, 0)
                };
                Canvas.SetLeft(label, pos * barWidth - 8);
                RulerCanvas.Children.Add(label);
            }
        }
    }

    // Invalidate size on layout
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        if (sizeInfo.NewSize.Width != sizeInfo.PreviousSize.Width)
            RebuildHandles();
    }

    #region Mouse Handling

    private void Bar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        var vm = EditorViewModel;
        if (vm == null) return;

        double barWidth = BarBorder.ActualWidth;
        var pos = GetPositionRelativeToBar(e);
        double gradientPos = Math.Clamp(pos / barWidth, 0.0, 1.0);

        // Check if clicking on an existing key handle
        var clickedKey = FindKeyAtPosition(gradientPos, barWidth, 12); // 12px tolerance

        if (clickedKey != null)
        {
            // Select and start dragging
            vm.SelectedKey = clickedKey;
            _isDragging = true;
            _dragKey = clickedKey;
            BarBorder.CaptureMouse();
            e.Handled = true;
        }
        else
        {
            // Insert new key at click position
            vm.AddKeyCommand.Execute(gradientPos);
            e.Handled = true;
        }
    }

    private void Bar_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || _dragKey == null) return;

        var vm = EditorViewModel;
        if (vm == null) return;

        double barWidth = BarBorder.ActualWidth;
        var pos = GetPositionRelativeToBar(e);
        double gradientPos = Math.Clamp(pos / barWidth, 0.0, 1.0);

        vm.UpdateKeyPosition(_dragKey, gradientPos);
        e.Handled = true;
    }

    private void Bar_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            BarBorder.ReleaseMouseCapture();
            _isDragging = false;
            _dragKey = null;
            e.Handled = true;
        }
    }

    private double GetPositionRelativeToBar(MouseEventArgs e)
    {
        var point = e.GetPosition(BarBorder);
        return point.X;
    }

    private ColorKeyViewModel? FindKeyAtPosition(double gradientPos, double barWidth, double tolerancePx)
    {
        var vm = EditorViewModel;
        if (vm == null) return null;

        double targetX = gradientPos * barWidth;
        double tolerance = tolerancePx;

        foreach (var key in vm.Keys)
        {
            double keyX = key.Position * barWidth;
            if (Math.Abs(keyX - targetX) <= tolerance)
                return key;
        }
        return null;
    }

    #endregion
}
