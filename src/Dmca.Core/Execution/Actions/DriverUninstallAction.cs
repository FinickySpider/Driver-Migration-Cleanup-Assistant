using System.Diagnostics;
using Dmca.Core.Models;

namespace Dmca.Core.Execution.Actions;

/// <summary>
/// Removes a driver package from the driver store using pnputil.
/// Target ID follows the pattern: drv:{inf} or pkg:{publishedName}.
/// </summary>
public sealed class DriverUninstallAction : IActionHandler
{
    public ActionType HandledType => ActionType.UNINSTALL_DRIVER_PACKAGE;

    public async Task<ActionResult> ExecuteAsync(ExecutionAction action, ExecutionMode mode, CancellationToken cancellationToken = default)
    {
        // Extract the driver published name from the target ID
        var publishedName = ExtractPublishedName(action.TargetId);
        if (string.IsNullOrWhiteSpace(publishedName))
        {
            return new ActionResult(Success: false, ErrorMessage: $"Cannot extract driver published name from target '{action.TargetId}'.");
        }

        var command = $"pnputil /delete-driver {publishedName} /force";

        if (mode == ExecutionMode.DRY_RUN)
        {
            return new ActionResult(Success: true, Command: command, Output: $"[DRY RUN] Would execute: {command}");
        }

        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "pnputil",
                Arguments = $"/delete-driver {publishedName} /force",
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

    /// <summary>
    /// Extracts published name from item ID.
    /// Patterns: "drv:oem42.inf" → "oem42.inf", "pkg:oem42.inf" → "oem42.inf".
    /// </summary>
    internal static string? ExtractPublishedName(string targetId)
    {
        var colonIndex = targetId.IndexOf(':');
        if (colonIndex < 0 || colonIndex >= targetId.Length - 1)
            return null;

        return targetId[(colonIndex + 1)..].Trim();
    }
}
