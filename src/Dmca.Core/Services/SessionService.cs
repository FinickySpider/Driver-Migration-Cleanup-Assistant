using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.Core.Services;

/// <summary>
/// Service for creating and managing sessions.
/// </summary>
public sealed class SessionService
{
    private const string AppVersion = "1.0.0";

    private readonly ISessionRepository _sessionRepo;

    public SessionService(ISessionRepository sessionRepo)
    {
        _sessionRepo = sessionRepo;
    }

    /// <summary>
    /// Creates a new session with status NEW.
    /// </summary>
    public async Task<Session> CreateSessionAsync()
    {
        var now = DateTime.UtcNow;
        var session = new Session
        {
            Id = Guid.NewGuid(),
            CreatedAt = now,
            UpdatedAt = now,
            Status = SessionStatus.NEW,
            AppVersion = AppVersion,
        };

        await _sessionRepo.CreateAsync(session);
        return session;
    }

    /// <summary>
    /// Retrieves a session by ID.
    /// </summary>
    public async Task<Session?> GetSessionAsync(Guid id) =>
        await _sessionRepo.GetByIdAsync(id);

    /// <summary>
    /// Gets the most recent session.
    /// </summary>
    public async Task<Session?> GetCurrentSessionAsync() =>
        await _sessionRepo.GetCurrentAsync();

    /// <summary>
    /// Transitions a session to the given status, enforcing valid state transitions.
    /// </summary>
    public async Task TransitionAsync(Guid sessionId, SessionStatus newStatus)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId)
            ?? throw new InvalidOperationException($"Session {sessionId} not found.");

        SessionStateMachine.ValidateTransition(session.Status, newStatus);

        session.Status = newStatus;
        session.UpdatedAt = DateTime.UtcNow;
        await _sessionRepo.UpdateAsync(session);
    }
}
