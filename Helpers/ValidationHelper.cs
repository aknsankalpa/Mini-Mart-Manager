namespace RetailFlow.Helpers;

/// <summary>
/// Small, reusable checks shared by services and view models, so a business rule (e.g.
/// "a price must be positive", "a sale can't exceed available stock") is written once and
/// reused everywhere it's needed, rather than re-expressed slightly differently in the
/// Products screen, the Sales screen, and the persistence layer.
/// </summary>
public static class ValidationHelper
{
    public static bool IsPositive(decimal value) => value > 0;

    public static bool IsPositive(int value) => value > 0;

    public static bool IsNonNegative(int value) => value >= 0;

    /// <summary>Sale quantity &lt;= stock.</summary>
    public static bool IsSufficientStock(int requestedQuantity, int availableStock) => requestedQuantity <= availableStock;

    /// <summary>Low stock = stock &lt;= reorder level.</summary>
    public static bool IsLowStock(int stockQuantity, int reorderLevel) => stockQuantity <= reorderLevel;
}
