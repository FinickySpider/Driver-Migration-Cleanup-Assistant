using Dmca.Evals.Models;
using Dmca.Evals.MockApi;

namespace Dmca.Evals.ModelClients;

public sealed class OpenAIModelClient : IModelClient
{
    public Task<(string assistantText, List<ToolCall> toolCalls)> RunAsync(
        Fixture fixture,
        MockDmcaApiServer toolRouter,
        CancellationToken ct)
    {
        throw new NotImplementedException(
            "Wire this to OpenAI tool-calling. Use ai/openai_tools.json and route tool calls to toolRouter.CallTool().");
    }
}
