using Dmca.Core.Models;

namespace Dmca.Core.Execution.Actions;

/// <summary>
/// Abstraction for a single executable action.
/// Each implementation wraps the OS-level command for its action type.
/// </summary>
public interface IActionHandler
{
    /// <summary>
    /// The action type this handler handles.
    /// </summary>
    ActionType HandledType { get; }

    /// <summary>
    /// Executes the action.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="mode">Live or dry-run.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result with success flag, command run, output, and error (if any).</returns>
    Task<ActionResult> ExecuteAsync(ExecutionAction action, ExecutionMode mode, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of executing a single action.
/// </summary>
public sealed record ActionResult(
    bool Success,
    string? Command = null,
    string? Output = null,
    string? ErrorMessage = null);
