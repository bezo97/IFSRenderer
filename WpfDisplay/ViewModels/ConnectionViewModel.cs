using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace WpfDisplay.ViewModels
{
    //TODO: cleanup & refactor
    public class ConnectionViewModel : ObservableObject
    {

        public readonly IteratorViewModel from;
        public readonly IteratorViewModel to;//TODO: replace with toPoint

        public Geometry ArrowBody { get; private set; }
        public Geometry ArrowHeadLeft { get; private set; }
        public Geometry ArrowHeadRight { get; private set; }

        public ConnectionViewModel(IteratorViewModel from, IteratorViewModel to)
        {
            this.from = from;
            this.to = to;
            from.PropertyChanged += handlePositionsChanged;
            to.PropertyChanged += handlePositionsChanged;
            UpdateGeometry();
        }

        private void handlePositionsChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName=="XCoord" || e.PropertyName == "YCoord")
            {
                UpdateGeometry();
            }
        }

        public void UpdateGeometry()
        {
            if (from == to)
                CalcGeometryToSelf();
            else
                CalcGeometryByPoints();
            RaisePropertyChanged(() => ArrowBody);
            RaisePropertyChanged(() => ArrowHeadLeft);
            RaisePropertyChanged(() => ArrowHeadRight);
        }

        private void CalcGeometryByPoints()
        {
            Point p1 = new Point(from.XCoord, from.YCoord);
            Point p2 = new Point(to.XCoord, to.YCoord);

            double xdir = p2.X - p1.X;
            double ydir = p2.Y - p1.Y;
            double angle = Math.Atan2(ydir, xdir) + Math.PI / 4;//TODO: make this a setting?

            PathSegmentCollection seg = new PathSegmentCollection(1);
            seg.Add(new PolyBezierSegment(new PointCollection(3) {
                new Point((p1.X * 2 + p2.X) / 3 + 30*Math.Cos(angle), (p1.Y * 2 + p2.Y) / 3 + 30*Math.Sin(angle)),
                new Point((p1.X + p2.X * 2) / 3 + 30*Math.Cos(angle), (p1.Y + p2.Y * 2) / 3 + 30*Math.Sin(angle)), p2 }, true));

            ArrowBody = new PathGeometry(new PathFigureCollection { new PathFigure(p1, seg, false) });
            
            //Find middle of the arrow body. TODO: find better way
            PathGeometry flattened = ArrowBody.GetFlattenedPathGeometry();//bezier -> line path
            double minL = 9999;
            PointCollection ffig = ((PolyLineSegment)flattened.Figures[0].Segments[0]).Points;
            double halfX = (ffig[0].X + ffig[ffig.Count - 1].X) / 2;
            double halfY = (ffig[0].Y + ffig[ffig.Count - 1].Y) / 2;
            int iP = 0;
            for (; iP < ffig.Count; iP++)
            {
                double nextL = Math.Min(minL, Math.Sqrt(Math.Pow(halfX - ffig[iP].X, 2) + Math.Pow(halfY - ffig[iP].Y, 2)));
                if (nextL < minL)
                    minL = nextL;
                else
                    break;
            }

            //calc arrow head
            Point mid = ffig[iP];
            Point prev = ffig[iP - 1];
            Point dir = new Point(mid.X - prev.X, mid.Y - prev.Y);
            angle = Math.Atan2(dir.Y, dir.X);
            ArrowHeadLeft = new LineGeometry(mid, new Point(mid.X - Math.Cos(angle + 0.5) * IteratorViewModel.BaseSize / 5.0, mid.Y - Math.Sin(angle + 0.5) * IteratorViewModel.BaseSize / 5.0));
            ArrowHeadRight = new LineGeometry(mid, new Point(mid.X - Math.Cos(angle - 0.5) * IteratorViewModel.BaseSize / 5.0, mid.Y - Math.Sin(angle - 0.5) * IteratorViewModel.BaseSize / 5.0));
        }

        private void CalcGeometryToSelf()
        {
            //calc loopback angle
            double dirx = 0;
            double diry = 0;
            foreach (ConnectionViewModel c in from.ConnectionViewModels)
            {
                if (from != c.to)
                {
                    dirx += c.to.XCoord - from.XCoord;
                    diry += c.to.YCoord - from.YCoord;
                }
            }
            double loopbackAngle = Math.Atan2(-diry, -dirx);

            double r = from.WeightedSize / 2.0 / 5.0 * 4.0;
            double cosa = Math.Cos(loopbackAngle);
            double sina = Math.Sin(loopbackAngle);
            Point mid = new Point(from.XCoord + r * cosa, from.YCoord + r * sina);
            ArrowBody = new EllipseGeometry(mid, r, r);
            mid = new Point(from.XCoord + 2 * r * cosa, from.YCoord + 2 * r * sina);
            double a = Math.Atan2(sina, cosa) - 3.1415 / 4.0;
            cosa = Math.Cos(a);
            sina = Math.Sin(a);
            ArrowHeadLeft = new LineGeometry(mid, new Point(mid.X - IteratorViewModel.BaseSize / 5.0 * cosa, mid.Y - IteratorViewModel.BaseSize / 5.0 * sina));
            ArrowHeadRight = new LineGeometry(mid, new Point(mid.X - IteratorViewModel.BaseSize / 5.0 * sina, mid.Y + IteratorViewModel.BaseSize / 5.0 * cosa));
        }

        public double Weight => from.iterator.WeightTo[to.iterator];
    }
}
