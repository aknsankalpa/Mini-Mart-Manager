using System.Text.RegularExpressions;
using RetailFlow.Models;

namespace RetailFlow.Services;

/// <summary>
/// A rule-based natural-language query and navigation assistant. It normalizes the raw
/// text, matches it against a fixed set of known phrasings to detect an intent, extracts
/// parameters (a search term, a date range, an amount range) with a small set of regex
/// templates, and delegates the actual work to ProductService/SalesService/DashboardService.
///
/// This is keyword and pattern matching, not machine learning: every phrase this class
/// understands is an explicit rule below. That is what keeps it deterministic, fully
/// offline, and easy to test — and also why it only understands a fixed set of request
/// shapes rather than "understanding" language in general.
/// </summary>
public class AssistantQueryService
{
    private readonly ProductService _productService = new();
    private readonly SalesService _salesService = new();
    private readonly DashboardService _dashboardService = new();

    private static readonly string[] TransactionSynonyms = { "transaction", "transactions", "sales history", "sale history" };

    private static readonly string[] DashboardPhrases = { "dashboard", "open dashboard", "go to dashboard", "show dashboard", "view dashboard" };
    private static readonly string[] ProductsPhrases = { "products", "open products", "go to products", "show products", "view products" };
    private static readonly string[] SalesNavPhrases = { "sales", "open sales", "go to sales", "show sales", "new sale", "view sales", "pos" };
    private static readonly string[] TransactionsNavPhrases = { "transactions", "transaction history", "show transactions", "sales history", "open transactions", "go to transactions", "view transactions" };

    public AssistantResponse Interpret(string rawQuery)
    {
        if (string.IsNullOrWhiteSpace(rawQuery))
        {
            return new AssistantResponse { Message = "Please enter a question or command." };
        }

        try
        {
            var query = new AssistantQuery { OriginalQuery = rawQuery };
            DetectIntent(Normalize(rawQuery), query);
            return BuildResponse(query);
        }
        catch (Exception)
        {
            // Whatever went wrong internally, the user only ever sees a plain-language
            // message — never a raw exception or database error.
            return new AssistantResponse { Message = "Sorry, I ran into a problem handling that request. Please try again." };
        }
    }

    // ---------- Normalization (10.17) ----------

    private static string Normalize(string input)
    {
        var text = input.ToLowerInvariant().Trim();
        text = Regex.Replace(text, @"[^\w\s]", " ");    // drop punctuation like !?.,'"
        text = Regex.Replace(text, @"\s+", " ").Trim(); // collapse repeated spaces
        return text;
    }

    // ---------- Intent detection (10.18) ----------

