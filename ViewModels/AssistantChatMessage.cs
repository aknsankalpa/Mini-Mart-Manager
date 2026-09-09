using System.Windows.Input;

namespace RetailFlow.ViewModels;

/// <summary>
/// One line of the MiniMart Assistant's conversation transcript — either something the
/// user typed, or the assistant's reply. A reply may carry its own follow-up action
/// (e.g. "View Low Stock Products"), which is why this isn't just a plain string.
/// </summary>
public class AssistantChatMessage
{
    public bool IsUser { get; init; }
    public string Text { get; init; } = string.Empty;
    public string? ActionButtonText { get; init; }
    public ICommand? ActionCommand { get; init; }
}
