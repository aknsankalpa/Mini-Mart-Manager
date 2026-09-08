using RetailFlow.Models;

namespace RetailFlow.Data;

/// <summary>
/// Ensures the SQLite database and its tables exist, and seeds a small set of realistic
/// sample products on first run so the app has something to demonstrate immediately.
/// Safe to call on every startup: EnsureCreated() does nothing if the database already
/// exists, and the product seed is skipped once any product is present.
/// </summary>
public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.EnsureCreated();

        if (context.Products.Any())
        {
            return;
        }

        var now = DateTime.Now;

        // A couple of these are deliberately left below their reorder level so the
        // Low Stock Alert feature (Phase 10) has real data to demonstrate from the start.
        context.Products.AddRange(
            new Product { ProductCode = "P001", Name = "Rice 5kg", Category = "Groceries", Price = 1250.00m, StockQuantity = 40, ReorderLevel = 10, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Product { ProductCode = "P002", Name = "Sugar 1kg", Category = "Groceries", Price = 350.00m, StockQuantity = 3, ReorderLevel = 15, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Product { ProductCode = "P003", Name = "Tea 500g", Category = "Beverages", Price = 900.00m, StockQuantity = 4, ReorderLevel = 8, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Product { ProductCode = "P004", Name = "Milk Powder 400g", Category = "Dairy", Price = 1450.00m, StockQuantity = 2, ReorderLevel = 6, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Product { ProductCode = "P005", Name = "Biscuits", Category = "Snacks", Price = 220.00m, StockQuantity = 25, ReorderLevel = 10, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Product { ProductCode = "P006", Name = "Cooking Oil 1L", Category = "Groceries", Price = 1100.00m, StockQuantity = 18, ReorderLevel = 8, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Product { ProductCode = "P007", Name = "Flour 1kg", Category = "Groceries", Price = 300.00m, StockQuantity = 30, ReorderLevel = 10, IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Product { ProductCode = "P008", Name = "Soap", Category = "Household", Price = 150.00m, StockQuantity = 50, ReorderLevel = 12, IsActive = true, CreatedAt = now, UpdatedAt = now }
        );

        context.SaveChanges();
    }
}
