using RetailFlow.Data;
using RetailFlow.Models;
using RetailFlow.ViewModels;

namespace RetailFlow.Services;

/// <summary>
/// Turns a completed cart into a persisted Sale + SaleItem rows. Subtotal and Total are
/// always recomputed here from each cart line's UnitPrice x Quantity — the UI's displayed
/// numbers are never taken on trust and written straight to the database.
/// </summary>
public class SalesService
{
    public (bool Success, string ErrorMessage, string InvoiceNumber) CompleteSale(List<CartItem> cartItems, decimal discount)
    {
        if (cartItems.Count == 0)
        {
            return (false, "Add at least one product to the cart before completing the sale.", string.Empty);
        }

        using var context = new AppDbContext();

        // Re-check every line against the live database before writing anything. The
        // ViewModel already checks this at the moment a product is added to the cart, but
        // time passes between then and clicking Complete Sale — another screen in this same
        // app could deactivate a product or its stock could drop in the meantime.
        foreach (var item in cartItems)
        {
            var product = context.Products.Find(item.ProductId);

            if (product is null || !product.IsActive)
            {
                return (false, $"'{item.ProductName}' is no longer available. Please remove it from the cart.", string.Empty);
            }

            if (item.Quantity > product.StockQuantity)
            {
                return (false, $"Insufficient Stock\n\nOnly {product.StockQuantity} units of {product.Name} are currently available.", string.Empty);
            }
        }

        var subtotal = cartItems.Sum(item => item.UnitPrice * item.Quantity);

        if (discount < 0 || discount > subtotal)
        {
            return (false, "Invalid discount amount.", string.Empty);
        }

        var sale = new Sale
        {
            InvoiceNumber = GenerateInvoiceNumber(context),
            SaleDate = DateTime.Now,
            SubTotal = subtotal,
            Discount = discount,
            Total = subtotal - discount
        };

        foreach (var item in cartItems)
        {
            sale.SaleItems.Add(new SaleItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                LineTotal = item.UnitPrice * item.Quantity
            });
        }

        context.Sales.Add(sale);
        context.SaveChanges();

        return (true, string.Empty, sale.InvoiceNumber);
    }

    /// <summary>
    /// Produces invoice numbers like INV-20260908-001, resetting the sequence each day.
    /// Fine for a single-user desktop app; a busier multi-till system would need a
    /// database-level sequence to stay race-free under concurrent sales.
    /// </summary>
    private static string GenerateInvoiceNumber(AppDbContext context)
    {
        var today = DateTime.Now.Date;
        var todaysSaleCount = context.Sales.Count(s => s.SaleDate.Date == today);
        return $"INV-{today:yyyyMMdd}-{todaysSaleCount + 1:D3}";
    }
}
