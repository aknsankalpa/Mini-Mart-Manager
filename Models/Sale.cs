namespace RetailFlow.Models;

/// <summary>
/// A completed sales transaction (one invoice). The line-by-line detail lives in SaleItems.
/// </summary>
public class Sale
{
    public int Id { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; } = DateTime.Now;

    public decimal SubTotal { get; set; }

    public decimal Discount { get; set; }

    public decimal Total { get; set; }

    // Navigation property: the products and quantities that make up this sale.
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
