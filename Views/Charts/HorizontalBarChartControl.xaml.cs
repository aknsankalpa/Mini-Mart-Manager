using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using RetailFlow.Models;

namespace RetailFlow.Views.Charts;

/// <summary>Hand-drawn horizontal bar chart used for the Dashboard's Top Selling Products visualization.</summary>
public partial class HorizontalBarChartControl : ChartControlBase
{
    private static readonly SolidColorBrush BarBrush = new(Color.FromRgb(0x7C, 0x3A, 0xED));
    private static readonly SolidColorBrush LabelBrush = new(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly SolidColorBrush ValueLabelBrush = new(Color.FromRgb(0x6B, 0x72, 0x80));

    public HorizontalBarChartControl()
    {
        InitializeComponent();
    }

    protected override void Redraw()
    {
        if (DrawCanvas is null) return;
        DrawCanvas.Children.Clear();

        var bars = ItemsSource?.Cast<TopSellingProduct>().ToList() ?? new List<TopSellingProduct>();
        var width = ActualWidth;
        var height = ActualHeight;
        if (bars.Count == 0 || width <= 0 || height <= 0) return;

        const double nameColumnWidth = 110;
        const double rightMargin = 55;
        var plotWidth = Math.Max(1, width - nameColumnWidth - rightMargin);

        var rowHeight = height / bars.Count;
        var barHeight = Math.Max(6, Math.Min(24, rowHeight * 0.55));

        var maxUnits = bars.Max(b => b.UnitsSold);
        if (maxUnits <= 0) maxUnits = 1;

        for (var i = 0; i < bars.Count; i++)
        {
            var rowCenter = i * rowHeight + rowHeight / 2;
            var barWidth = (double)bars[i].UnitsSold / maxUnits * plotWidth;

            var nameLabel = new TextBlock
            {
                Text = bars[i].ProductName,
                FontSize = 11,
                Foreground = LabelBrush,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Width = nameColumnWidth - 8,
                TextAlignment = TextAlignment.Right
            };
            Canvas.SetLeft(nameLabel, 0);
            Canvas.SetTop(nameLabel, rowCenter - 8);
            DrawCanvas.Children.Add(nameLabel);

            var rect = new Rectangle
            {
                Width = Math.Max(2, barWidth),
                Height = barHeight,
                Fill = BarBrush,
                RadiusX = 3,
                RadiusY = 3
            };
            Canvas.SetLeft(rect, nameColumnWidth);
            Canvas.SetTop(rect, rowCenter - barHeight / 2);
            DrawCanvas.Children.Add(rect);

            var valueLabel = new TextBlock
            {
                Text = $"{bars[i].UnitsSold} units",
                FontSize = 10,
                Foreground = ValueLabelBrush
            };
            Canvas.SetLeft(valueLabel, nameColumnWidth + Math.Max(2, barWidth) + 6);
            Canvas.SetTop(valueLabel, rowCenter - 7);
            DrawCanvas.Children.Add(valueLabel);
        }
    }
}
