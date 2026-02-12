namespace Dmca.Evals.Models;

public sealed class EvalResult
{
    public required string ScenarioId { get; init; }
    public required bool Pass { get; init; }
    public List<string> FailReasons { get; init; } = new();

    public Dictionary<string, double> Metrics { get; init; } = new();
    public List<ToolCall> ToolCalls { get; init; } = new();
    public List<string> CreatedProposalJson { get; init; } = new();
    public string? AssistantText { get; init; }
}
