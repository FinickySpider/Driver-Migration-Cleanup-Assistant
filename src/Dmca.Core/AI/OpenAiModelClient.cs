using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Dmca.Core.AI;

/// <summary>
/// OpenAI API client using chat completions with tool-calling.
/// Sends conversation messages and tool definitions, returns structured responses.
/// </summary>
public sealed class OpenAiModelClient : IAiModelClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly string _toolDefinitionsJson;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    public OpenAiModelClient(string apiKey, string model = "gpt-4o", string? toolDefinitionsJson = null)
    {
        _model = model;
        _toolDefinitionsJson = toolDefinitionsJson ?? "[]";

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.openai.com/v1/"),
            Timeout = TimeSpan.FromSeconds(60),
        };
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<AiResponse> SendAsync(ConversationManager conversation, CancellationToken cancellationToken = default)
    {
        var requestBody = BuildRequestBody(conversation);
        var content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("chat/completions", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return ParseResponse(responseJson);
    }

    private string BuildRequestBody(ConversationManager conversation)
    {
        var tools = JsonSerializer.Deserialize<JsonElement>(_toolDefinitionsJson);
        var messages = JsonSerializer.Deserialize<JsonElement>(conversation.ToJson());

        var request = new Dictionary<string, object>
        {
            ["model"] = _model,
            ["messages"] = messages,
        };

        if (tools.ValueKind == JsonValueKind.Array && tools.GetArrayLength() > 0)
            request["tools"] = tools;

        return JsonSerializer.Serialize(request, JsonOpts);
    }

    private static AiResponse ParseResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var choice = doc.RootElement.GetProperty("choices")[0];
        var message = choice.GetProperty("message");
        var finishReason = choice.GetProperty("finish_reason").GetString() ?? "unknown";

        var textContent = message.TryGetProperty("content", out var c) && c.ValueKind != JsonValueKind.Null
            ? c.GetString()
            : null;

        var toolCalls = new List<ToolCallMessage>();
        if (message.TryGetProperty("tool_calls", out var tc) && tc.ValueKind == JsonValueKind.Array)
        {
            foreach (var call in tc.EnumerateArray())
            {
                var fn = call.GetProperty("function");
                toolCalls.Add(new ToolCallMessage
                {
                    Id = call.GetProperty("id").GetString()!,
                    Type = call.GetProperty("type").GetString()!,
                    Function = new ToolCallFunction
                    {
                        Name = fn.GetProperty("name").GetString()!,
                        Arguments = fn.GetProperty("arguments").GetString()!,
                    },
                });
            }
        }

        return new AiResponse
        {
            Content = textContent,
            ToolCalls = toolCalls.AsReadOnly(),
            FinishReason = finishReason,
        };
    }

    public void Dispose() => _httpClient.Dispose();
}
