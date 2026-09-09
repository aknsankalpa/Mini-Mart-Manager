using Microsoft.EntityFrameworkCore;
using RetailFlow.Data;
using RetailFlow.Helpers;
using RetailFlow.Models;

namespace RetailFlow.Services;

/// <summary>
/// Read-only aggregates for the Dashboard and the MiniMart Assistant's summary answers.
/// GetActiveProductCount/GetSalesSummary are the original, synchronous, unfiltered
/// versions the Assistant still calls directly — kept as-is so that integration point
/// never breaks. Everything below them is the newer, filter-aware set behind the
/// Power BI-style dashboard, aggregated on the database side rather than in memory.
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

    /// <summary>All distinct categories among active products, for the Category slicer.</summary>
    public List<string> GetCategories()
    {
        using var context = new AppDbContext();
        return context.Products
            .Where(p => p.IsActive)
            .Select(p => p.Category)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
    }

    /// <summary>All active products, for the Product slicer.</summary>
    public List<Product> GetActiveProducts()
    {
        using var context = new AppDbContext();
        return context.Products.Where(p => p.IsActive).OrderBy(p => p.Name).ToList();
    }

    private static IQueryable<SaleItem> FilteredLineItems(AppDbContext context, DashboardFilter filter)
    {
        var exclusiveEnd = filter.EndDate.Date.AddDays(1);

        var query = context.SaleItems
            .Include(si => si.Sale)
            .Include(si => si.Product)
            .Where(si => si.Sale.SaleDate >= filter.StartDate.Date && si.Sale.SaleDate < exclusiveEnd);

        if (!string.IsNullOrEmpty(filter.Category))
        {
            query = query.Where(si => si.Product.Category == filter.Category);
        }

        if (filter.ProductId.HasValue)
        {
            query = query.Where(si => si.ProductId == filter.ProductId.Value);
        }

        return query;
    }

    /// <summary>
    /// A line item's revenue with its sale's discount allocated proportionally (by share of
    /// the sale's subtotal) rather than the raw pre-discount LineTotal. Summing this across
    /// every item of a sale always reproduces that sale's net Total exactly, so an
    /// unfiltered KPI/chart total matches what the rest of the app (the MiniMart Assistant's
    /// "today's sales" answer, the Transaction History total) already reports.
    /// </summary>
    private static decimal NetRevenue(decimal lineTotal, decimal saleSubTotal, decimal saleDiscount) =>
        saleSubTotal > 0 ? lineTotal - (saleDiscount * lineTotal / saleSubTotal) : lineTotal;

    /// <summary>
    /// The four KPI cards. ActiveProductCount and LowStockCount reflect current inventory
    /// state (a Category/Product slicer narrows them, but the Date slicer never does — how
    /// many products exist right now doesn't change based on which days you're looking at).
    /// </summary>
    public async Task<DashboardSummary> GetDashboardSummaryAsync(DashboardFilter filter)
    {
        await using var context = new AppDbContext();

        var lineItems = await FilteredLineItems(context, filter)
            .Select(si => new { si.Product.Name, si.Quantity, si.LineTotal, si.Sale.SubTotal, si.Sale.Discount })
            .ToListAsync();

        var productsQuery = context.Products.Where(p => p.IsActive);
        if (!string.IsNullOrEmpty(filter.Category))
        {
            productsQuery = productsQuery.Where(p => p.Category == filter.Category);
        }
        if (filter.ProductId.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.Id == filter.ProductId.Value);
        }

        var activeProductCount = await productsQuery.CountAsync();
        var lowStockCount = await productsQuery.CountAsync(p => p.StockQuantity <= p.ReorderLevel);

        var best = lineItems
            .GroupBy(li => li.Name)
            .Select(g => new { Name = g.Key, Units = g.Sum(x => x.Quantity) })
            .OrderByDescending(g => g.Units)
            .FirstOrDefault();

        return new DashboardSummary
        {
            PeriodSales = lineItems.Sum(li => NetRevenue(li.LineTotal, li.SubTotal, li.Discount)),
            ActiveProductCount = activeProductCount,
            LowStockCount = lowStockCount,
            BestSellingProductName = best?.Name,
            BestSellingUnits = best?.Units ?? 0
        };
    }

    /// <summary>Revenue per calendar day within the filtered range, for the Sales Trend line chart.</summary>
    public async Task<List<SalesTrendPoint>> GetSalesTrendAsync(DashboardFilter filter)
    {
        await using var context = new AppDbContext();

        var raw = await FilteredLineItems(context, filter)
            .Select(si => new { si.Sale.SaleDate, si.LineTotal, si.Sale.SubTotal, si.Sale.Discount })
            .ToListAsync();

        return raw
            .GroupBy(x => x.SaleDate.Date)
            .Select(g => new SalesTrendPoint { Date = g.Key, Total = g.Sum(x => NetRevenue(x.LineTotal, x.SubTotal, x.Discount)) })
            .OrderBy(p => p.Date)
            .ToList();
    }

    /// <summary>Revenue per product category within the filtered range, for the Sales by Category bar chart.</summary>
    public async Task<List<CategorySales>> GetSalesByCategoryAsync(DashboardFilter filter)
    {
        await using var context = new AppDbContext();

        var raw = await FilteredLineItems(context, filter)
            .Select(si => new { si.Product.Category, si.LineTotal, si.Sale.SubTotal, si.Sale.Discount })
            .ToListAsync();

        return raw
            .GroupBy(x => x.Category)
            .Select(g => new CategorySales { Category = g.Key, Total = g.Sum(x => NetRevenue(x.LineTotal, x.SubTotal, x.Discount)) })
            .OrderByDescending(c => c.Total)
            .ToList();
    }

    /// <summary>
    /// The best-selling products within the filtered range, ranked by units sold.
    /// </summary>
    public async Task<List<TopSellingProduct>> GetTopSellingProductsAsync(DashboardFilter filter, int topN = 5)
    {
        await using var context = new AppDbContext();

        var raw = await FilteredLineItems(context, filter)
            .Select(si => new { si.Product.Name, si.Quantity, si.LineTotal, si.Sale.SubTotal, si.Sale.Discount })
            .ToListAsync();

        return raw
            .GroupBy(x => x.Name)
            .Select(g => new TopSellingProduct
            {
                ProductName = g.Key,
                UnitsSold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => NetRevenue(x.LineTotal, x.SubTotal, x.Discount))
            })
            .OrderByDescending(p => p.UnitsSold)
            .Take(topN)
            .ToList();
    }

    /// <summary>
    /// Revenue bucketed by day-of-week x 4-hour window, for the Sales Heatmap. Hours are
    /// bucketed (rather than one row per hour) so the grid stays readable at dashboard size.
    /// </summary>
    public async Task<List<HeatmapCell>> GetSalesHeatmapAsync(DashboardFilter filter)
    {
        await using var context = new AppDbContext();

        var raw = await FilteredLineItems(context, filter)
            .Select(si => new { si.Sale.SaleDate, si.LineTotal, si.Sale.SubTotal, si.Sale.Discount })
            .ToListAsync();

        return raw
            .GroupBy(x => (x.SaleDate.DayOfWeek, Bucket: (x.SaleDate.Hour / 4) * 4))
            .Select(g => new HeatmapCell { Day = g.Key.DayOfWeek, HourBucketStart = g.Key.Bucket, Total = g.Sum(x => NetRevenue(x.LineTotal, x.SubTotal, x.Discount)) })
            .ToList();
    }

    /// <summary>
    /// Units sold (within the filtered range) vs. current stock for every matching active
    /// product, for the Stock vs Sales scatter plot. Products with zero sales in the range
    /// are still included, plotted at x = 0.
    /// </summary>
    public async Task<List<StockVsSalesPoint>> GetStockVsSalesAsync(DashboardFilter filter)
    {
        await using var context = new AppDbContext();

        var productsQuery = context.Products.Where(p => p.IsActive);
        if (!string.IsNullOrEmpty(filter.Category))
        {
            productsQuery = productsQuery.Where(p => p.Category == filter.Category);
        }
        if (filter.ProductId.HasValue)
        {
            productsQuery = productsQuery.Where(p => p.Id == filter.ProductId.Value);
        }
        var products = await productsQuery.ToListAsync();

        var unitsSoldByProduct = await FilteredLineItems(context, filter)
            .GroupBy(si => si.ProductId)
            .Select(g => new { ProductId = g.Key, Units = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Units);

        return products
            .Select(p => new StockVsSalesPoint
            {
                ProductName = p.Name,
                UnitsSold = unitsSoldByProduct.TryGetValue(p.Id, out var units) ? units : 0,
                StockQuantity = p.StockQuantity,
                IsLowStock = ValidationHelper.IsLowStock(p.StockQuantity, p.ReorderLevel)
            })
            .ToList();
    }
}
