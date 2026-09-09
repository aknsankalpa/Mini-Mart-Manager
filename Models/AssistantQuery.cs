namespace RetailFlow.Models;

/// <summary>
/// The structured result of interpreting one natural-language request typed into the
/// MiniMart Assistant. Fields are nullable because most of them only apply to a subset
/// of intents — a navigation command has no date range, a date query has no amount, etc.
/// SearchTerm is deliberately the one field used for both "product name" and "search
/// term" cases (searching for a product and asking about a product's stock are really
/// the same lookup), so there is no separate, always-identical ProductName property.
/// </summary>
public class AssistantQuery
{
    public string OriginalQuery { get; init; } = string.Empty;
    public AssistantIntent Intent { get; set; } = AssistantIntent.Unknown;
    public string? SearchTerm { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? MinimumAmount { get; set; }
    public decimal? MaximumAmount { get; set; }
}