    private void DetectIntent(string normalized, AssistantQuery query)
    {
        // 1. Short, exact navigation commands. These use an exact whole-string match
        //    (not "contains") so a longer sentence like "sales history from monday"
        //    isn't mistaken for the bare "sales" navigation command.
        if (DashboardPhrases.Contains(normalized)) { query.Intent = AssistantIntent.NavigateDashboard; return; }
        if (ProductsPhrases.Contains(normalized)) { query.Intent = AssistantIntent.NavigateProducts; return; }
        if (SalesNavPhrases.Contains(normalized)) { query.Intent = AssistantIntent.NavigateSales; return; }
        if (TransactionsNavPhrases.Contains(normalized)) { query.Intent = AssistantIntent.NavigateTransactions; return; }

        // 2. Low stock.
        if (ContainsAny(normalized, "low stock", "low in stock", "below reorder", "need restocking", "needs restocking", "restock"))
        {
            query.Intent = AssistantIntent.LowStockQuery;
            return;
        }

        // 3. Product count.
        if (ContainsAny(normalized, "how many products", "total products", "number of products", "product count"))
        {
            query.Intent = AssistantIntent.ProductCountQuery;
            return;
        }

        // 4. Sales summary — a specific request for a total, checked before the more
        //    general transaction check below so it isn't swallowed by the word "sales".
        if (ContainsAny(normalized, "how much did we sell", "sales total", "total sales",
                "what are today s sales", "what are todays sales", "how much sales", "todays sales", "today s sales"))
        {
            query.Intent = AssistantIntent.SalesSummary;
            ExtractDateRange(normalized, query);
            return;
        }

        // 5. Transactions / sales history, with optional date and amount filters.
        var mentionsTransactions = ContainsAny(normalized, TransactionSynonyms)
            || (normalized.Contains("sales") && ContainsAny(normalized, "from", "between", "this week", "this month",
                    "yesterday", "today", "above", "over", "below", "under"));

        if (mentionsTransactions)
        {
            query.Intent = AssistantIntent.TransactionQuery;
            ExtractDateRange(normalized, query);
            ExtractAmountRange(normalized, query);
            return;
        }

        // 6. Stock questions — either a specific product's quantity or a general search.
        // These templates are specific enough on their own (each requires an exact
        // phrase shape like "X stock" or "how much X do we have") that a separate
        // "contains a stock-related word" pre-check isn't needed — some valid phrasings,
        // like "...do we have", don't literally contain a word like "stock" or "available".
        var stockTerm = ExtractSearchTerm(normalized, StockQueryPatterns);
        if (stockTerm is not null)
        {
            query.Intent = AssistantIntent.SearchStock;
            query.SearchTerm = stockTerm;
            return;
        }

        // 7. General product search.
        var productTerm = ExtractSearchTerm(normalized, ProductSearchPatterns);
        if (productTerm is not null)
        {
            query.Intent = AssistantIntent.SearchProduct;
            query.SearchTerm = productTerm;
            return;
        }

        query.Intent = AssistantIntent.Unknown;
    }

    private static bool ContainsAny(string text, params string[] candidates) => candidates.Any(text.Contains);

    // ---------- Parameter extraction ----------

    private static readonly Regex[] StockQueryPatterns =
    {
        new(@"^(?:show|find)\s+(.+?)\s+stock$"),
        new(@"^what is the stock of (.+)$"),
        new(@"^how much (.+?) is available$"),
        new(@"^how much (.+?) do we have$"),
        new(@"^how many (.+?)(?: products)? are in stock$"),
        new(@"^(.+?)\s+stock$"),
        new(@"^(.+?)\s+quantity$"),
        new(@"^(.+?)\s+available$"),
    };

    private static readonly Regex[] ProductSearchPatterns =
    {
        new(@"^find products called (.+)$"),
        new(@"^show (.+?) products$"),
        new(@"^show (.+)$"),
        new(@"^find (.+)$"),
        new(@"^search for (.+)$"),
        new(@"^search (.+)$"),
    };

    /// <summary>ExtractProductName — tries each template in order; first match wins.</summary>
    private static string? ExtractSearchTerm(string normalized, Regex[] patterns)
    {
        foreach (var pattern in patterns)
        {
            var match = pattern.Match(normalized);
            if (!match.Success)
            {
                continue;
            }

            var term = match.Groups[1].Value.Trim();
            term = Regex.Replace(term, @"^(products|items)\s+", "");
            term = Regex.Replace(term, @"\s+(products|items)$", "");

            if (!string.IsNullOrWhiteSpace(term))
            {
                return term;
            }
        }

        return null;
    }

