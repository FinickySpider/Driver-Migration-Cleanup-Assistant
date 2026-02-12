using Dmca.Evals.Models;
using Dmca.Evals.MockApi;

namespace Dmca.Evals.ModelClients;

public interface IModelClient
{
    Task<(string assistantText, List<ToolCall> toolCalls)> RunAsync(
        Fixture fixture,
        MockDmcaApiServer toolRouter,
        CancellationToken ct);
}
