using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using RetailFlow.Models;

namespace RetailFlow.Views.Charts;

/// <summary>
/// Hand-drawn Day-of-Week x hour-of-day grid used for the Dashboard's Sales Heatmap.
/// Hours are bucketed into six 4-hour windows (rather than one column per hour) so the
/// grid stays readable at the size the no-scroll dashboard layout allows.
/// </summary>
public partial class HeatmapControl : ChartControlBase
{
    private static readonly DayOfWeek[] DayOrder =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    };

    private static readonly int[] HourBuckets = { 0, 4, 8, 12, 16, 20 };

    private static readonly SolidColorBrush EmptyCellBrush = new(Color.FromRgb(0xF3, 0xF4, 0xF6));
    private static readonly SolidColorBrush LabelBrush = new(Color.FromRgb(0x6B, 0x72, 0x80));
    private static readonly Color HeatColor = Color.FromRgb(0x25, 0x63, 0xEB);

    public HeatmapControl()
    {
        InitializeComponent();
    }

    private static string BucketLabel(int hour) => hour switch
    {
        0 => "12–4am",
        4 => "4–8am",
        8 => "8am–12pm",
        12 => "12–4pm",
        16 => "4–8pm",
        _ => "8pm–12am"
    };

    protected override void Redraw()
    {
        if (DrawCanvas is null) return;
        DrawCanvas.Children.Clear();

        var cells = ItemsSource?.Cast<HeatmapCell>().ToList() ?? new List<HeatmapCell>();
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0) return;

        const double rowLabelWidth = 62;
        const double headerHeight = 18;

        var gridWidth = Math.Max(1, width - rowLabelWidth);
        var gridHeight = Math.Max(1, height - headerHeight);
        var colWidth = gridWidth / DayOrder.Length;
        var rowHeight = gridHeight / HourBuckets.Length;

        var maxValue = cells.Count > 0 ? cells.Max(c => c.Total) : 0;
        if (maxValue <= 0) maxValue = 1;

        for (var d = 0; d < DayOrder.Length; d++)
        {
            var dayLabel = new TextBlock
            {
                Text = DayOrder[d].ToString()[..3],
                FontSize = 10,
                Foreground = LabelBrush,
                Width = colWidth,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(dayLabel, rowLabelWidth + d * colWidth);
            Canvas.SetTop(dayLabel, 0);
            DrawCanvas.Children.Add(dayLabel);
        }

        for (var h = 0; h < HourBuckets.Length; h++)
        {
            var hourLabel = new TextBlock
            {
                Text = BucketLabel(HourBuckets[h]),
                FontSize = 9,
                Foreground = LabelBrush
            };
            Canvas.SetLeft(hourLabel, 0);
            Canvas.SetTop(hourLabel, headerHeight + h * rowHeight + rowHeight / 2 - 6);
            DrawCanvas.Children.Add(hourLabel);

            for (var d = 0; d < DayOrder.Length; d++)
            {
                var match = cells.FirstOrDefault(c => c.Day == DayOrder[d] && c.HourBucketStart == HourBuckets[h]);
                var intensity = match is null ? 0.0 : (double)(match.Total / maxValue);

                var fill = intensity <= 0
                    ? (Brush)EmptyCellBrush
                    : new SolidColorBrush(Color.FromArgb(
                        (byte)(40 + intensity * 215), HeatColor.R, HeatColor.G, HeatColor.B));

                var cell = new Rectangle
                {
                    Width = Math.Max(0, colWidth - 3),
                    Height = Math.Max(0, rowHeight - 3),
                    Fill = fill,
                    RadiusX = 3,
                    RadiusY = 3,
                    ToolTip = match is null
                        ? $"{DayOrder[d]}, {BucketLabel(HourBuckets[h])}: no sales"
                        : $"{DayOrder[d]}, {BucketLabel(HourBuckets[h])}: Rs. {match.Total:N2}"
                };
                Canvas.SetLeft(cell, rowLabelWidth + d * colWidth + 1);
                Canvas.SetTop(cell, headerHeight + h * rowHeight + 1);
                DrawCanvas.Children.Add(cell);
            }
        }
    }
}
