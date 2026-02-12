using System.Diagnostics;
using Dmca.Core.Models;

namespace Dmca.Core.Execution.Actions;

/// <summary>
/// Creates a system restore point using PowerShell.
/// Failure of this action MUST block all subsequent destructive actions.
/// </summary>
public sealed class RestorePointAction : IActionHandler
{
    public ActionType HandledType => ActionType.CREATE_RESTORE_POINT;

    public async Task<ActionResult> ExecuteAsync(ExecutionAction action, ExecutionMode mode, CancellationToken cancellationToken = default)
    {
        const string command = "powershell.exe -NoProfile -Command \"Checkpoint-Computer -Description 'DMCA Pre-Cleanup' -RestorePointType MODIFY_SETTINGS\"";

        if (mode == ExecutionMode.DRY_RUN)
        {
            return new ActionResult(Success: true, Command: command, Output: "[DRY RUN] Would create system restore point.");
        }

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -Command \"Checkpoint-Computer -Description 'DMCA Pre-Cleanup' -RestorePointType MODIFY_SETTINGS\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            return process.ExitCode == 0
                ? new ActionResult(Success: true, Command: command, Output: output.Trim())
                : new ActionResult(Success: false, Command: command, Output: output.Trim(), ErrorMessage: error.Trim());
        }
        catch (Exception ex)
        {
            return new ActionResult(Success: false, Command: command, ErrorMessage: ex.Message);
        }
    }
}
