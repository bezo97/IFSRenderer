#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WpfDisplay.ViewModels;

namespace WpfDisplay.Views;

/// <summary>
/// Interaction logic for NodeMap.xaml
/// </summary>
public partial class NodeMap : UserControl
{
    IFSViewModel? vm => DataContext as IFSViewModel;

    public NodeMap()
    {
        InitializeComponent();
    }

    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);
        if (vm?.ConnectingIterator is not null)
        {
            dragArrow.EndPoint = Mouse.GetPosition(wrapperGrid);
            dragArrow.UpdateGeometry();
        }
    }

    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseDown(e);
        //if (vm?.ConnectingIterator is not null)
        {
            dragArrow.EndPoint = Mouse.GetPosition(wrapperGrid);
            dragArrow.UpdateGeometry();
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if(vm is not null)
            vm.ConnectingIterator = null;
    }

    internal double CalculateLoopbackAngle(Point p)
    {
        IEnumerable<ConnectionViewModel> arrows = vm!.ConnectionViewModels;
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
