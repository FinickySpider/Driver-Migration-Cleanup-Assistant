using System.Diagnostics;
using Dmca.Core.Models;

namespace Dmca.Core.Execution.Actions;

/// <summary>
/// Uninstalls a program using its quiet uninstall string, or falls back to
/// the regular uninstall string with common silent switches.
/// Default timeout: 120 seconds.
/// </summary>
public sealed class ProgramUninstallAction : IActionHandler
{
    /// <summary>
    /// Default timeout for program uninstall in seconds.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 120;

    public ActionType HandledType => ActionType.UNINSTALL_PROGRAM;

    public async Task<ActionResult> ExecuteAsync(ExecutionAction action, ExecutionMode mode, CancellationToken cancellationToken = default)
    {
        // The Command field should be populated by the ActionQueueBuilder with the uninstall string
        var uninstallCommand = action.Command;
        if (string.IsNullOrWhiteSpace(uninstallCommand))
        {
            return new ActionResult(Success: false, ErrorMessage: $"No uninstall command available for '{action.TargetId}'.");
        }

        if (mode == ExecutionMode.DRY_RUN)
        {
            return new ActionResult(Success: true, Command: uninstallCommand, Output: $"[DRY RUN] Would execute: {uninstallCommand}");
        }

        try
        {
            var (fileName, arguments) = ParseCommand(uninstallCommand);

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

            // Apply timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(TimeoutSeconds));

            try
            {
                var output = await process.StandardOutput.ReadToEndAsync(cts.Token);
                var error = await process.StandardError.ReadToEndAsync(cts.Token);
                await process.WaitForExitAsync(cts.Token);

                return process.ExitCode == 0
                    ? new ActionResult(Success: true, Command: uninstallCommand, Output: output.Trim())
                    : new ActionResult(Success: false, Command: uninstallCommand, Output: output.Trim(), ErrorMessage: error.Trim());
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout — not user cancellation
                try { process.Kill(); } catch { /* best effort */ }
                return new ActionResult(Success: false, Command: uninstallCommand, ErrorMessage: $"Uninstall timed out after {TimeoutSeconds}s.");
            }
        }
        catch (Exception ex)
        {
            return new ActionResult(Success: false, Command: uninstallCommand, ErrorMessage: ex.Message);
        }
    }

    /// <summary>
    /// Parses a command line into fileName and arguments.
    /// Handles quoted paths like: "C:\Program Files\App\uninstall.exe" /S
    /// </summary>
    internal static (string FileName, string Arguments) ParseCommand(string commandLine)
    {
        commandLine = commandLine.Trim();

        if (commandLine.StartsWith('"'))
        {
            var closingQuote = commandLine.IndexOf('"', 1);
            if (closingQuote > 0)
            {
                var fileName = commandLine[1..closingQuote];
                var args = closingQuote + 1 < commandLine.Length
                    ? commandLine[(closingQuote + 1)..].Trim()
                    : "";
                return (fileName, args);
            }
        }

        // No quotes — split on first space
        var spaceIndex = commandLine.IndexOf(' ');
        if (spaceIndex > 0)
        {
            return (commandLine[..spaceIndex], commandLine[(spaceIndex + 1)..].Trim());
        }

        return (commandLine, "");
    }
}
