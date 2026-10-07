using System.Collections.ObjectModel;
using System.Windows.Input;
using RetailFlow.Helpers;
using RetailFlow.Models;
using RetailFlow.Services;

namespace RetailFlow.ViewModels;

/// <summary>
/// Drives the MiniMart Assistant screen: keeps the chat transcript, sends each typed
/// query to AssistantQueryService, and raises NavigationRequested so the application
/// layer (MainWindow) — not this class — performs the actual screen switch. Keeping
/// navigation out of here (and out of AssistantQueryService) is what lets the assistant
/// stay a plain interpretation layer with no WPF control access of its own.
/// </summary>
public class AssistantViewModel : ViewModelBase
{
    private readonly AssistantQueryService _assistantQueryService = new();

    public ObservableCollection<AssistantChatMessage> Messages { get; } = new();

    private string _inputText = string.Empty;
    public string InputText { get => _inputText; set => SetField(ref _inputText, value); }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; private set => SetField(ref _isBusy, value); }

    public ICommand SendCommand { get; }
    public ICommand QuickActionCommand { get; }

    /// <summary>Raised when a response should navigate the main window to another screen.</summary>
    public event EventHandler<AssistantResponse>? NavigationRequested;

    public AssistantViewModel()
    {
        SendCommand = new RelayCommand(_ => _ = SendAsync(), _ => !IsBusy);
        QuickActionCommand = new RelayCommand(parameter =>
        {
            if (parameter is not string quickQuery)
            {
                return;
            }

            InputText = quickQuery;

            // A chip whose query ends with a trailing space (e.g. "find ") is a prompt
            // to complete, not a ready-to-run command — it just fills the box so the
            // user can type the rest and press Enter themselves.
            if (!quickQuery.EndsWith(' '))
            {
                _ = SendAsync();
            }
        });

        Messages.Add(new AssistantChatMessage
        {
            IsUser = false,
            Text = "Hi! Ask me about products, stock, sales or transactions — or tell me where you'd like to go."
        });
    }

    private async Task SendAsync()
    {
        var userQuery = InputText.Trim();

        if (string.IsNullOrWhiteSpace(userQuery))
        {
            Messages.Add(new AssistantChatMessage { IsUser = false, Text = "Please enter a question or command." });
            return;
        }

        Messages.Add(new AssistantChatMessage { IsUser = true, Text = userQuery });
        InputText = string.Empty;

        // The AI model can take several seconds, so the screen shows a placeholder and
        // stays responsive instead of freezing until the answer arrives.
        var thinking = new AssistantChatMessage { IsUser = false, Text = "Thinking…" };
        Messages.Add(thinking);

        AssistantResponse response;
        IsBusy = true;
        CommandManager.InvalidateRequerySuggested();
        try
        {
            response = await _assistantQueryService.InterpretAsync(userQuery);
        }
        finally
        {
            IsBusy = false;
            Messages.Remove(thinking);
            CommandManager.InvalidateRequerySuggested();
        }

        ICommand? actionCommand = null;
        if (response.NavigationTarget is not null)
        {
            actionCommand = new RelayCommand(_ => NavigationRequested?.Invoke(this, response));
        }

        Messages.Add(new AssistantChatMessage
        {
            IsUser = false,
            Text = response.Message,
            ActionButtonText = response.RequiresUserAction ? response.ActionButtonText : null,
            ActionCommand = response.RequiresUserAction ? actionCommand : null
        });

        // A direct command like "go to products" or "tea stock" navigates immediately —
        // that is the whole point of asking. Informational results that merely offer a
        // related screen (e.g. a low-stock summary) wait for the user to opt in via the
        // action button instead of jumping away from the conversation on their own.
        if (response.NavigationTarget is not null && !response.RequiresUserAction)
        {
            NavigationRequested?.Invoke(this, response);
        }
    }
}
