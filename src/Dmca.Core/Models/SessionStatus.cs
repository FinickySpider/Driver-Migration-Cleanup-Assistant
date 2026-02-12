namespace Dmca.Core.Models;

/// <summary>
/// Session status lifecycle:
/// NEW → SCANNED → PLANNED → PENDING_APPROVALS → READY_TO_EXECUTE → EXECUTING → COMPLETED | FAILED
/// </summary>
public enum SessionStatus
{
    NEW,
    SCANNED,
    PLANNED,
    PENDING_APPROVALS,
    READY_TO_EXECUTE,
    EXECUTING,
    COMPLETED,
    FAILED
}
