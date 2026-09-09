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
///
/// Most products have zero sales in a short period (e.g. "Today"), so a plain scatter tends
/// to pile many points up at x = 0, several of them sitting on exactly the same stock value
/// (and therefore exactly on top of each other). To keep that readable, points that share the
/// same (UnitsSold, StockQuantity) are nudged apart slightly (jittered) rather than drawn as a
/// single indistinguishable dot. Product names aren't printed next to every dot — in a chart
/// this compact that many labels collide far more than they clarify — so identification is by
/// hovering a dot (its tooltip has the exact name, units sold, and stock) or by color, backed
/// by the legend.
/// </summary>
public partial class ScatterChartControl : ChartControlBase
{
    private static readonly SolidColorBrush NormalBrush = new(Color.FromRgb(0x25, 0x63, 0xEB));
    private static readonly SolidColorBrush LowStockBrush = new(Color.FromRgb(0xDC, 0x26, 0x26));
    private static readonly SolidColorBrush AxisBrush = new(Color.FromRgb(0xE5, 0xE7, 0xEB));
    private static readonly SolidColorBrush GridBrush = new(Color.FromRgb(0xF3, 0xF4, 0xF6));
    private static readonly SolidColorBrush LabelBrush = new(Color.FromRgb(0x6B, 0x72, 0x80));
    private static readonly SolidColorBrush TitleBrush = new(Color.FromRgb(0x37, 0x41, 0x51));

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

        const double leftMargin = 42;
        const double bottomMargin = 34;
        const double topMargin = 24;
        const double rightMargin = 16;

        var plotWidth = Math.Max(1, width - leftMargin - rightMargin);
        var plotHeight = Math.Max(1, height - topMargin - bottomMargin);

        var maxX = Math.Max(1, points.Max(p => p.UnitsSold));
        var maxY = Math.Max(1, points.Max(p => p.StockQuantity));

        // --- Legend (top-right): what the two dot colors mean ---
        var legendX = leftMargin + plotWidth - 118;
        AddLegendEntry(legendX, 2, NormalBrush, "Normal stock");
        AddLegendEntry(legendX, 16, LowStockBrush, "Low stock");

        // --- Gridlines + numeric ticks on both axes (0%, 50%, 100% of the max) ---
        for (var i = 0; i <= 2; i++)
        {
            var y = topMargin + plotHeight - (plotHeight * i / 2.0);
            if (i > 0)
            {
                DrawCanvas.Children.Add(new Line
                {
                    X1 = leftMargin, X2 = leftMargin + plotWidth, Y1 = y, Y2 = y,
                    Stroke = GridBrush, StrokeThickness = 1
                });
            }
            var yLabel = new TextBlock { Text = $"{Math.Round(maxY * i / 2.0)}", FontSize = 9, Foreground = LabelBrush };
            Canvas.SetLeft(yLabel, 2);
            Canvas.SetTop(yLabel, y - 6);
            DrawCanvas.Children.Add(yLabel);

            var x = leftMargin + (plotWidth * i / 2.0);
            var xLabel = new TextBlock { Text = $"{Math.Round(maxX * i / 2.0)}", FontSize = 9, Foreground = LabelBrush };
            Canvas.SetLeft(xLabel, x - (i == 2 ? 14 : 6));
            Canvas.SetTop(xLabel, topMargin + plotHeight + 4);
            DrawCanvas.Children.Add(xLabel);
        }

        // --- Axes ---
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

        var xAxisTitle = new TextBlock { Text = "Units Sold", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = TitleBrush };
        Canvas.SetLeft(xAxisTitle, leftMargin + plotWidth / 2 - 28);
        Canvas.SetTop(xAxisTitle, topMargin + plotHeight + 17);
        DrawCanvas.Children.Add(xAxisTitle);

        var yAxisTitle = new TextBlock { Text = "Stock Qty", FontSize = 10, FontWeight = FontWeights.SemiBold, Foreground = TitleBrush };
        Canvas.SetLeft(yAxisTitle, 0);
        Canvas.SetTop(yAxisTitle, topMargin - 20);
        DrawCanvas.Children.Add(yAxisTitle);

        // --- Points, jittered apart when several products share the exact same spot ---
        var groups = points.GroupBy(p => (p.UnitsSold, p.StockQuantity));
        foreach (var group in groups)
        {
            var members = group.ToList();
            var baseX = leftMargin + (group.Key.UnitsSold / (double)maxX) * plotWidth;
            var baseY = topMargin + plotHeight - (group.Key.StockQuantity / (double)maxY) * plotHeight;

            for (var i = 0; i < members.Count; i++)
            {
                // Fan overlapping points out around their shared position instead of stacking
                // them exactly on top of one another, where only the topmost would be visible.
                var offset = members.Count == 1 ? 0 : (i - (members.Count - 1) / 2.0) * 8;
                var x = baseX + offset;
                var y = baseY;
                var point = members[i];

                var dot = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = point.IsLowStock ? LowStockBrush : NormalBrush,
                    Stroke = Brushes.White,
                    StrokeThickness = 1,
                    Opacity = 0.9,
                    ToolTip = $"{point.ProductName}\nUnits sold: {point.UnitsSold}\nStock: {point.StockQuantity}" +
                              (point.IsLowStock ? "\n⚠ Low stock" : "")
                };
                Canvas.SetLeft(dot, x - 5);
                Canvas.SetTop(dot, y - 5);
                DrawCanvas.Children.Add(dot);
            }
        }
    }

    private void AddLegendEntry(double x, double y, SolidColorBrush color, string text)
    {
        var dot = new Ellipse { Width = 8, Height = 8, Fill = color };
        Canvas.SetLeft(dot, x);
        Canvas.SetTop(dot, y + 2);
        DrawCanvas.Children.Add(dot);

        var label = new TextBlock { Text = text, FontSize = 9, Foreground = LabelBrush };
        Canvas.SetLeft(label, x + 12);
        Canvas.SetTop(label, y);
        DrawCanvas.Children.Add(label);
    }
}
