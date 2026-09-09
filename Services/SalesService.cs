using Microsoft.EntityFrameworkCore;
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
        // app could deactivate a product or its stock could drop in the meantime. Each
        // Product found here stays tracked by the context, so the same instance can have
        // its stock reduced below without a second round-trip to the database.
        var products = new Dictionary<int, Product>();
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

            products[item.ProductId] = product;
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

            // Reduce stock on the same tracked Product instance validated above.
            var product = products[item.ProductId];
            product.StockQuantity -= item.Quantity;
            product.UpdatedAt = DateTime.Now;
        }

        context.Sales.Add(sale);

        // The new Sale, its SaleItems, and every stock deduction are all written together
        // in one transaction: SaveChanges() applies all of them or none of them. If it
        // throws, the transaction rolls back and stock is left completely unchanged —
        // there is no window where a sale fails to save but stock drops anyway.
        using var transaction = context.Database.BeginTransaction();
        try
        {
            context.SaveChanges();
            transaction.Commit();
        }
        catch (Exception)
        {
            transaction.Rollback();
            return (false, "The sale could not be saved due to an unexpected error. Stock has not been changed.", string.Empty);
        }

        return (true, string.Empty, sale.InvoiceNumber);
    }

    /// <summary>
    /// Returns completed sales, most recent first, optionally filtered by invoice number
    /// (partial match) and/or a date range. Used by the Transaction History screen.
    /// </summary>
    public List<Sale> SearchSales(string invoiceSearch, DateTime? fromDate, DateTime? toDate)
    {
        using var context = new AppDbContext();

        var query = context.Sales.AsQueryable();

        if (!string.IsNullOrWhiteSpace(invoiceSearch))
        {
            var term = invoiceSearch.Trim();
            query = query.Where(s => s.InvoiceNumber.Contains(term));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(s => s.SaleDate >= fromDate.Value.Date);
        }

        if (toDate.HasValue)
        {
            // Add a day so the "to" date is inclusive of everything sold on that day.
            var exclusiveEnd = toDate.Value.Date.AddDays(1);
            query = query.Where(s => s.SaleDate < exclusiveEnd);
        }

        return query.OrderByDescending(s => s.SaleDate).ToList();
    }

    /// <summary>
    /// Loads one sale together with its line items and each item's product, for the
    /// Transaction History detail view.
    /// </summary>
    public Sale? GetSaleWithItems(int saleId)
    {
        using var context = new AppDbContext();

        return context.Sales
            .Include(s => s.SaleItems)
            .ThenInclude(si => si.Product)
            .FirstOrDefault(s => s.Id == saleId);
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
