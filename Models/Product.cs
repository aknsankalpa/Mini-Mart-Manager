namespace RetailFlow.Models;

/// <summary>
/// A product that can be stocked and sold. Products are never hard-deleted so that
/// historical SaleItems always resolve correctly; use IsActive to retire a product instead.
/// </summary>
public class Product
{
    public int Id { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    public int ReorderLevel { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation property: every SaleItem that has ever sold this product.
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
}
