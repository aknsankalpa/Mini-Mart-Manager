using System.IO;
using Microsoft.EntityFrameworkCore;
using RetailFlow.Models;

namespace RetailFlow.Data;

/// <summary>
/// Represents the SQLite database as a set of .NET objects. Each DbSet below is one table;
/// EF Core translates LINQ queries against them into SQL.
/// </summary>
public class AppDbContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // The database file lives next to the application (not the current working
        // directory, which differs between "dotnet run" and double-clicking the .exe),
        // so the app is self-contained and behaves the same however it's launched.
        var dbPath = Path.Combine(AppContext.BaseDirectory, "retailflow.db");
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // --- Product ---
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.ProductCode).IsUnique();
            entity.Property(p => p.ProductCode).IsRequired().HasMaxLength(30);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Category).HasMaxLength(50);
            entity.Property(p => p.Price).HasPrecision(18, 2);
        });

        // --- Sale ---
        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasIndex(s => s.InvoiceNumber).IsUnique();
            entity.Property(s => s.InvoiceNumber).IsRequired().HasMaxLength(30);
            entity.Property(s => s.SubTotal).HasPrecision(18, 2);
            entity.Property(s => s.Discount).HasPrecision(18, 2);
            entity.Property(s => s.Total).HasPrecision(18, 2);
        });

        // --- SaleItem ---
        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.Property(si => si.UnitPrice).HasPrecision(18, 2);
            entity.Property(si => si.LineTotal).HasPrecision(18, 2);

            // Sale 1 -> many SaleItems. Deleting a sale deletes its line items with it.
            entity.HasOne(si => si.Sale)
                  .WithMany(s => s.SaleItems)
                  .HasForeignKey(si => si.SaleId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Product 1 -> many SaleItems. Products are only ever deactivated, never
            // deleted (see Product.IsActive), so this blocks an accidental hard delete
            // that would otherwise corrupt historical transaction data.
            entity.HasOne(si => si.Product)
                  .WithMany(p => p.SaleItems)
                  .HasForeignKey(si => si.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
