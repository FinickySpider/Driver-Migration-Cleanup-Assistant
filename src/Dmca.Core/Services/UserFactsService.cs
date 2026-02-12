using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.Core.Services;

/// <summary>
/// Service for managing user facts.
/// </summary>
public sealed class UserFactsService
{
    private readonly IUserFactRepository _factRepo;

    public UserFactsService(IUserFactRepository factRepo)
    {
        _factRepo = factRepo;
    }

    /// <summary>
    /// Adds a single user-provided fact to a session.
    /// </summary>
    public async Task AddFactAsync(Guid sessionId, string key, string value, FactSource source = FactSource.USER)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var fact = new UserFact
        {
            SessionId = sessionId,
            Key = key,
            Value = value,
            Source = source,
            CreatedAt = DateTime.UtcNow,
        };

        await _factRepo.AddAsync(fact);
    }

    /// <summary>
    /// Gets all facts for a session.
    /// </summary>
    public async Task<IReadOnlyList<UserFact>> GetFactsAsync(Guid sessionId) =>
        await _factRepo.GetBySessionIdAsync(sessionId);
}
