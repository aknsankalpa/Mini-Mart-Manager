using RetailFlow.Data;

namespace RetailFlow.Services;

/// <summary>
/// Small read-only aggregates used by the MiniMart Assistant's summary answers now, and
/// by the Dashboard screen once it's built. Kept separate from ProductService/SalesService
/// because these queries are about totals across the whole store, not one product or sale.
/// </summary>
public class DashboardService
{
    public int GetActiveProductCount()
    {
        using var context = new AppDbContext();
        return context.Products.Count(p => p.IsActive);
    }

    /// <summary>
    /// Total sales value and transaction count for the given date range (inclusive of
    /// both end dates).
    /// </summary>
    public (decimal Total, int Count) GetSalesSummary(DateTime fromDate, DateTime toDate)
    {
        using var context = new AppDbContext();

        var exclusiveEnd = toDate.Date.AddDays(1);
        var sales = context.Sales
            .Where(s => s.SaleDate >= fromDate.Date && s.SaleDate < exclusiveEnd)
            .ToList();

        return (sales.Sum(s => s.Total), sales.Count);
    }
}
