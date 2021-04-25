using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using IFSEngine.Utility;

namespace WpfDisplay.Controls.Animation.Helper
{
    public class TimelineDrawer
    {
        private readonly double activeAreaStart;
        private readonly double activeAreaEnd;

        public TimelineDrawer(double activeAreaStart,double activeAreaEnd)
        {
            this.activeAreaStart = activeAreaStart;
            this.activeAreaEnd = activeAreaEnd;
        }

        public void DrawLines(Canvas parentCanvas, in bool differentHeight)
        {
            int secs = 10;
            for (int i = 0; i < secs + 1; i++)
            {
                var bigLine = new Line
                {
                    X1 = 0,
                    Y1 = differentHeight ? parentCanvas.ActualHeight / 3 : 0,
                    X2 = 0,
                    Y2 = parentCanvas.ActualHeight,
                    Stroke = Brushes.WhiteSmoke,
                    StrokeThickness = 2,
                    Opacity = 0.5
                };

                PlaceLine(parentCanvas, ((double)i / secs), bigLine);
            }

            for (int i = 0; i < secs; i++)
            {
                var halfLine = new Line
                {
                    X1 = 0,
                    Y1 = differentHeight ? 2 * parentCanvas.ActualHeight / 3 : 0,
                    X2 = 0,
                    Y2 = parentCanvas.ActualHeight,
                    Stroke = Brushes.WhiteSmoke,
                    StrokeThickness = 1,
                    Opacity = 0.5
                };
                PlaceLine(parentCanvas, (double)i / secs + 0.5 / secs, halfLine);


                var quartersLine1 = new Line
                {
                    X1 = 0,
                    Y1 = differentHeight ? 3 * parentCanvas.ActualHeight / 4 : 0,
                    X2 = 0,
                    Y2 = parentCanvas.ActualHeight,
                    Stroke = Brushes.WhiteSmoke,
                    StrokeThickness = 0.5,
                    Opacity = 0.5
                };
                PlaceLine(parentCanvas, ((double)i / secs + 0.25 / secs), quartersLine1);


                var quartersLine2 = new Line
                {
                    X1 = 0,
                    Y1 = differentHeight ? 3 * parentCanvas.ActualHeight / 4 : 0,
                    X2 = 0,
                    Y2 = parentCanvas.ActualHeight,
                    Stroke = Brushes.WhiteSmoke,
                    StrokeThickness = 0.5,
                    Opacity = 0.5
                };
                PlaceLine(parentCanvas, ((double)i / secs + 0.75 / secs), quartersLine2);
            }

        }

        private double MapToActiveArea(double normalizedOriginalValue) => normalizedOriginalValue.Remap(0, 1, activeAreaStart, activeAreaEnd);

        private void PlaceLine(Canvas parent, double normalizedLeftOffset, Line lineToPlace)
        {
            Canvas.SetLeft(lineToPlace, MapToActiveArea(normalizedLeftOffset) * parent.ActualWidth);
            parent.Children.Insert(0, lineToPlace);
        }
    }
}
