using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

/// <summary>
/// Interaction logic for NodeMap.xaml
/// </summary>
public partial class NodeMap : UserControl
{
    public NodeMap()
    {
        InitializeComponent();
    }

    internal double CalculateLoopbackAngle(Point p)
    {
        IEnumerable<ConnectionViewModel> arrows = itemsControl.ItemContainerGenerator.Items.OfType<ConnectionViewModel>();//TODO: ugh, nicer
        Vector dir;
        foreach (var c in arrows.Where(nc => nc.StartPoint == p || nc.EndPoint == p))
        {
            Vector of = new Vector(
                c.StartPoint.X + c.EndPoint.X,
                c.StartPoint.Y + c.EndPoint.Y
                )/2;
            dir += of - (Vector)p;
        }
        return Math.Atan2(-dir.Y, -dir.X);
    }

}
