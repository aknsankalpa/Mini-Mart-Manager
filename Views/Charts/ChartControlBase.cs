using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace RetailFlow.Views.Charts;

/// <summary>
/// Shared plumbing for the five hand-drawn Dashboard charts: an ItemsSource dependency
/// property that redraws whenever the bound collection is replaced OR its contents change
/// (the Dashboard's chart collections are cleared-and-refilled in place, not replaced, so
/// listening only for DependencyProperty changes would miss every reload), plus a redraw on
/// resize so the chart always fills its cell in the no-scroll layout. Each concrete control
/// supplies its own XAML (a root Canvas) and overrides Redraw() to draw into it — nothing
/// here depends on what shape the data is.
/// </summary>
public abstract class ChartControlBase : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(ChartControlBase),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    protected ChartControlBase()
    {
        SizeChanged += (_, _) => Redraw();
        Loaded += (_, _) => Redraw();
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ChartControlBase)d;

        if (e.OldValue is INotifyCollectionChanged oldIncc)
        {
            oldIncc.CollectionChanged -= control.OnCollectionChanged;
        }

        if (e.NewValue is INotifyCollectionChanged newIncc)
        {
            newIncc.CollectionChanged += control.OnCollectionChanged;
        }

        control.Redraw();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Redraw();

    protected abstract void Redraw();
}
