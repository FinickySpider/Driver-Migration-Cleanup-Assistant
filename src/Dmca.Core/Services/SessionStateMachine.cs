using Dmca.Core.Models;

namespace Dmca.Core.Services;

/// <summary>
/// Defines valid session status transitions.
/// </summary>
public static class SessionStateMachine
{
    private static readonly Dictionary<SessionStatus, HashSet<SessionStatus>> ValidTransitions = new()
    {
        [SessionStatus.NEW] = [SessionStatus.SCANNED, SessionStatus.FAILED],
        [SessionStatus.SCANNED] = [SessionStatus.PLANNED, SessionStatus.FAILED],
        [SessionStatus.PLANNED] = [SessionStatus.PENDING_APPROVALS, SessionStatus.READY_TO_EXECUTE, SessionStatus.FAILED],
        [SessionStatus.PENDING_APPROVALS] = [SessionStatus.READY_TO_EXECUTE, SessionStatus.PLANNED, SessionStatus.FAILED],
        [SessionStatus.READY_TO_EXECUTE] = [SessionStatus.EXECUTING, SessionStatus.FAILED],
        [SessionStatus.EXECUTING] = [SessionStatus.COMPLETED, SessionStatus.FAILED],
        [SessionStatus.COMPLETED] = [],
        [SessionStatus.FAILED] = [SessionStatus.NEW],
    };

    /// <summary>
    /// Returns true if transitioning from <paramref name="from"/> to <paramref name="to"/> is valid.
    /// </summary>
    public static bool CanTransition(SessionStatus from, SessionStatus to)
    {
        return ValidTransitions.TryGetValue(from, out var targets) && targets.Contains(to);
    }

    /// <summary>
    /// Throws if the transition is invalid.
    /// </summary>
    public static void ValidateTransition(SessionStatus from, SessionStatus to)
    {
        if (!CanTransition(from, to))
        {
            throw new InvalidOperationException(
                $"Invalid session status transition: {from} → {to}");
        }
    }
}
