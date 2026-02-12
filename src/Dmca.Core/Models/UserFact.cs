namespace Dmca.Core.Models;

/// <summary>
/// An individual fact about the user's migration context.
/// Matches userFacts.json → facts[] schema.
/// </summary>
public sealed class UserFact
{
    public required Guid SessionId { get; init; }
    public required string Key { get; init; }
    public required string Value { get; init; }
    public required FactSource Source { get; init; }
    public required DateTime CreatedAt { get; init; }
}
