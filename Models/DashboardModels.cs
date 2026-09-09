namespace RetailFlow.Models;

/// <summary>
/// Read-only DTOs returned by DashboardService's aggregate queries. Kept separate from the
/// entities (Product/Sale/SaleItem) because these shapes exist only to feed the Dashboard's
/// KPI cards and charts, not to be persisted.
/// </summary>

public class DashboardSummary
{
    public decimal PeriodSales { get; set; }
    public int ActiveProductCount { get; set; }
    public int LowStockCount { get; set; }
    public string? BestSellingProductName { get; set; }
    public int BestSellingUnits { get; set; }
}

public class SalesTrendPoint
{
    public DateTime Date { get; set; }
    public decimal Total { get; set; }
}

public class CategorySales
{
    public string Category { get; set; } = string.Empty;
    public decimal Total { get; set; }
}

public class TopSellingProduct
{
    public string ProductName { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public decimal Revenue { get; set; }
}

public class HeatmapCell
{
    public DayOfWeek Day { get; set; }
    public int HourBucketStart { get; set; }
    public decimal Total { get; set; }
}

public class StockVsSalesPoint
{
    public string ProductName { get; set; } = string.Empty;
    public int UnitsSold { get; set; }
    public int StockQuantity { get; set; }
    public bool IsLowStock { get; set; }
}
