using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using RetailFlow.Models;

namespace RetailFlow.Views.Charts;

/// <summary>Hand-drawn line chart used for the Dashboard's Sales Trend visualization.</summary>
public partial class LineChartControl : ChartControlBase
{
    private static readonly SolidColorBrush LineBrush = new(Color.FromRgb(0x25, 0x63, 0xEB));
    private static readonly SolidColorBrush GridBrush = new(Color.FromRgb(0xE5, 0xE7, 0xEB));
    private static readonly SolidColorBrush LabelBrush = new(Color.FromRgb(0x6B, 0x72, 0x80));

    public LineChartControl()
    {
        InitializeComponent();
    }

    protected override void Redraw()
    {
        if (DrawCanvas is null) return;
        DrawCanvas.Children.Clear();

        var points = ItemsSource?.Cast<SalesTrendPoint>().ToList() ?? new List<SalesTrendPoint>();
        var width = ActualWidth;
        var height = ActualHeight;
        if (points.Count == 0 || width <= 0 || height <= 0) return;

        const double leftMargin = 55;
        const double bottomMargin = 24;
        const double topMargin = 10;
        const double rightMargin = 10;

        var plotWidth = Math.Max(1, width - leftMargin - rightMargin);
        var plotHeight = Math.Max(1, height - topMargin - bottomMargin);

        var maxValue = points.Max(p => p.Total);
        if (maxValue <= 0) maxValue = 1;

        // Horizontal gridlines + Y-axis labels at 0%, 50%, 100% of the max value.
        for (var i = 0; i <= 2; i++)
        {
            var y = topMargin + plotHeight - (plotHeight * i / 2.0);
            DrawCanvas.Children.Add(new Line
            {
                X1 = leftMargin, X2 = leftMargin + plotWidth, Y1 = y, Y2 = y,
                Stroke = GridBrush, StrokeThickness = 1
            });

            var label = new TextBlock { Text = $"Rs. {maxValue * i / 2:N0}", FontSize = 10, Foreground = LabelBrush };
            Canvas.SetLeft(label, 0);
            Canvas.SetTop(label, y - 7);
            DrawCanvas.Children.Add(label);
        }

        double XFor(int index) => points.Count == 1
            ? leftMargin + plotWidth / 2
            : leftMargin + plotWidth * index / (double)(points.Count - 1);
        double YFor(decimal value) => topMargin + plotHeight - (double)(value / maxValue) * plotHeight;

        var polyline = new Polyline { Stroke = LineBrush, StrokeThickness = 2.5, StrokeLineJoin = PenLineJoin.Round };
        for (var i = 0; i < points.Count; i++)
        {
            polyline.Points.Add(new Point(XFor(i), YFor(points[i].Total)));
        }
        DrawCanvas.Children.Add(polyline);

        // Data-point markers plus every date label (thinned out if there isn't room for all of them).
        var labelStride = Math.Max(1, (int)Math.Ceiling(points.Count / (plotWidth / 55.0)));
        for (var i = 0; i < points.Count; i++)
        {
            var x = XFor(i);
            var y = YFor(points[i].Total);

            var dot = new Ellipse { Width = 6, Height = 6, Fill = LineBrush };
            Canvas.SetLeft(dot, x - 3);
            Canvas.SetTop(dot, y - 3);
            DrawCanvas.Children.Add(dot);

            if (i % labelStride == 0 || i == points.Count - 1)
            {
                var dateLabel = new TextBlock
                {
                    Text = points[i].Date.ToString("MMM d"),
                    FontSize = 10,
                    Foreground = LabelBrush
                };
                Canvas.SetLeft(dateLabel, Math.Min(Math.Max(x - 15, leftMargin), leftMargin + plotWidth - 30));
                Canvas.SetTop(dateLabel, topMargin + plotHeight + 4);
                DrawCanvas.Children.Add(dateLabel);
            }
        }
    }
}
