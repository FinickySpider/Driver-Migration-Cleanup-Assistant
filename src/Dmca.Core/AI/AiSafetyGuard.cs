using System.Text.RegularExpressions;
using Dmca.Core.Models;

namespace Dmca.Core.AI;

/// <summary>
/// Safety guardrails for AI advisor output.
/// Validates proposals, detects forbidden phrases, and enforces constraints.
/// </summary>
public static partial class AiSafetyGuard
{
    /// <summary>
    /// Maximum number of changes per proposal.
    /// </summary>
    public const int MaxChangesPerProposal = 5;

    /// <summary>
    /// Default max AI delta (without user-fact confirmation).
    /// </summary>
    public const int DefaultMaxDelta = 25;

    /// <summary>
    /// Max AI delta with explicit user-fact confirmation.
    /// </summary>
    public const int UserFactMaxDelta = 40;

    /// <summary>
    /// Forbidden phrases that indicate the AI is trying to execute or auto-approve.
    /// Case-insensitive match.
    /// </summary>
    private static readonly string[] ForbiddenPhrases =
    [
        "auto-approve",
        "approve automatically",
        "executing now",
        "i will execute",
        "i executed",
        "i'm going to run the uninstall",
        "i am going to run the uninstall",
    ];

    /// <summary>
    /// Checks AI text output for forbidden phrases.
    /// Returns a list of detected violations (empty if clean).
    /// </summary>
    public static IReadOnlyList<string> DetectForbiddenPhrases(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var lower = text.ToLowerInvariant();
        return ForbiddenPhrases
            .Where(phrase => lower.Contains(phrase, StringComparison.Ordinal))
            .Select(phrase => $"Forbidden phrase detected: \"{phrase}\"")
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Validates a proposal before it is created.
    /// Returns a list of violations (empty if valid).
    /// </summary>
    public static IReadOnlyList<string> ValidateProposal(
        IReadOnlyList<ProposalChange> changes,
        IReadOnlyList<PlanItem>? planItems = null)
    {
        var violations = new List<string>();

        if (changes.Count > MaxChangesPerProposal)
            violations.Add($"Too many changes: {changes.Count} exceeds max of {MaxChangesPerProposal}.");

        if (changes.Count == 0)
            violations.Add("Proposal must have at least one change.");

        foreach (var change in changes)
        {
            if (string.IsNullOrWhiteSpace(change.Reason))
                violations.Add($"Change for {change.TargetId} ({change.Type}) missing reason.");

            if (string.IsNullOrWhiteSpace(change.TargetId))
                violations.Add($"Change of type {change.Type} missing target ID.");

            if (!IsValidItemIdFormat(change.TargetId))
                violations.Add($"Invalid item ID format: {change.TargetId}");

            if (change.Type == "score_delta")
            {
                var delta = change.Delta ?? 0;
                if (Math.Abs(delta) > UserFactMaxDelta)
                    violations.Add(
                        $"Score delta {delta} for {change.TargetId} exceeds absolute max of ±{UserFactMaxDelta}.");
            }

            // Check if target is hard-blocked
            if (planItems is not null && change.Type is "score_delta" or "recommendation")
            {
                var item = planItems.FirstOrDefault(i => i.ItemId == change.TargetId);
                if (item?.HardBlocks.Count > 0)
                    violations.Add(
                        $"Cannot modify hard-blocked item {change.TargetId} (blocks: {string.Join(", ", item.HardBlocks.Select(b => b.Code))}).");
            }
        }

        return violations.AsReadOnly();
    }

    /// <summary>
    /// Validates that an item ID follows the expected prefix format.
    /// </summary>
    public static bool IsValidItemIdFormat(string? itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return false;

        return ValidItemIdRegex().IsMatch(itemId);
    }

    /// <summary>
    /// Clamps a delta to the allowed range.
    /// </summary>
    public static int ClampDelta(int delta, bool hasUserFact = false)
    {
        var max = hasUserFact ? UserFactMaxDelta : DefaultMaxDelta;
        return Math.Clamp(delta, -max, max);
    }

    /// <summary>
    /// Checks whether a tool name is in the allowed set for AI.
    /// </summary>
    public static bool IsAllowedTool(string toolName) =>
        AllowedTools.Contains(toolName);

    /// <summary>
    /// The set of tool names the AI is allowed to call.
    /// </summary>
    public static readonly IReadOnlySet<string> AllowedTools = new HashSet<string>
    {
        "get_session",
        "get_inventory_latest",
        "get_inventory_item",
        "get_plan_current",
        "get_hardblocks",
        "create_proposal",
        "list_proposals",
        "get_proposal",
    };

    [GeneratedRegex(@"^(drv:|svc:|pkg:|app:)[A-Za-z0-9._:\-]+$")]
    private static partial Regex ValidItemIdRegex();
}
