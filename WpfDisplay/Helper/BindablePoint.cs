using System.Windows;

namespace WpfDisplay.Helper;

public class BindablePoint(double x, double y)
{
    public double X { get; set; } = x;
    public double Y { get; set; } = y;

    public static BindablePoint FromPoint(Point p) => new(p.X, p.Y);
}

public static class BindablePointExtensions
{
    public static Point ToPoint(this BindablePoint p) => new() { X = p.X, Y = p.Y };

}
