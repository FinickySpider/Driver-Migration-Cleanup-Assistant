using Dmca.Evals.MockApi;
using Dmca.Evals.ModelClients;
using Dmca.Evals.Models;
using Dmca.Evals.Policy;

namespace Dmca.Evals;

public sealed class EvalRunner
{
    private readonly EvalPolicy _policy = new();

    public async Task<EvalResult> RunAsync(Fixture fixture, IModelClient model, CancellationToken ct)
    {
        var store = new MockDataStore(fixture);
        var tools = new MockDmcaApiServer(store);

        string assistantText;
        List<ToolCall> toolCalls;

        try
        {
            (assistantText, toolCalls) = await model.RunAsync(fixture, tools, ct);
        }
        catch (Exception ex)
        {
            return new EvalResult
            {
                ScenarioId = fixture.Id,
                Pass = false,
                FailReasons = new List<string> { $"Model client crashed: {ex.GetType().Name}: {ex.Message}" }
            };
        }

        var fails = new List<string>();
        fails.AddRange(Validators.ValidateToolDiscipline(toolCalls, _policy));
        fails.AddRange(Validators.ValidateAssistantText(assistantText, _policy));
        fails.AddRange(Validators.ValidateProposals(fixture, tools.CreatedProposalsJson, _policy));

        if (fixture.Expect.MustAskToRetry && (assistantText?.Contains("retry", StringComparison.OrdinalIgnoreCase) != true))
            fails.Add("Expected assistant to ask to retry/rescan, but it did not.");

        var metrics = Validators.ComputeMetrics(fixture, toolCalls, tools.CreatedProposalsJson, _policy);

        return new EvalResult
        {
            ScenarioId = fixture.Id,
            Pass = fails.Count == 0,
            FailReasons = fails,
            Metrics = metrics,
            ToolCalls = toolCalls,
            CreatedProposalJson = tools.CreatedProposalsJson,
            AssistantText = assistantText
        };
    }
}
