using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using RetailFlow.Models;

namespace RetailFlow.Views.Charts;

/// <summary>
/// Hand-drawn scatter plot used for the Dashboard's Product Stock vs Sales visualization.
/// X = units sold in the selected period, Y = current stock on hand; low-stock products are
/// drawn in red so they stand out regardless of where they land on the axes.
/// </summary>
public partial class ScatterChartControl : ChartControlBase
{
    private static readonly SolidColorBrush NormalBrush = new(Color.FromRgb(0x25, 0x63, 0xEB));
    private static readonly SolidColorBrush LowStockBrush = new(Color.FromRgb(0xDC, 0x26, 0x26));
    private static readonly SolidColorBrush AxisBrush = new(Color.FromRgb(0xE5, 0xE7, 0xEB));
    private static readonly SolidColorBrush LabelBrush = new(Color.FromRgb(0x6B, 0x72, 0x80));

    public ScatterChartControl()
    {
        InitializeComponent();
    }

    protected override void Redraw()
    {
        if (DrawCanvas is null) return;
        DrawCanvas.Children.Clear();

        var points = ItemsSource?.Cast<StockVsSalesPoint>().ToList() ?? new List<StockVsSalesPoint>();
        var width = ActualWidth;
        var height = ActualHeight;
        if (points.Count == 0 || width <= 0 || height <= 0) return;

        const double leftMargin = 34;
        const double bottomMargin = 26;
        const double topMargin = 10;
        const double rightMargin = 10;

        var plotWidth = Math.Max(1, width - leftMargin - rightMargin);
        var plotHeight = Math.Max(1, height - topMargin - bottomMargin);

        var maxX = Math.Max(1, points.Max(p => p.UnitsSold));
        var maxY = Math.Max(1, points.Max(p => p.StockQuantity));

        DrawCanvas.Children.Add(new Line
        {
            X1 = leftMargin, X2 = leftMargin, Y1 = topMargin, Y2 = topMargin + plotHeight,
            Stroke = AxisBrush, StrokeThickness = 1
        });
        DrawCanvas.Children.Add(new Line
        {
            X1 = leftMargin, X2 = leftMargin + plotWidth, Y1 = topMargin + plotHeight, Y2 = topMargin + plotHeight,
            Stroke = AxisBrush, StrokeThickness = 1
        });

        var xAxisLabel = new TextBlock { Text = "Units sold →", FontSize = 9, Foreground = LabelBrush };
        Canvas.SetLeft(xAxisLabel, leftMargin + plotWidth - 70);
        Canvas.SetTop(xAxisLabel, topMargin + plotHeight + 10);
        DrawCanvas.Children.Add(xAxisLabel);

        var yAxisLabel = new TextBlock { Text = "Stock", FontSize = 9, Foreground = LabelBrush };
        Canvas.SetLeft(yAxisLabel, 0);
        Canvas.SetTop(yAxisLabel, topMargin - 2);
        DrawCanvas.Children.Add(yAxisLabel);

        foreach (var point in points)
        {
            var x = leftMargin + (point.UnitsSold / (double)maxX) * plotWidth;
            var y = topMargin + plotHeight - (point.StockQuantity / (double)maxY) * plotHeight;

            var dot = new Ellipse
            {
                Width = 9,
                Height = 9,
                Fill = point.IsLowStock ? LowStockBrush : NormalBrush,
                Opacity = 0.85,
                ToolTip = $"{point.ProductName}\nUnits sold: {point.UnitsSold}\nStock: {point.StockQuantity}" +
                          (point.IsLowStock ? "\n⚠ Low stock" : "")
            };
            Canvas.SetLeft(dot, x - 4.5);
            Canvas.SetTop(dot, y - 4.5);
            DrawCanvas.Children.Add(dot);
        }
    }
}
