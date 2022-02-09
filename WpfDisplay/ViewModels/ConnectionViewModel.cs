using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using WpfDisplay.Models;

namespace WpfDisplay.ViewModels;

[ObservableObject]
public partial class ConnectionViewModel
{
    private readonly IEnumerable<ConnectionViewModel> _nodemapConnections;
    public readonly IteratorViewModel from;
    public readonly IteratorViewModel to;
    private readonly Workspace _workspace;

    [ObservableProperty] private bool _isSelected;

    public Point StartPoint => new(from.XCoord, from.YCoord);
    public Point EndPoint => new(to.XCoord, to.YCoord);
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
        from.PropertyChanged += HandlePositionsChanged;
        to.PropertyChanged += HandlePositionsChanged;
        UpdateGeometry();
    }

    private void HandlePositionsChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "NodePosition")
        {
            UpdateGeometry();
        }
    }

    public void UpdateGeometry()
    {
        if (IsLoopback)
            CalcGeometryToSelf();
        else
            CalcGeometryByPoints();

        OnPropertyChanged(string.Empty);
    }

    private void CalcGeometryByPoints()
    {
        Point p1 = new(from.XCoord, from.YCoord);
        Point p2 = new(to.XCoord, to.YCoord);

        double xdir = p2.X - p1.X;
        double ydir = p2.Y - p1.Y;
        double angle = Math.Atan2(ydir, xdir) + Math.PI / 4;//TODO: make this a setting?
        double cosa = Math.Cos(angle);
        double sina = Math.Sin(angle);

        //Must create a new collection every time:
        //https://stackoverflow.com/questions/21618046/wpf-path-binding-to-pointcollection-not-updating-ui
        //BodyPoints.Clear();
        BodyPoints = new(3)
        {
            new Point(
                (p1.X * 2 + p2.X) / 3 + 30 * cosa,
                (p1.Y * 2 + p2.Y) / 3 + 30 * sina),
            new Point(
                (p1.X + p2.X * 2) / 3 + 30 * cosa,
                (p1.Y + p2.Y * 2) / 3 + 30 * sina),
            p2
        };
        BodyPoints.Freeze();


        Point mid = new(
            (p1.X + p2.X) / 2 + 22 * cosa,
            (p1.Y + p2.Y) / 2 + 22 * sina
        );
        Point dir = new(p2.X - p1.X, p2.Y - p1.Y);
        angle = Math.Atan2(dir.Y, dir.X);

        ArrowHeadMid = mid;
        ArrowHeadLeft = new Point(
            mid.X - Math.Cos(angle + 0.5) * IteratorViewModel.BaseSize / 5.0,
            mid.Y - Math.Sin(angle + 0.5) * IteratorViewModel.BaseSize / 5.0);
        ArrowHeadRight = new Point(
            mid.X - Math.Cos(angle - 0.5) * IteratorViewModel.BaseSize / 5.0,
            mid.Y - Math.Sin(angle - 0.5) * IteratorViewModel.BaseSize / 5.0);
    }

    private void CalcGeometryToSelf()
    {
        //calc loopback angle
        double dirx = 0;
        double diry = 0;
        foreach (ConnectionViewModel c in _nodemapConnections.Where(nc => nc.from == from || nc.to == from))
        {
            dirx += c.to.XCoord - from.XCoord;
            diry += c.to.YCoord - from.YCoord;
        }
        double loopbackAngle = Math.Atan2(-diry, -dirx);

        double r = from.WeightedSize / 2.0 / 5.0 * 4.0;
        double cosa = Math.Cos(loopbackAngle);
        double sina = Math.Sin(loopbackAngle);
        Point emid = new(from.XCoord + r * cosa, from.YCoord + r * sina);
        EllipseMid = emid;
        EllipseRadius = r;

        Point mid = new(from.XCoord + 2 * r * cosa, from.YCoord + 2 * r * sina);
        double a = Math.Atan2(sina, cosa) - 3.1415 / 4.0;
        cosa = Math.Cos(a);
        sina = Math.Sin(a);
        ArrowHeadMid = mid;
        ArrowHeadLeft = new Point(
            mid.X - IteratorViewModel.BaseSize / 5.0 * cosa,
            mid.Y - IteratorViewModel.BaseSize / 5.0 * sina);
        ArrowHeadRight = new Point(
            mid.X - IteratorViewModel.BaseSize / 5.0 * sina,
            mid.Y + IteratorViewModel.BaseSize / 5.0 * cosa);
    }

}
