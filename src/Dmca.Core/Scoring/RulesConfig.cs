namespace Dmca.Core.Scoring;

/// <summary>
/// Strongly-typed configuration loaded from rules.yml.
/// Immutable after construction.
/// </summary>
public sealed class RulesConfig
{
    public required int Version { get; init; }
    public required Limits Limits { get; init; }
    public required IReadOnlyList<RecommendationBand> RecommendationBands { get; init; }
    public required IReadOnlyList<HardBlockDefinition> HardBlocks { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<string>> KeywordSets { get; init; }
    public required IReadOnlyList<SignalDefinition> Signals { get; init; }
    public required PostRules PostRules { get; init; }
}

public sealed class Limits
{
    public required int ScoreMin { get; init; }
    public required int ScoreMax { get; init; }
    public required int AiDeltaMin { get; init; }
    public required int AiDeltaMax { get; init; }
    public required int AiDeltaMaxWithUserFact { get; init; }
}

public sealed class RecommendationBand
{
    public required int Min { get; init; }
    public required int Max { get; init; }
    public required string Recommendation { get; init; }
}

public sealed class HardBlockDefinition
{
    public required string Code { get; init; }
    public required ConditionGroup When { get; init; }
    public required string Message { get; init; }
    /// <summary>Optional type filter. If set, block only applies to these item types.</summary>
    public IReadOnlyList<string>? AppliesToTypes { get; init; }
}

public sealed class SignalDefinition
{
    public required string Id { get; init; }
    public required int Weight { get; init; }
    public required ConditionGroup When { get; init; }
    public required string Rationale { get; init; }
    /// <summary>Optional type filter.</summary>
    public IReadOnlyList<string>? AppliesToTypes { get; init; }
    /// <summary>Optional user-fact requirement.</summary>
    public ConditionGroup? RequiresUserFact { get; init; }
}

/// <summary>
/// Groups of conditions using all (AND) or any (OR) semantics.
/// </summary>
public sealed class ConditionGroup
{
    /// <summary>All conditions must match (AND).</summary>
    public IReadOnlyList<Condition>? All { get; init; }
    /// <summary>At least one condition must match (OR).</summary>
    public IReadOnlyList<Condition>? Any { get; init; }
}

/// <summary>
/// A single condition evaluated against an inventory item or user fact.
/// </summary>
public sealed class Condition
{
    /// <summary>Dot-path into item, e.g. "item.signature.isMicrosoft" or "item.vendor".</summary>
    public string? Path { get; init; }
    public string? Op { get; init; }
    public object? Value { get; init; }
    /// <summary>For matches_keywords operator — references a keyword_set key.</summary>
    public string? Keywords { get; init; }
    /// <summary>For user-fact conditions: the fact key to match.</summary>
    public string? Key { get; init; }
    /// <summary>For user-fact conditions: case-insensitive value match.</summary>
    public string? EqualsI { get; init; }
}

public sealed class PostRules
{
    public bool ClampScore { get; init; }
    public bool ComputeFinalScore { get; init; }
}
