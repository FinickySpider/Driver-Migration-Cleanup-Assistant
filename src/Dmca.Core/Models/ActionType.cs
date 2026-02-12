namespace Dmca.Core.Models;

/// <summary>
/// Types of executable actions.
/// </summary>
public enum ActionType
{
    CREATE_RESTORE_POINT,
    UNINSTALL_DRIVER_PACKAGE,
    DISABLE_SERVICE,
    UNINSTALL_PROGRAM,
}

/// <summary>
/// Execution status of a single action.
/// </summary>
public enum ActionStatus
{
    PENDING,
    RUNNING,
    COMPLETED,
    FAILED,
    SKIPPED,
    DRY_RUN,
    CANCELLED,
}

/// <summary>
/// Execution mode for the action queue.
/// </summary>
public enum ExecutionMode
{
    LIVE,
    DRY_RUN,
}
