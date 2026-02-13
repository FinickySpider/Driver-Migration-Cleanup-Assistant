using Dmca.Core.Models;

namespace Dmca.Core.Reports;

/// <summary>
/// Generates a delta report comparing two inventory snapshots.
/// Categorizes items as removed, changed, unchanged, or added,
/// and generates next-steps guidance.
/// </summary>
public static class DeltaReportGenerator
{
    /// <summary>
    /// Generates a delta report between pre-execution and post-execution snapshots.
    /// </summary>
    public static DeltaReport Generate(
        Guid sessionId,
        InventorySnapshot preSnapshot,
        InventorySnapshot postSnapshot)
    {
        var preItems = preSnapshot.Items.ToDictionary(i => i.ItemId);
        var postItems = postSnapshot.Items.ToDictionary(i => i.ItemId);

        var allItemIds = preItems.Keys.Union(postItems.Keys).ToHashSet();
        var deltaItems = new List<DeltaReportItem>();

        foreach (var itemId in allItemIds)
        {
            var inPre = preItems.TryGetValue(itemId, out var pre);
            var inPost = postItems.TryGetValue(itemId, out var post);

            if (inPre && !inPost)
            {
                // Removed
                deltaItems.Add(new DeltaReportItem
                {
                    ItemId = itemId,
                    ItemType = pre!.ItemType,
                    DisplayName = pre.DisplayName,
                    Status = DeltaStatus.REMOVED,
                });
            }
            else if (!inPre && inPost)
            {
                // Added (new item appeared after execution)
                deltaItems.Add(new DeltaReportItem
                {
                    ItemId = itemId,
                    ItemType = post!.ItemType,
                    DisplayName = post.DisplayName,
                    Status = DeltaStatus.ADDED,
                });
            }
            else if (inPre && inPost)
            {
                var changes = CompareItems(pre!, post!);
                deltaItems.Add(new DeltaReportItem
                {
                    ItemId = itemId,
                    ItemType = pre!.ItemType,
                    DisplayName = pre.DisplayName,
                    Status = changes.Count > 0 ? DeltaStatus.CHANGED : DeltaStatus.UNCHANGED,
                    Changes = changes,
                });
            }
        }

        var summary = new DeltaReportSummary
        {
            Removed = deltaItems.Count(d => d.Status == DeltaStatus.REMOVED),
            Changed = deltaItems.Count(d => d.Status == DeltaStatus.CHANGED),
            Unchanged = deltaItems.Count(d => d.Status == DeltaStatus.UNCHANGED),
            Added = deltaItems.Count(d => d.Status == DeltaStatus.ADDED),
        };

        var nextSteps = GenerateNextSteps(deltaItems, summary);

        return new DeltaReport
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            PreSnapshotId = preSnapshot.Id,
            PostSnapshotId = postSnapshot.Id,
            CreatedAt = DateTime.UtcNow,
            Summary = summary,
            Items = deltaItems.AsReadOnly(),
            NextSteps = nextSteps.AsReadOnly(),
        };
    }

    /// <summary>
    /// Compares two inventory items and returns a list of property-level changes.
    /// </summary>
    internal static IReadOnlyList<PropertyChange> CompareItems(InventoryItem pre, InventoryItem post)
    {
        var changes = new List<PropertyChange>();

        CompareProperty(changes, "Version", pre.Version, post.Version);
        CompareProperty(changes, "Vendor", pre.Vendor, post.Vendor);
        CompareProperty(changes, "Provider", pre.Provider, post.Provider);
        CompareProperty(changes, "DriverInf", pre.DriverInf, post.DriverInf);
        CompareProperty(changes, "DriverStorePublishedName",
            pre.DriverStorePublishedName, post.DriverStorePublishedName);
        CompareProperty(changes, "Present",
            pre.Present?.ToString(), post.Present?.ToString());
        CompareProperty(changes, "Running",
            pre.Running?.ToString(), post.Running?.ToString());
        CompareProperty(changes, "StartType",
            pre.StartType?.ToString(), post.StartType?.ToString());

        return changes.AsReadOnly();
    }

    private static void CompareProperty(List<PropertyChange> changes, string name, string? oldVal, string? newVal)
    {
        if (!string.Equals(oldVal, newVal, StringComparison.Ordinal))
        {
            changes.Add(new PropertyChange
            {
                Property = name,
                OldValue = oldVal,
                NewValue = newVal,
            });
        }
    }

    /// <summary>
    /// Generates next-steps guidance based on the delta results.
    /// </summary>
    internal static List<string> GenerateNextSteps(
        IReadOnlyList<DeltaReportItem> items,
        DeltaReportSummary summary)
    {
        var steps = new List<string>();

        if (summary.Removed > 0)
        {
            steps.Add($"✅ {summary.Removed} item(s) successfully removed.");
            steps.Add("Recommend rebooting to complete driver removal and release locked files.");
        }

        if (summary.Changed > 0)
        {
            steps.Add($"⚠️ {summary.Changed} item(s) changed (e.g., services disabled). Verify expected state.");

            var serviceChanges = items
                .Where(i => i.Status == DeltaStatus.CHANGED && i.ItemType == InventoryItemType.SERVICE)
                .ToList();

            if (serviceChanges.Count > 0)
            {
                var startTypeChanges = serviceChanges
                    .Where(i => i.Changes.Any(c => c.Property == "StartType"))
                    .ToList();

                if (startTypeChanges.Count > 0)
                    steps.Add($"  → {startTypeChanges.Count} service(s) had start type changed.");
            }
        }

        if (summary.Unchanged > 0)
            steps.Add($"ℹ️ {summary.Unchanged} item(s) remain unchanged.");

        if (summary.Added > 0)
            steps.Add($"🆕 {summary.Added} new item(s) detected since the initial scan.");

        if (summary.Removed == 0 && summary.Changed == 0)
            steps.Add("No items were removed or changed. Review the execution log for errors.");

        steps.Add("Run a final system check to confirm stability before normal use.");

        return steps;
    }

    /// <summary>
    /// Exports a delta report as markdown text.
    /// </summary>
    public static string ExportAsMarkdown(DeltaReport report)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("# Delta Report");
        sb.AppendLine();
        sb.AppendLine($"**Session:** {report.SessionId}");
        sb.AppendLine($"**Generated:** {report.CreatedAt:yyyy-MM-dd HH:mm:ss UTC}");
        sb.AppendLine($"**Pre-snapshot:** {report.PreSnapshotId}");
        sb.AppendLine($"**Post-snapshot:** {report.PostSnapshotId}");
        sb.AppendLine();

        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"| Status | Count |");
        sb.AppendLine($"|--------|-------|");
        sb.AppendLine($"| Removed | {report.Summary.Removed} |");
        sb.AppendLine($"| Changed | {report.Summary.Changed} |");
        sb.AppendLine($"| Unchanged | {report.Summary.Unchanged} |");
        sb.AppendLine($"| Added | {report.Summary.Added} |");
        sb.AppendLine();

        if (report.Items.Any(i => i.Status == DeltaStatus.REMOVED))
        {
            sb.AppendLine("## Removed Items");
            sb.AppendLine();
            foreach (var item in report.Items.Where(i => i.Status == DeltaStatus.REMOVED))
                sb.AppendLine($"- **{item.DisplayName}** (`{item.ItemId}`) — {item.ItemType}");
            sb.AppendLine();
        }

        if (report.Items.Any(i => i.Status == DeltaStatus.CHANGED))
        {
            sb.AppendLine("## Changed Items");
            sb.AppendLine();
            foreach (var item in report.Items.Where(i => i.Status == DeltaStatus.CHANGED))
            {
                sb.AppendLine($"### {item.DisplayName} (`{item.ItemId}`)");
                foreach (var change in item.Changes)
                    sb.AppendLine($"- {change.Property}: `{change.OldValue ?? "(null)"}` → `{change.NewValue ?? "(null)"}`");
                sb.AppendLine();
            }
        }

        if (report.Items.Any(i => i.Status == DeltaStatus.ADDED))
        {
            sb.AppendLine("## New Items");
            sb.AppendLine();
            foreach (var item in report.Items.Where(i => i.Status == DeltaStatus.ADDED))
                sb.AppendLine($"- **{item.DisplayName}** (`{item.ItemId}`) — {item.ItemType}");
            sb.AppendLine();
        }

        sb.AppendLine("## Next Steps");
        sb.AppendLine();
        foreach (var step in report.NextSteps)
            sb.AppendLine($"- {step}");

        return sb.ToString();
    }
}
