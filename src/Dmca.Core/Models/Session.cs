namespace Dmca.Core.Models;

/// <summary>
/// Top-level workflow container. Matches session.json schema.
/// </summary>
public sealed class Session
{
    public required Guid Id { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; }
    public SessionStatus Status { get; set; }
    public required string AppVersion { get; init; }
}
