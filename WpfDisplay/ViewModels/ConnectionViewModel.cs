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
    public class ConnectionViewModel : ObservableObject
    {

        public readonly IteratorViewModel from;
        public readonly IteratorViewModel to;//TODO: replace with toPoint

        public Point StartPoint => new Point(from.XCoord, from.YCoord);
        public Point EndPoint => new Point(to.XCoord, to.YCoord);
        public Point ArrowHeadMid { get; set; }
        public Point ArrowHeadLeft { get; set; }
        public Point ArrowHeadRight { get; set; }
        public PointCollection BodyPoints { get; set; } //not observable??

        public bool IsLoopback => (from == to);
        public double EllipseRadius { get; set; }
        public Point EllipseMid { get; set; }

        public double Weight => from.iterator.WeightTo[to.iterator];

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
            if (IsLoopback)
                CalcGeometryToSelf();
            else
                CalcGeometryByPoints();

            RaisePropertyChanged(() => ArrowHeadMid);
            RaisePropertyChanged(() => ArrowHeadLeft);
            RaisePropertyChanged(() => ArrowHeadRight);
            RaisePropertyChanged(() => StartPoint);
            RaisePropertyChanged(() => EndPoint);
            RaisePropertyChanged(() => BodyPoints);
            RaisePropertyChanged(() => EllipseMid);
            RaisePropertyChanged(() => EllipseRadius);
        }

        private void CalcGeometryByPoints()
        {
            Point p1 = new Point(from.XCoord, from.YCoord);
            Point p2 = new Point(to.XCoord, to.YCoord);

            double xdir = p2.X - p1.X;
            double ydir = p2.Y - p1.Y;
            double angle = Math.Atan2(ydir, xdir) + Math.PI / 4;//TODO: make this a setting?

            BodyPoints = new PointCollection(3)
            {
                new Point(
                    (p1.X * 2 + p2.X) / 3 + 30 * Math.Cos(angle),
                    (p1.Y * 2 + p2.Y) / 3 + 30 * Math.Sin(angle)),
                new Point(
                    (p1.X + p2.X * 2) / 3 + 30 * Math.Cos(angle),
                    (p1.Y + p2.Y * 2) / 3 + 30 * Math.Sin(angle)),
                p2
            };

            Point mid = new Point(
                (p1.X + p2.X) / 2 + 22 * Math.Cos(angle),
                (p1.Y + p2.Y) / 2 + 22 * Math.Sin(angle));
            Point dir = new Point(p2.X - p1.X, p2.Y - p1.Y);
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
            Point emid = new Point(from.XCoord + r * cosa, from.YCoord + r * sina);
            EllipseMid = emid;
            EllipseRadius = r;

            Point mid = new Point(from.XCoord + 2 * r * cosa, from.YCoord + 2 * r * sina);
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
}
