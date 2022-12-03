using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using WpfDisplay.Models;
using WpfDisplay.Helper;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class ConnectionViewModel
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

    public bool IsLoopback => (from == to);
    public double EllipseRadius { get; set; }
    public Point EllipseMid { get; set; }
    
    private ValueSliderViewModel _weight;
    public ValueSliderViewModel Weight => _weight ??= new ValueSliderViewModel(_workspace)
    {
        Label = "Weight",
        ToolTip = "The weight of the connection controls the transition probability between the two iterators. 0 weight means no connection.",
        DefaultValue = 1.0,
        GetV = () => from.iterator.WeightTo[to.iterator],
        SetV = (value) => {
            from.iterator.WeightTo[to.iterator] = value;
            _workspace.Renderer.InvalidateHistogramBuffer();
            OnPropertyChanged(nameof(Weight));
        },
        MinValue = 0,
        Increment = 0.01,
        ValueWillChange = _workspace.TakeSnapshot,
    };

    public ConnectionViewModel(IEnumerable<ConnectionViewModel> nodemapConnections, IteratorViewModel from, IteratorViewModel to, Workspace workspace)
    {
        this._nodemapConnections = nodemapConnections;
        this.from = from;
        this.to = to;
        this._workspace = workspace;
        from.PropertyChanged += HandleIteratorsChanged;
        to.PropertyChanged += HandleIteratorsChanged;
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