    private static void ExtractDateRange(string normalized, AssistantQuery query)
    {
        var today = DateTime.Today;

        if (normalized.Contains("yesterday"))
        {
            var yesterday = today.AddDays(-1);
            query.StartDate = yesterday;
            query.EndDate = yesterday;
            return;
        }

        if (normalized.Contains("this week"))
        {
            var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7; // Monday = 0 ... Sunday = 6
            query.StartDate = today.AddDays(-daysSinceMonday);
            query.EndDate = today;
            return;
        }

        if (normalized.Contains("this month"))
        {
            query.StartDate = new DateTime(today.Year, today.Month, 1);
            query.EndDate = today;
            return;
        }

        // Explicit ranges like "from september 1 to september 5" or "between ... and ...".
        // DateTime.TryParse fills in the current year when the text doesn't specify one,
        // so the system date decides the year rather than any hard-coded value here.
        var rangeMatch = Regex.Match(normalized, @"(?:from|between)\s+(.+?)\s+(?:to|and)\s+(.+)");
        if (rangeMatch.Success)
        {
            var startText = rangeMatch.Groups[1].Value;
            var endText = rangeMatch.Groups[2].Value;

            // Guard against amount phrases like "between 1000 and 5000" being misread as
            // dates — a real date reference contains a month name, not just digits.
            if (LooksLikeDate(startText) && LooksLikeDate(endText) &&
                DateTime.TryParse(startText, out var start) &&
                DateTime.TryParse(endText, out var end))
            {
                query.StartDate = start.Date;
                query.EndDate = end.Date;
                return;
            }
        }

        if (normalized.Contains("today"))
        {
            query.StartDate = today;
            query.EndDate = today;
        }
    }

    private static bool LooksLikeDate(string text) => Regex.IsMatch(text, "[a-z]");

    private static void ExtractAmountRange(string normalized, AssistantQuery query)
    {
        var betweenMatch = Regex.Match(normalized, @"between\s+(?:rs\s*)?(\d+(?:\.\d+)?)\s+and\s+(?:rs\s*)?(\d+(?:\.\d+)?)");
        if (betweenMatch.Success)
        {
            query.MinimumAmount = decimal.Parse(betweenMatch.Groups[1].Value);
            query.MaximumAmount = decimal.Parse(betweenMatch.Groups[2].Value);
            return;
        }

        var aboveMatch = Regex.Match(normalized, @"(?:above|over)\s+(?:rs\s*)?(\d+(?:\.\d+)?)");
        if (aboveMatch.Success)
        {
            query.MinimumAmount = decimal.Parse(aboveMatch.Groups[1].Value);
            return;
        }

        var belowMatch = Regex.Match(normalized, @"(?:below|under)\s+(?:rs\s*)?(\d+(?:\.\d+)?)");
        if (belowMatch.Success)
        {
            query.MaximumAmount = decimal.Parse(belowMatch.Groups[1].Value);
        }
    }

    // ---------- Response building (BuildResponse) ----------

    private AssistantResponse BuildResponse(AssistantQuery query) => query.Intent switch
    {
        AssistantIntent.NavigateDashboard => Navigate("Dashboard", "Opening Dashboard."),
        AssistantIntent.NavigateProducts => Navigate("Products", "Opening Products."),
        AssistantIntent.NavigateSales => Navigate("Sales", "Opening New Sale."),
        AssistantIntent.NavigateTransactions => Navigate("Transactions", "Opening Transaction History."),

        AssistantIntent.LowStockQuery => BuildLowStockResponse(),
        AssistantIntent.ProductCountQuery => BuildProductCountResponse(),
        AssistantIntent.SalesSummary => BuildSalesSummaryResponse(query),
        AssistantIntent.TransactionQuery => BuildTransactionQueryResponse(query),
        AssistantIntent.SearchStock or AssistantIntent.ProductStockQuery => BuildStockResponse(query),
        AssistantIntent.SearchProduct => BuildProductSearchResponse(query),

        _ => BuildUnknownResponse()
    };

    private static AssistantResponse Navigate(string target, string message) => new()
    {
        Message = message,
        NavigationTarget = target
    };

    private AssistantResponse BuildLowStockResponse()
    {
        var lowStock = _productService.GetLowStockProducts();

        if (lowStock.Count == 0)
        {
            return new AssistantResponse { Message = "No products are currently below their reorder level." };
        }

        return new AssistantResponse
        {
            Message = $"There are currently {lowStock.Count} product(s) below their reorder level.\n\nWould you like to view them?",
            NavigationTarget = "Products",
            LowStockOnly = true,
            RequiresUserAction = true,
            ActionButtonText = "View Low Stock Products"
        };
    }

    private AssistantResponse BuildProductCountResponse()
    {
        var count = _dashboardService.GetActiveProductCount();
        return new AssistantResponse { Message = $"There are currently {count} active product(s)." };
    }

