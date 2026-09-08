namespace RetailFlow.Models;

/// <summary>
/// One line of a Sale: a single product, the quantity sold, and its price at the time of
/// sale. Kept as a separate table (rather than fields on Sale) because a sale can contain
/// many products, and UnitPrice is copied here so a later price change never rewrites history.
/// </summary>
public class SaleItem
{
    public int Id { get; set; }

    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }
}
