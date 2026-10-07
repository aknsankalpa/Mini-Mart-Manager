using System.Text.Json;
using RetailFlow.Models;

namespace RetailFlow.Services;

/// <summary>
/// The only things the AI assistant is allowed to do: a fixed list of read-only functions
/// that wrap the existing services. The model picks a function and its arguments; this
/// class runs the real query and returns plain text for the model to phrase as an answer.
/// Nothing the model says is trusted as data.
/// </summary>
public class AssistantToolService
{
    private readonly ProductService _productService = new();
    private readonly SalesService _salesService = new();
    private readonly DashboardService _dashboardService = new();

    public static readonly object[] Definitions =
    {
        Tool("get_product_stock",
            "Current stock level of product(s) whose name matches the text.",
            new { product_name = Str("Product name or part of it, e.g. 'tea' or 'Green Tea 250g'.") },
            "product_name"),

        Tool("get_low_stock",
            "Products at or below their reorder level.",
            new { }),

        Tool("get_active_product_count",
            "How many products are active in the store.",
            new { }),

        Tool("get_sales_summary",
            "Total sales value and number of transactions over a period.",
            new { days = Int("Number of days including today: 1 = today, 7 = last 7 days, 30 = last 30 days.") },
            "days"),

        Tool("get_units_sold",
            "How many units of a product were sold, and the revenue, over a period.",
            new
            {
                product_name = Str("Product name as the user wrote it, e.g. 'toothpaste'."),
                days = Int("Number of days including today: 1 = today, 30 = last 30 days.")
            },
            "product_name", "days"),

        Tool("get_top_products",
            "Best-selling products by units sold over a period.",
            new
            {
                days = Int("Number of days including today: 1 = today, 30 = last 30 days."),
                count = Int("How many products to list, 1 to 10.")
            },
            "days"),

        Tool("get_transactions",
            "Transactions (sales) made over a period, with the most recent ones listed.",
            new { days = Int("Number of days including today: 1 = today, 7 = last 7 days.") },
            "days"),
    };

    public async Task<string> ExecuteAsync(string name, JsonElement arguments)
    {
        return name switch
        {
            "get_product_stock" => ProductStock(GetString(arguments, "product_name")),
            "get_low_stock" => LowStock(),
            "get_active_product_count" => $"There are {_dashboardService.GetActiveProductCount()} active product(s).",
            "get_sales_summary" => SalesSummary(GetDays(arguments)),
            "get_units_sold" => UnitsSold(GetString(arguments, "product_name"), GetDays(arguments)),
            "get_top_products" => await TopProductsAsync(GetDays(arguments), GetInt(arguments, "count", 5)),
            "get_transactions" => Transactions(GetDays(arguments)),
            _ => $"Unknown function '{name}'."
        };
    }

    private string ProductStock(string name)
    {
        var matches = FindProducts(name);
        if (matches.Count == 0)
        {
            return $"No product matches \"{name}\".";
        }

        return string.Join("\n", matches.Take(10).Select(p =>
            $"{p.Name}: {p.StockQuantity} unit(s) in stock (reorder level {p.ReorderLevel})."));
    }

    private string LowStock()
    {
        var lowStock = _productService.GetLowStockProducts();
        if (lowStock.Count == 0)
        {
            return "No products are at or below their reorder level.";
        }

        var lines = lowStock.Take(15).Select(p => $"{p.Name}: {p.StockQuantity} unit(s) (reorder level {p.ReorderLevel})");
        return $"{lowStock.Count} product(s) at or below reorder level:\n" + string.Join("\n", lines);
    }

    private string SalesSummary(int days)
    {
        var (from, to, description) = Period(days);
        var (total, count) = _dashboardService.GetSalesSummary(from, to);
        return $"Sales for {description}: Rs. {total:N2} from {count} transaction(s).";
    }

    private string UnitsSold(string name, int days)
    {
        var (from, to, description) = Period(days);
        var matches = FindProducts(name);
        if (matches.Count == 0)
        {
            return $"No product matches \"{name}\".";
        }

        var (units, revenue) = _dashboardService.GetUnitsSold(matches.Select(p => p.Id), from, to);
        var names = string.Join(", ", matches.Take(5).Select(p => p.Name));
        return $"{names} sold {units} unit(s) for Rs. {revenue:N2} in {description}.";
    }

    private async Task<string> TopProductsAsync(int days, int count)
    {
        var (from, to, description) = Period(days);
        var filter = new DashboardFilter { StartDate = from, EndDate = to };
        var top = await _dashboardService.GetTopSellingProductsAsync(filter, Math.Clamp(count, 1, 10));

        if (top.Count == 0)
        {
            return $"No sales in {description}.";
        }

        var lines = top.Select((p, i) => $"{i + 1}. {p.ProductName}: {p.UnitsSold} unit(s), Rs. {p.Revenue:N2}");
        return $"Best-selling products in {description}:\n" + string.Join("\n", lines);
    }

    private string Transactions(int days)
    {
        var (from, to, description) = Period(days);
        var sales = _salesService.SearchSales(from, to);
        if (sales.Count == 0)
        {
            return $"No transactions in {description}.";
        }

        var recent = sales.Take(5).Select(s => $"{s.InvoiceNumber}, {s.SaleDate:MMM d h:mm tt}, Rs. {s.Total:N2}");
        return $"{sales.Count} transaction(s) in {description}, total Rs. {sales.Sum(s => s.Total):N2}. Most recent:\n"
            + string.Join("\n", recent);
    }

    /// <summary>Matches a product by name; a plural such as "toothpastes" falls back to its singular.</summary>
    private List<Product> FindProducts(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new List<Product>();
        }

        var matches = _productService.Search(name, includeInactive: false);
        if (matches.Count == 0 && name.EndsWith('s'))
        {
            matches = _productService.Search(name[..^1], includeInactive: false);
        }

        return matches;
    }

    /// <summary>"days" counts today as day 1, so 30 means today and the 29 days before it.</summary>
    private static (DateTime From, DateTime To, string Description) Period(int days)
    {
        var to = DateTime.Today;
        var from = to.AddDays(-(days - 1));
        var description = days == 1
            ? $"today ({to:MMM d, yyyy})"
            : $"the last {days} days ({from:MMM d} to {to:MMM d, yyyy})";
        return (from, to, description);
    }

    private static int GetDays(JsonElement arguments) => Math.Clamp(GetInt(arguments, "days", 30), 1, 365);

    private static string GetString(JsonElement arguments, string property) =>
        arguments.ValueKind == JsonValueKind.Object
            && arguments.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.String
                ? value.GetString() ?? string.Empty
                : string.Empty;

    private static int GetInt(JsonElement arguments, string property, int fallback)
    {
        if (arguments.ValueKind != JsonValueKind.Object || !arguments.TryGetProperty(property, out var value))
        {
            return fallback;
        }

        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
        {
            return number;
        }

        return value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed)
            ? parsed
            : fallback;
    }

    private static object Tool(string name, string description, object properties, params string[] required) => new
    {
        type = "function",
        function = new
        {
            name,
            description,
            parameters = new { type = "object", properties, required }
        }
    };

    private static object Str(string description) => new { type = "string", description };

    private static object Int(string description) => new { type = "integer", description };
}
