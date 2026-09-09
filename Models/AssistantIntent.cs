namespace RetailFlow.Models;

/// <summary>
/// The set of requests the MiniMart Assistant knows how to handle. This is a closed,
/// rule-based list — not a machine-learning classification — so every value here maps
/// to an explicit set of matching phrases in AssistantQueryService.
/// </summary>
public enum AssistantIntent
{
    Unknown,
    NavigateDashboard,
    NavigateProducts,
    NavigateSales,
    NavigateTransactions,
    SearchProduct,
    SearchStock,
    LowStockQuery,
    TransactionQuery,
    SalesSummary,
    ProductStockQuery,
    ProductCountQuery
}
