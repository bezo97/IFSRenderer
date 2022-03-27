using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WpfDisplay.Helper;

public class BindablePoint
{
    public double X { get; set; }
    public double Y { get; set; }

    public BindablePoint(double x, double y)
    {
        X = x;
        Y = y;
    }

    public static BindablePoint FromPoint(Point p) => new(p.X, p.Y);
}

public static class BindablePointExtensions
{
    public static Point ToPoint(this BindablePoint p) => new() { X = p.X, Y = p.Y };

}
