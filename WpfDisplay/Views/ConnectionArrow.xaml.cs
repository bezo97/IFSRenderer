using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

/// <summary>
/// Interaction logic for ConnectionArrow.xaml
/// </summary>
public partial class ConnectionArrow : UserControl
{

    public ConnectionArrow()
    {
        InitializeComponent();

        DataContextChanged += (s, e) =>
        {//update geometry when nodes change position
            ((INotifyPropertyChanged)e.NewValue).PropertyChanged += (s2, e2) => {
                if (e2.PropertyName == "ConnectionProps")
                    UpdateGeometry();
            };
            if(ParentNodeMap is not null)
                UpdateGeometry();
        };

        Loaded += (s, e) => UpdateGeometry();

    }

    public Point StartPoint
    {
        get { return (Point)GetValue(StartPointProperty); }
        set { SetValue(StartPointProperty, value); UpdateGeometry(); }
    }
    public static readonly DependencyProperty StartPointProperty =
        DependencyProperty.Register("StartPoint", typeof(Point), typeof(ConnectionArrow), new PropertyMetadata(null));

    public Point EndPoint
    {
        get { return (Point)GetValue(EndPointProperty); }
        set { SetValue(EndPointProperty, value); UpdateGeometry(); }
    }
    public static readonly DependencyProperty EndPointProperty =
        DependencyProperty.Register("EndPoint", typeof(Point), typeof(ConnectionArrow), new PropertyMetadata(null));

    public double ArrowHeadSize
    {
        get { return (double)GetValue(ArrowHeadSizeProperty); }
        set { SetValue(ArrowHeadSizeProperty, value); }
    }
    public static readonly DependencyProperty ArrowHeadSizeProperty =
        DependencyProperty.Register("ArrowHeadSize", typeof(double), typeof(ConnectionArrow), new PropertyMetadata(20.0));

    public double ArrowCurve
    {
        get { return (double)GetValue(ArrowCurveProperty); }
        set { SetValue(ArrowCurveProperty, value); }
    }
    public static readonly DependencyProperty ArrowCurveProperty =
        DependencyProperty.Register("ArrowCurve", typeof(double), typeof(ConnectionArrow), new PropertyMetadata(Math.PI/4.0));

    public RelayCommand<ConnectionViewModel> SelectCommand
    {
        get { return (RelayCommand<ConnectionViewModel>)GetValue(SelectCommandProperty); }
        set { SetValue(SelectCommandProperty, value); }
    }
    public static readonly DependencyProperty SelectCommandProperty =
        DependencyProperty.Register("SelectCommand", typeof(RelayCommand<ConnectionViewModel>), typeof(ConnectionArrow), new PropertyMetadata(null));

    public bool IsLoopback => (StartPoint == EndPoint);

    public NodeMap ParentNodeMap
    {
        get { return (NodeMap)GetValue(ParentNodeMapProperty); }
        set { SetValue(ParentNodeMapProperty, value); }
    }
    public static readonly DependencyProperty ParentNodeMapProperty =
        DependencyProperty.Register("ParentNodeMap", typeof(NodeMap), typeof(ConnectionArrow), new PropertyMetadata(null));

    public void UpdateGeometry()
    {
        if (IsLoopback)
            CalcGeometryToSelf();
        else
            CalcGeometryByPoints();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        SelectCommand?.Execute(DataContext);
    }

    private void CalcGeometryByPoints()
    {
        Point p1 = StartPoint;
        Point p2 = EndPoint;

        double xdir = p2.X - p1.X;
        double ydir = p2.Y - p1.Y;
        double angle = Math.Atan2(ydir, xdir) + ArrowCurve;
        double cosa = Math.Cos(angle);
        double sina = Math.Sin(angle);

        var pg = new PathGeometry(new List<PathFigure> { new PathFigure(StartPoint, new List<PathSegment>{new PolyBezierSegment(new List<Point>
        {
                new Point(
                    (p1.X * 2 + p2.X) / 3 + 30 * cosa,
                    (p1.Y * 2 + p2.Y) / 3 + 30 * sina),
                new Point(
                    (p1.X + p2.X * 2) / 3 + 30 * cosa,
                    (p1.Y + p2.Y * 2) / 3 + 30 * sina),
                p2
        }, true)}, false) });
        pg.Freeze();
        arrowBody.Data = pg;
        arrowClickArea.Data = pg;


        Point mid = new(
            (p1.X + p2.X) / 2 + 22 * cosa,
            (p1.Y + p2.Y) / 2 + 22 * sina
        );
        Point dir = new(p2.X - p1.X, p2.Y - p1.Y);
        angle = Math.Atan2(dir.Y, dir.X);

        LineGeometry arrowHeadLeft = new LineGeometry(mid, new Point(
            mid.X - Math.Cos(angle + 0.5) * ArrowHeadSize,
            mid.Y - Math.Sin(angle + 0.5) * ArrowHeadSize));
        LineGeometry arrowHeadRight = new LineGeometry(mid, new Point(
            mid.X - Math.Cos(angle - 0.5) * ArrowHeadSize,
            mid.Y - Math.Sin(angle - 0.5) * ArrowHeadSize));
        GeometryGroup arrowHeadGeometry = new GeometryGroup { Children = { arrowHeadLeft, arrowHeadRight } };
        arrowHead.Data = arrowHeadGeometry;
    }

    private void CalcGeometryToSelf()
    {
        if (DataContext is not ConnectionViewModel vm)
            return;
        double loopbackAngle = ParentNodeMap?.CalculateLoopbackAngle(StartPoint) ?? 0;

        double r = vm.from.NodeSize * 0.4;
        double cosa = Math.Cos(loopbackAngle);
        double sina = Math.Sin(loopbackAngle);
        Point emid = StartPoint + r * new Vector(cosa, sina);
        var pg = new EllipseGeometry(emid, r, r);
        pg.Freeze();
        arrowBody.Data = pg;
        arrowClickArea.Data = pg;

        Point mid = StartPoint + 2.0f * r * new Vector(cosa, sina);
        double a = Math.Atan2(sina, cosa) - 3.1415 / 4.0;
        cosa = Math.Cos(a);
        sina = Math.Sin(a);

        LineGeometry arrowHeadLeft = new LineGeometry(mid, new Point(
            mid.X - ArrowHeadSize * cosa,
            mid.Y - ArrowHeadSize * sina));
        LineGeometry arrowHeadRight = new LineGeometry(mid, new Point(
            mid.X - ArrowHeadSize * sina,
            mid.Y + ArrowHeadSize * cosa));
        GeometryGroup arrowHeadGeometry = new GeometryGroup { Children = { arrowHeadLeft, arrowHeadRight } };
        arrowHead.Data = arrowHeadGeometry;
    }

}
