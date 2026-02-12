namespace Dmca.Core.Models;

/// <summary>
/// A single change within a proposal.
/// Matches proposal.json → ProposalChange schema.
/// </summary>
public sealed class ProposalChange
{
    /// <summary>
    /// Type of change: score_delta, recommendation, pin_protect, action_add,
    /// action_remove, note_add, fact_request.
    /// </summary>
    public required string Type { get; init; }

    /// <summary>Target inventory item ID (drv:|svc:|pkg:|app: prefix).</summary>
    public required string TargetId { get; init; }

    /// <summary>For score_delta: the delta value.</summary>
    public int? Delta { get; init; }

    /// <summary>For recommendation/pin_protect: the value to set.</summary>
    public string? Value { get; init; }

    /// <summary>Freeform note.</summary>
    public string? Note { get; init; }

    /// <summary>Human-readable reason for the change. Required.</summary>
    public required string Reason { get; init; }

    /// <summary>For fact_request: the requested fact details.</summary>
    public FactRequest? FactRequest { get; init; }
}

/// <summary>
/// A request from the AI for user-provided facts.
/// </summary>
public sealed class FactRequest
{
    public string? FactKey { get; init; }
    public string? Prompt { get; init; }
}

/// <summary>
/// Evidence supporting a proposal change.
/// Matches proposal.json → Evidence schema.
/// </summary>
public sealed class Evidence
{
    public required string Kind { get; init; }
    public string? Path { get; init; }
    public object? Value { get; init; }
    public string? Note { get; init; }
}
