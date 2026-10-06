using RetailFlow.Models;

namespace RetailFlow.Data;

/// <summary>
/// Ensures the SQLite database and its tables exist, and seeds a realistic product
/// catalog and sales history on first run so the app has something to demonstrate
/// immediately. Safe to call on every startup: EnsureCreated() does nothing if the
/// database already exists, and the seed itself is skipped once any product is present.
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

        var products = CreateProducts();
        context.Products.AddRange(products);

        var sales = CreateSales(products);
        context.Sales.AddRange(sales);

        context.SaveChanges();
    }

    /// <summary>
    /// ~28 products across a small minimart's usual categories. Six are deliberately
    /// stocked at or below their reorder level (marked LOW below) so the Dashboard, the
    /// Products screen's "Low stock only" filter, and the MiniMart Assistant's low-stock
    /// query all have real data to demonstrate from the very first run.
    /// </summary>
    private static List<Product> CreateProducts()
    {
        var now = DateTime.Now;

        Product Make(string code, string name, string category, decimal price, int stock, int reorderLevel) => new()
        {
            ProductCode = code,
            Name = name,
            Category = category,
            Price = price,
            StockQuantity = stock,
            ReorderLevel = reorderLevel,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        return new List<Product>
        {
            Make("P001", "Rice 5kg", "Groceries", 1250.00m, 40, 10),
            Make("P002", "Sugar 1kg", "Groceries", 350.00m, 3, 15),               // LOW
            Make("P003", "Green Tea 250g", "Beverages", 450.00m, 12, 10),
            Make("P004", "Black Tea 500g", "Beverages", 650.00m, 8, 10),          // LOW
            Make("P005", "Ceylon Tea 250g", "Beverages", 500.00m, 15, 10),
            Make("P006", "Milk Powder 400g", "Dairy", 1450.00m, 2, 6),            // LOW
            Make("P007", "Biscuits", "Snacks", 220.00m, 25, 10),
            Make("P008", "Cooking Oil 1L", "Groceries", 1100.00m, 18, 8),
            Make("P009", "Flour 1kg", "Groceries", 300.00m, 30, 10),
            Make("P010", "Soap", "Household", 150.00m, 50, 12),
            Make("P011", "Shampoo 200ml", "Personal Care", 650.00m, 20, 8),
            Make("P012", "Toothpaste 100g", "Personal Care", 320.00m, 22, 10),
            Make("P013", "Salt 1kg", "Groceries", 120.00m, 35, 10),
            Make("P014", "Red Lentils 1kg", "Groceries", 480.00m, 28, 10),
            Make("P015", "Instant Noodles", "Snacks", 180.00m, 45, 15),
            Make("P016", "Chocolate Bar", "Snacks", 250.00m, 5, 12),              // LOW
            Make("P017", "Butter 200g", "Dairy", 780.00m, 15, 6),
            Make("P018", "Cheese Slices", "Dairy", 950.00m, 12, 5),
            Make("P019", "Yogurt Cup", "Dairy", 130.00m, 40, 10),
            Make("P020", "Bread Loaf", "Bakery", 190.00m, 20, 8),
            Make("P021", "Eggs (12 pack)", "Dairy", 620.00m, 24, 8),
            Make("P022", "Dishwashing Liquid", "Household", 420.00m, 16, 6),
            Make("P023", "Toilet Paper (4 pack)", "Household", 380.00m, 30, 10),
            Make("P024", "Detergent Powder 1kg", "Household", 550.00m, 3, 10),    // LOW
            Make("P025", "Coffee 200g", "Beverages", 850.00m, 18, 6),
            Make("P026", "Fruit Juice 1L", "Beverages", 380.00m, 22, 8),
            Make("P027", "Mineral Water 1.5L", "Beverages", 120.00m, 60, 15),
            Make("P028", "Hand Sanitizer 100ml", "Personal Care", 280.00m, 6, 8), // LOW
        };
    }

    /// <summary>
    /// A small set of hand-written sales covering today, yesterday, this week, and this
    /// month, followed by a generated history of about 90 days (see CreateGeneratedSales).
    /// Dates are relative to DateTime.Today rather than a fixed calendar date, so the demo
    /// always looks current whenever it's run.
    /// </summary>
    private static List<Sale> CreateSales(List<Product> products)
    {
        Product Find(string code) => products.First(p => p.ProductCode == code);
        var today = DateTime.Today;

        var sales = new List<Sale>
        {
            // Today
            CreateSale("001", today.AddHours(9).AddMinutes(15), 100.00m,
                (Find("P001"), 2), (Find("P002"), 1)),
            CreateSale("002", today.AddHours(11).AddMinutes(30), 0.00m,
                (Find("P020"), 2), (Find("P017"), 1), (Find("P021"), 1)),
            CreateSale("003", today.AddHours(14).AddMinutes(45), 0.00m,
                (Find("P003"), 1), (Find("P007"), 3)),

            // Yesterday
            CreateSale("001", today.AddDays(-1).AddHours(10), 0.00m,
                (Find("P008"), 1), (Find("P009"), 2)),
            CreateSale("002", today.AddDays(-1).AddHours(16).AddMinutes(20), 50.00m,
                (Find("P012"), 1), (Find("P011"), 1), (Find("P010"), 2)),

            // Earlier this week
            CreateSale("001", today.AddDays(-2).AddHours(12), 0.00m,
                (Find("P006"), 1), (Find("P025"), 1)),
            CreateSale("001", today.AddDays(-3).AddHours(9).AddMinutes(40), 50.00m,
                (Find("P001"), 1), (Find("P013"), 1), (Find("P014"), 2)),
            CreateSale("001", today.AddDays(-4).AddHours(15), 0.00m,
                (Find("P015"), 5), (Find("P016"), 2)),
            CreateSale("001", today.AddDays(-5).AddHours(11).AddMinutes(10), 0.00m,
                (Find("P023"), 2), (Find("P022"), 1)),

            // Earlier this month
            CreateSale("001", today.AddDays(-10).AddHours(13), 0.00m,
                (Find("P026"), 3), (Find("P027"), 2)),
            CreateSale("001", today.AddDays(-15).AddHours(10).AddMinutes(30), 0.00m,
                (Find("P018"), 1), (Find("P019"), 4)),
            CreateSale("001", today.AddDays(-20).AddHours(16), 30.00m,
                (Find("P024"), 1), (Find("P028"), 2)),
        };

        sales.AddRange(CreateGeneratedSales(products));
        return sales;
    }

    /// <summary>
    /// Generates about 90 days of sales so the Dashboard's trend, category, top-product,
    /// heatmap, and stock charts show real movement. A fixed random seed makes every fresh
    /// database identical. Weekends and the most recent three weeks see more sales, staple
    /// products sell more often than specialty items, and about one sale in seven gets a
    /// round-number discount. Stock levels are a snapshot and are not reduced by these sales.
    /// </summary>
    private static List<Sale> CreateGeneratedSales(List<Product> products)
    {
        var random = new Random(2026);
        var today = DateTime.Today;
        var popularity = products.Select(p => (Product: p, Weight: PopularityWeight(p.ProductCode))).ToList();
        var peakHours = new[] { 9, 10, 11, 12, 12, 13, 14, 16, 17, 17, 18, 18, 19, 20 };

        var sales = new List<Sale>();

        for (var daysAgo = 89; daysAgo >= 0; daysAgo--)
        {
            var day = today.AddDays(-daysAgo);
            var isWeekend = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var salesToday = random.Next(1, 4) + (isWeekend ? 2 : 0) + (daysAgo < 21 ? 1 : 0);

            for (var n = 0; n < salesToday; n++)
            {
                var items = PickItems(random, popularity);
                var subtotal = items.Sum(item => item.Product.Price * item.Quantity);

                var discount = random.Next(7) == 0 ? random.Next(1, 11) * 10m : 0m;
                if (discount > subtotal)
                {
                    discount = 0m;
                }

                var saleTime = day.AddHours(peakHours[random.Next(peakHours.Length)]).AddMinutes(random.Next(60));
                sales.Add(CreateSale($"{10 + n:D3}", saleTime, discount, items.ToArray()));
            }
        }

        return sales;
    }

    private static List<(Product Product, int Quantity)> PickItems(Random random, List<(Product Product, int Weight)> popularity)
    {
        var itemCount = random.Next(1, 4);
        var picked = new List<(Product Product, int Quantity)>();

        while (picked.Count < itemCount)
        {
            var product = WeightedPick(random, popularity);
            if (picked.Any(item => item.Product.ProductCode == product.ProductCode))
            {
                continue;
            }

            picked.Add((product, random.Next(1, 4)));
        }

        return picked;
    }

    private static Product WeightedPick(Random random, List<(Product Product, int Weight)> popularity)
    {
        var roll = random.Next(popularity.Sum(p => p.Weight));
        foreach (var (product, weight) in popularity)
        {
            roll -= weight;
            if (roll < 0)
            {
                return product;
            }
        }

        return popularity[^1].Product;
    }

    private static int PopularityWeight(string productCode) => productCode switch
    {
        "P001" or "P003" or "P007" or "P009" or "P015" or "P017" or "P020" or "P021" => 4,
        "P002" or "P006" or "P016" or "P018" or "P024" or "P025" or "P027" or "P028" => 1,
        _ => 2
    };

    /// <summary>
    /// Builds one Sale with its SaleItems, computing SubTotal and Total from the given
    /// products/quantities — the same "subtotal = sum of line totals, total = subtotal -
    /// discount" rule the app enforces everywhere else, just written out for fixed demo
    /// data instead of a live cart.
    /// </summary>
    private static Sale CreateSale(string sequenceForDay, DateTime saleDate, decimal discount, params (Product Product, int Quantity)[] items)
    {
        var sale = new Sale
        {
            InvoiceNumber = $"INV-{saleDate:yyyyMMdd}-{sequenceForDay}",
            SaleDate = saleDate,
            Discount = discount
        };

        foreach (var (product, quantity) in items)
        {
            sale.SaleItems.Add(new SaleItem
            {
                Product = product,
                Quantity = quantity,
                UnitPrice = product.Price,
                LineTotal = product.Price * quantity
            });
        }

        sale.SubTotal = sale.SaleItems.Sum(si => si.LineTotal);
        sale.Total = sale.SubTotal - sale.Discount;

        return sale;
    }
}
