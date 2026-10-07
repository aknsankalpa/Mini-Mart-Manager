namespace RetailFlow.Services;

/// <summary>
/// Runs one question through the local model. The model may call the functions in
/// AssistantToolService; their results go back to it, and it then writes the answer.
/// </summary>
public class AiAssistant
{
    private const int MaxToolRounds = 3;

    private readonly OllamaClient _client = new();
    private readonly AssistantToolService _tools = new();

    /// <summary>
    /// Returns the answer, or null when the question can't be answered from this store's
    /// data (the caller then shows the out-of-scope message).
    /// </summary>
    public async Task<string?> AnswerAsync(string question)
    {
        var messages = new List<OllamaMessage>
        {
            new("system", BuildSystemPrompt()),
            new("user", question)
        };

        for (var round = 0; round < MaxToolRounds; round++)
        {
            var reply = await _client.ChatAsync(messages, AssistantToolService.Definitions);

            if (reply.ToolCalls is null || reply.ToolCalls.Count == 0)
            {
                // A reply without reading the store's data would mean inventing figures, so
                // answering without a tool only ever counts as "out of scope".
                return round == 0 ? null : reply.Content.Trim();
            }

            messages.Add(reply);
            foreach (var call in reply.ToolCalls)
            {
                var result = await _tools.ExecuteAsync(call.Function.Name, call.Function.Arguments);
                messages.Add(new OllamaMessage("tool", result));
            }
        }

        return null;
    }

    private static string BuildSystemPrompt() =>
        $"""
        You are the MiniMart Assistant for a small retail store. Today's date is {DateTime.Today:yyyy-MM-dd}.
        You answer ONLY from the store's own data, and you can read that data only through the provided functions.
        Always call a function to get figures; never invent product names, stock, prices or sales.
        For periods such as "last 30 days", pass the number of days (today counts as 1).
        If the question is not about this store's products, stock, sales, transactions or dashboard
        (for example weather, news, other shops or general knowledge), do not call any function.
        Answer briefly and in plain English, quoting the figures the functions return.
        """;
}
