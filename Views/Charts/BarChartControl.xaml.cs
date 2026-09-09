using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using RetailFlow.Models;

namespace RetailFlow.Views.Charts;

/// <summary>Hand-drawn vertical bar chart used for the Dashboard's Sales by Category visualization.</summary>
public partial class BarChartControl : ChartControlBase
{
    private static readonly SolidColorBrush BarBrush = new(Color.FromRgb(0x25, 0x63, 0xEB));
    private static readonly SolidColorBrush LabelBrush = new(Color.FromRgb(0x6B, 0x72, 0x80));
    private static readonly SolidColorBrush ValueLabelBrush = new(Color.FromRgb(0x11, 0x18, 0x27));

    public BarChartControl()
    {
        InitializeComponent();
    }

    protected override void Redraw()
    {
        if (DrawCanvas is null) return;
        DrawCanvas.Children.Clear();

        var bars = ItemsSource?.Cast<CategorySales>().ToList() ?? new List<CategorySales>();
        var width = ActualWidth;
        var height = ActualHeight;
        if (bars.Count == 0 || width <= 0 || height <= 0) return;

        const double bottomMargin = 30;
        const double topMargin = 20;
        var plotHeight = Math.Max(1, height - topMargin - bottomMargin);

        var maxValue = bars.Max(b => b.Total);
        if (maxValue <= 0) maxValue = 1;

        var slot = width / bars.Count;
        var barWidth = Math.Max(4, Math.Min(48, slot * 0.55));

        for (var i = 0; i < bars.Count; i++)
        {
            var barHeight = (double)(bars[i].Total / maxValue) * plotHeight;
            var x = i * slot + (slot - barWidth) / 2;
            var y = topMargin + plotHeight - barHeight;

            var rect = new Rectangle
            {
                Width = barWidth,
                Height = Math.Max(0, barHeight),
                Fill = BarBrush,
                RadiusX = 3,
                RadiusY = 3
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            DrawCanvas.Children.Add(rect);

            var valueLabel = new TextBlock
            {
                Text = $"Rs. {bars[i].Total:N0}",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = ValueLabelBrush
            };
            Canvas.SetLeft(valueLabel, x + barWidth / 2 - 20);
            Canvas.SetTop(valueLabel, Math.Max(0, y - 16));
            DrawCanvas.Children.Add(valueLabel);

            var nameLabel = new TextBlock
            {
                Text = bars[i].Category,
                FontSize = 10,
                Foreground = LabelBrush,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Width = slot - 4,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(nameLabel, i * slot + 2);
            Canvas.SetTop(nameLabel, topMargin + plotHeight + 6);
            DrawCanvas.Children.Add(nameLabel);
        }
    }
}
