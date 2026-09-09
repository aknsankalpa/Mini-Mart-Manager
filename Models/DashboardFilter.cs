namespace RetailFlow.Models;

/// <summary>
/// The set of slicers applied to the Dashboard: a date range plus optional category/product
/// drill-down. Passed into every DashboardService aggregate method so the charts and KPIs
/// stay in sync with whatever the user has selected.
/// </summary>
public class DashboardFilter
{
    public DateTime StartDate { get; set; } = DateTime.Today;

    public DateTime EndDate { get; set; } = DateTime.Today;

    public string? Category { get; set; }

    public int? ProductId { get; set; }
}
