using System.Diagnostics;
using Dmca.Core.Models;

namespace Dmca.Core.Execution.Actions;

/// <summary>
/// Stops and disables a Windows service.
/// First stops the service (if running), then sets start type to Disabled.
/// </summary>
public sealed class ServiceDisableAction : IActionHandler
{
    public ActionType HandledType => ActionType.DISABLE_SERVICE;

    public async Task<ActionResult> ExecuteAsync(ExecutionAction action, ExecutionMode mode, CancellationToken cancellationToken = default)
    {
        var serviceName = ExtractServiceName(action.TargetId);
        if (string.IsNullOrWhiteSpace(serviceName))
        {
            return new ActionResult(Success: false, ErrorMessage: $"Cannot extract service name from target '{action.TargetId}'.");
        }

        var stopCommand = $"sc.exe stop \"{serviceName}\"";
        var disableCommand = $"sc.exe config \"{serviceName}\" start=disabled";
        var fullCommand = $"{stopCommand} && {disableCommand}";

        if (mode == ExecutionMode.DRY_RUN)
        {
            return new ActionResult(Success: true, Command: fullCommand, Output: $"[DRY RUN] Would execute:\n  1. {stopCommand}\n  2. {disableCommand}");
        }

        try
        {
            // Step 1: Stop the service (ignore errors — it may already be stopped)
            var stopResult = await RunCommandAsync("sc.exe", $"stop \"{serviceName}\"", cancellationToken);

            // Step 2: Disable the service
            var disableResult = await RunCommandAsync("sc.exe", $"config \"{serviceName}\" start=disabled", cancellationToken);

            var combinedOutput = string.Join("\n",
                $"[STOP] Exit={stopResult.ExitCode}: {stopResult.Output}",
                $"[DISABLE] Exit={disableResult.ExitCode}: {disableResult.Output}");

            // Disable is the critical step
            return disableResult.ExitCode == 0
                ? new ActionResult(Success: true, Command: fullCommand, Output: combinedOutput)
                : new ActionResult(Success: false, Command: fullCommand, Output: combinedOutput, ErrorMessage: disableResult.Error);
        }
        catch (Exception ex)
        {
            return new ActionResult(Success: false, Command: fullCommand, ErrorMessage: ex.Message);
        }
    }

    /// <summary>
    /// Extracts service name from item ID: "svc:MyService" → "MyService".
    /// </summary>
    internal static string? ExtractServiceName(string targetId)
    {
        var colonIndex = targetId.IndexOf(':');
        if (colonIndex < 0 || colonIndex >= targetId.Length - 1)
            return null;

        return targetId[(colonIndex + 1)..].Trim();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCommandAsync(
        string fileName, string arguments, CancellationToken ct)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync(ct);
        var error = await process.StandardError.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        return (process.ExitCode, output.Trim(), error.Trim());
    }
}