    private AssistantResponse BuildSalesSummaryResponse(AssistantQuery query)
    {
        var start = query.StartDate ?? DateTime.Today;
        var end = query.EndDate ?? DateTime.Today;

        var (total, count) = _dashboardService.GetSalesSummary(start, end);
        var label = start == DateTime.Today && end == DateTime.Today ? "Today's" : $"The {start:MMM dd} - {end:MMM dd}";

        return new AssistantResponse
        {
            Message = $"{label} sales total is Rs. {total:N2} from {count} transaction(s)."
        };
    }

    private AssistantResponse BuildTransactionQueryResponse(AssistantQuery query)
    {
        var sales = _salesService.SearchSales(string.Empty, query.StartDate, query.EndDate);

        // The amount filter is reported here in the chat, but is not (yet) mirrored as a
        // live filter on the Transaction History screen itself — see the documentation's
        // Limitations section for why.
        if (query.MinimumAmount.HasValue)
        {
            sales = sales.Where(s => s.Total >= query.MinimumAmount.Value).ToList();
        }

        if (query.MaximumAmount.HasValue)
        {
            sales = sales.Where(s => s.Total <= query.MaximumAmount.Value).ToList();
        }

        var period = DescribePeriod(query);

        return new AssistantResponse
        {
            Message = $"I found {sales.Count} transaction(s){period}. Opening Transaction History.",
            NavigationTarget = "Transactions",
            StartDate = query.StartDate,
            EndDate = query.EndDate
        };
    }

    private static string DescribePeriod(AssistantQuery query)
    {
        if (!query.StartDate.HasValue || !query.EndDate.HasValue)
        {
            return string.Empty;
        }

        return query.StartDate == query.EndDate
            ? $" on {query.StartDate:MMM dd, yyyy}"
            : $" between {query.StartDate:MMM dd} and {query.EndDate:MMM dd}";
    }

    /// <summary>
    /// Shared by SearchStock and ProductStockQuery — both ultimately just look up a
    /// product and report its stock. What differs is the wording, and that depends on
    /// how many products matched, not on which phrasing the user typed.
    /// </summary>
    private AssistantResponse BuildStockResponse(AssistantQuery query)
    {
        var term = query.SearchTerm ?? string.Empty;
        var matches = _productService.Search(term, includeInactive: false);

        if (matches.Count == 0)
        {
            return new AssistantResponse { Message = $"I couldn't find any products matching \"{term}\"." };
        }

        if (matches.Count == 1)
        {
            var product = matches[0];
            return new AssistantResponse { Message = $"{product.Name} currently has {product.StockQuantity} unit(s) in stock." };
        }

        var lines = matches.Take(5).Select(p => $"{p.Name} — {p.StockQuantity} units");
        var message = $"I found {matches.Count} products matching \"{term}\":\n\n" + string.Join("\n", lines);
        if (matches.Count > 5)
        {
            message += $"\n...and {matches.Count - 5} more.";
        }
        message += "\n\nOpening stock results.";

        return new AssistantResponse
        {
            Message = message,
            NavigationTarget = "Products",
            SearchTerm = term
        };
    }

    private AssistantResponse BuildProductSearchResponse(AssistantQuery query)
    {
        var term = query.SearchTerm ?? string.Empty;
        var matches = _productService.Search(term, includeInactive: false);

        if (matches.Count == 0)
        {
            return new AssistantResponse { Message = $"I couldn't find any products matching \"{term}\"." };
        }

        return new AssistantResponse
        {
            Message = $"I found {matches.Count} product(s) matching \"{term}\".\nOpening the product results.",
            NavigationTarget = "Products",
            SearchTerm = term
        };
    }

    private static AssistantResponse BuildUnknownResponse() => new()
    {
        Message = "I'm sorry, I can't handle that request yet.\n\nYou can ask me about:\n• Products\n• Stock\n• Sales\n• Transactions\n• Dashboard"
    };
}
