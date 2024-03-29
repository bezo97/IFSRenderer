﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

using CommunityToolkit.Mvvm.ComponentModel;

using WpfDisplay.Helper;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

public partial class ConnectionViewModel : ObservableObject
{
    private readonly IEnumerable<ConnectionViewModel> _nodemapConnections;
    public readonly IteratorViewModel from;
    public readonly IteratorViewModel to;
    private readonly Workspace _workspace;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ForegroundZIndex))]
    private bool _isSelected;

    public int ForegroundZIndex => IsSelected ? 1 : 0;

    public BindablePoint StartPoint => from.Position;
    public BindablePoint EndPoint => to.Position;
    public Point ArrowHeadMid { get; set; }
    public Point ArrowHeadLeft { get; set; }
    public Point ArrowHeadRight { get; set; }
    public PointCollection BodyPoints { get; set; } = new PointCollection(3);

    public bool IsLoopback => from == to;
    public double EllipseRadius { get; set; }
    public Point EllipseMid { get; set; }

    public double Weight
    {
        get => from.Iterator.WeightTo[to.Iterator];
        set
        {
            from.Iterator.WeightTo[to.Iterator] = value;
            OnPropertyChanged(nameof(Weight));
        }
    }

    private ValueSliderSettings _weightSlider;
    public ValueSliderSettings WeightSlider => _weightSlider ??= new()
    {
        Label = "Weight",
        ToolTip = "The weight of the connection controls the transition probability between the two iterators. 0 weight means no connection.",
        DefaultValue = 1.0,
        MinValue = 0,
        Increment = 0.01,
        AnimationPath = $"[{from.Iterator.Id}].[{to.Iterator.Id}]",
        ValueWillChange = _workspace.TakeSnapshot,
        ValueChanged = (v) => _workspace.Renderer.InvalidateHistogramBuffer()
    };

    public ConnectionViewModel(IEnumerable<ConnectionViewModel> nodemapConnections, IteratorViewModel from, IteratorViewModel to, Workspace workspace)
    {
        _nodemapConnections = nodemapConnections;
        this.from = from;
        this.to = to;
        _workspace = workspace;
        from.PropertyChanged += HandleIteratorsChanged;
        to.PropertyChanged += HandleIteratorsChanged;
        _workspace.PropertyChanged += (s, e) => OnPropertyChanged(string.Empty);
    }

    private void HandleIteratorsChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "ConnectionProps")
        {
            OnPropertyChanged(string.Empty);
            OnPropertyChanged("ConnectionProps");
            foreach (var c in _nodemapConnections)
            {//ugh
                //to update neighbour self-connecting arrow visuals
                c.OnPropertyChanged(string.Empty);
                c.OnPropertyChanged("ConnectionProps");
            }
        }
    }

}
