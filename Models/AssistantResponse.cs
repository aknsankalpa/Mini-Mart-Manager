namespace RetailFlow.Models;

/// <summary>
/// What the assistant hands back to the UI after interpreting a query: a message to
/// display, and — for intents that should open a screen — enough context for the
/// application layer (not this class) to perform that navigation and apply the same
/// filter the user asked about. AssistantQueryService only ever returns this structured
/// object; it never touches a WPF control directly.
/// </summary>
public class AssistantResponse
{
    public string Message { get; set; } = string.Empty;
    public AssistantIntent Intent { get; set; } = AssistantIntent.Unknown;

    /// <summary>"Dashboard", "Products", "Sales", or "Transactions" — null means stay put.</summary>
    public string? NavigationTarget { get; set; }

    public string? SearchTerm { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool LowStockOnly { get; set; }

    /// <summary>
    /// True when navigation should wait for the user to click a follow-up button
    /// (e.g. a low-stock summary) rather than happening immediately, the way a direct
    /// "go to products" command does.
    /// </summary>
    public bool RequiresUserAction { get; set; }

    public string? ActionButtonText { get; set; }
}
