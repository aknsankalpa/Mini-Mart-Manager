using Microsoft.EntityFrameworkCore;
using RetailFlow.Data;
using RetailFlow.Helpers;
using RetailFlow.Models;

namespace RetailFlow.Services;

/// <summary>
/// All product business rules and database access live here, not in the ViewModel or the
/// View. This keeps validation and persistence in one place regardless of which screen
/// ends up using it (only Products for now, but Sales will also need to read products).
/// </summary>
public class ProductService
{
    /// <summary>
    /// Returns products matching the search term (by code, name, or category), optionally
    /// including inactive (deactivated) ones. Passing an empty search term returns everything.
    /// </summary>
    public List<Product> Search(string searchTerm, bool includeInactive)
    {
        using var context = new AppDbContext();

        var query = context.Products.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim();
            query = query.Where(p =>
                p.ProductCode.Contains(term) ||
                p.Name.Contains(term) ||
                p.Category.Contains(term));
        }

        return query.OrderBy(p => p.Name).ToList();
    }

    public (bool Success, string ErrorMessage) AddProduct(Product product)
    {
        var (isValid, validationError) = Validate(product);
        if (!isValid)
        {
            return (false, validationError);
        }

        using var context = new AppDbContext();

        if (context.Products.Any(p => p.ProductCode == product.ProductCode))
        {
            return (false, "Product code already exists.");
        }

        product.Id = 0;
        product.IsActive = true;
        product.CreatedAt = DateTime.Now;
        product.UpdatedAt = DateTime.Now;

        try
        {
            context.Products.Add(product);
            context.SaveChanges();
            return (true, string.Empty);
        }
        catch (DbUpdateException)
        {
            // Covers the rare race where two saves happen at almost the same moment
            // and both pass the check above; the database's unique index is the final say.
            return (false, "Product code already exists.");
        }
    }

    public (bool Success, string ErrorMessage) UpdateProduct(Product product)
    {
        var (isValid, validationError) = Validate(product);
        if (!isValid)
        {
            return (false, validationError);
        }

        using var context = new AppDbContext();

        if (context.Products.Any(p => p.ProductCode == product.ProductCode && p.Id != product.Id))
        {
            return (false, "Product code already exists.");
        }

        var existing = context.Products.Find(product.Id);
        if (existing is null)
        {
            return (false, "This product no longer exists. It may have been removed.");
        }

        existing.ProductCode = product.ProductCode;
        existing.Name = product.Name;
        existing.Category = product.Category;
        existing.Price = product.Price;
        existing.StockQuantity = product.StockQuantity;
        existing.ReorderLevel = product.ReorderLevel;
        existing.UpdatedAt = DateTime.Now;

        try
        {
            context.SaveChanges();
            return (true, string.Empty);
        }
        catch (DbUpdateException)
        {
            return (false, "Product code already exists.");
        }
    }

    /// <summary>
    /// Activates or deactivates a product. Products are never hard-deleted (see
    /// Product.IsActive) so that past sales referencing this product stay intact.
    /// </summary>
    public (bool Success, string ErrorMessage) SetActiveStatus(int productId, bool isActive)
    {
        using var context = new AppDbContext();

        var product = context.Products.Find(productId);
        if (product is null)
        {
            return (false, "This product no longer exists. It may have been removed.");
        }

        product.IsActive = isActive;
        product.UpdatedAt = DateTime.Now;
        context.SaveChanges();

        return (true, string.Empty);
    }

    private static (bool IsValid, string ErrorMessage) Validate(Product product)
    {
        if (string.IsNullOrWhiteSpace(product.ProductCode))
        {
            return (false, "Product code is required.");
        }

        if (string.IsNullOrWhiteSpace(product.Name))
        {
            return (false, "Product name is required.");
        }

        if (!ValidationHelper.IsPositive(product.Price))
        {
            return (false, "Price must be greater than 0.");
        }

        if (!ValidationHelper.IsNonNegative(product.StockQuantity))
        {
            return (false, "Stock quantity cannot be negative.");
        }

        if (!ValidationHelper.IsNonNegative(product.ReorderLevel))
        {
            return (false, "Reorder level cannot be negative.");
        }

        return (true, string.Empty);
    }
}
