using RetailFlow.ViewModels;

namespace RetailFlow.Services;

/// <summary>
/// The arithmetic behind a sale — subtotal and total — defined exactly once so the live
/// numbers shown while building a cart (SalesViewModel) and the numbers actually written
/// to the database when the sale completes (SalesService) can never disagree. Each
/// CartItem's own LineTotal (UnitPrice x Quantity) is the one place that formula lives;
/// this class only ever sums it, never recomputes it.
/// </summary>
public static class SaleCalculator
{
    public static decimal CalculateSubtotal(IEnumerable<CartItem> cartItems) =>
        cartItems.Sum(item => item.LineTotal);

    public static decimal CalculateTotal(decimal subtotal, decimal discount) =>
        subtotal - discount;
}
