namespace Dmca.Evals.Models;

public sealed class ToolCall
{
    public required string Name { get; init; }
    public required string ArgumentsJson { get; init; }
}
