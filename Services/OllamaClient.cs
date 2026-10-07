using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RetailFlow.Services;

public record OllamaMessage(
    string Role,
    string Content,
    [property: JsonPropertyName("tool_calls")] List<OllamaToolCall>? ToolCalls = null);

public record OllamaToolCall(OllamaFunctionCall Function);

public record OllamaFunctionCall(string Name, JsonElement Arguments);

internal record OllamaChatResponse(OllamaMessage Message);

/// <summary>
/// Talks to the Ollama server running on this PC (localhost only), so store data never
/// leaves the machine.
/// </summary>
public class OllamaClient
{
    public const string ModelName = "qwen2.5:3b";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    // The first request after Ollama starts can take a while because the model is loaded
    // into memory, so the timeout is generous.
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("http://localhost:11434"),
        Timeout = TimeSpan.FromSeconds(120)
    };

    public async Task<OllamaMessage> ChatAsync(IReadOnlyList<OllamaMessage> messages, object tools)
    {
        var request = new { model = ModelName, stream = false, messages, tools, options = new { temperature = 0 } };

        using var response = await Http.PostAsync("/api/chat", JsonContent.Create(request, options: JsonOptions));
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<OllamaChatResponse>(JsonOptions);
        return body?.Message ?? throw new InvalidOperationException("Ollama returned an empty reply.");
    }
}
