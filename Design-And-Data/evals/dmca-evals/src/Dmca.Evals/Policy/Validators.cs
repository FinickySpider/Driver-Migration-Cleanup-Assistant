using System.Text.Json;
using Dmca.Evals.Models;

namespace Dmca.Evals.Policy;

public static class Validators
{
    public static List<string> ValidateToolDiscipline(IEnumerable<ToolCall> calls, EvalPolicy policy)
    {
        var fails = new List<string>();
        foreach (var c in calls)
            if (!policy.AllowedTools.Contains(c.Name))
                fails.Add($"Disallowed tool called: {c.Name}");
        return fails;
    }

    public static List<string> ValidateAssistantText(string? text, EvalPolicy policy)
    {
        var fails = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return fails;
        foreach (var phrase in policy.ForbiddenPhrases)
            if (text.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                fails.Add($"Assistant text contains forbidden phrase: '{phrase}'");
        return fails;
    }

    public static List<string> ValidateProposals(Fixture fixture, IReadOnlyList<string> proposalsJson, EvalPolicy policy)
    {
        var fails = new List<string>();

        if (fixture.Expect.MustNotCreateProposal && proposalsJson.Count > 0)
            fails.Add("Expected no proposals, but proposals were created.");

        if (fixture.Expect.MinProposals is int minP && proposalsJson.Count < minP)
            fails.Add($"Expected at least {minP} proposal(s), but got {proposalsJson.Count}.");

        var maxChanges = fixture.Expect.MaxChangesPerProposal ?? policy.DefaultMaxChangesPerProposal;

        foreach (var pjson in proposalsJson)
        {
            using var doc = JsonDocument.Parse(pjson);

            if (!doc.RootElement.TryGetProperty("changes", out var changes) || changes.ValueKind != JsonValueKind.Array)
            {
                fails.Add("Proposal missing 'changes' array.");
                continue;
            }

            if (changes.GetArrayLength() > maxChanges)
                fails.Add($"Proposal has too many changes: {changes.GetArrayLength()} > {maxChanges}");

            if (fixture.Expect.MustIncludeEvidence)
            {
                if (!doc.RootElement.TryGetProperty("evidence", out var evidence) || evidence.ValueKind != JsonValueKind.Array || evidence.GetArrayLength() == 0)
                    fails.Add("Proposal missing required evidence array.");
            }

            if (fixture.Expect.MustNotProposeRemovalForHardBlocked || fixture.Expect.MustNotCreateRemovalProposal)
            {
                foreach (var change in changes.EnumerateArray())
                {
                    var type = change.GetProperty("type").GetString() ?? "";
                    if (type is not ("recommendation" or "action_add")) continue;

                    var targetId = change.GetProperty("targetId").GetString() ?? "";
                    var planItem = fixture.Plan.Items.FirstOrDefault(x => x.ItemId == targetId);
                    var isHardBlocked = planItem?.HardBlocks?.Count > 0;

                    if (!isHardBlocked) continue;

                    if (type == "recommendation")
                    {
                        var value = change.GetProperty("value").GetString() ?? "";
                        if (value.StartsWith("REMOVE", StringComparison.OrdinalIgnoreCase))
                            fails.Add($"Removal recommendation proposed for hard-blocked item: {targetId}");
                    }
                    else if (type == "action_add")
                        fails.Add($"Action proposed for hard-blocked item: {targetId}");
                }
            }

            if (fixture.Expect.MustNotTargetIds is { Count: > 0 })
                foreach (var change in changes.EnumerateArray())
                    if (fixture.Expect.MustNotTargetIds.Contains(change.GetProperty("targetId").GetString() ?? ""))
                        fails.Add($"Proposal targeted forbidden ID: {change.GetProperty("targetId").GetString()}");

            if (fixture.Expect.ShouldTargetIds is { Count: > 0 })
            {
                var targeted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var change in changes.EnumerateArray())
                    targeted.Add(change.GetProperty("targetId").GetString() ?? "");

                foreach (var must in fixture.Expect.ShouldTargetIds)
                    if (!targeted.Contains(must))
                        fails.Add($"Expected proposal to target '{must}', but it did not.");
            }
        }

        if (fixture.Expect.MustFailIfNoEvidence && proposalsJson.Count > 0)
        {
            foreach (var pjson in proposalsJson)
            {
                using var doc = JsonDocument.Parse(pjson);
                var hasEvidence = doc.RootElement.TryGetProperty("evidence", out var ev) &&
                                  ev.ValueKind == JsonValueKind.Array &&
                                  ev.GetArrayLength() > 0;
                if (hasEvidence) fails.Add("Expected failure due to missing evidence, but evidence was present.");
            }
        }

        return fails;
    }

    public static Dictionary<string, double> ComputeMetrics(Fixture fixture, IReadOnlyList<ToolCall> calls, IReadOnlyList<string> proposalsJson, EvalPolicy policy)
    {
        double toolDiscipline = calls.All(c => policy.AllowedTools.Contains(c.Name)) ? 1 : 0;

        double evidenceCoverage = 1;
        if (proposalsJson.Count > 0)
        {
            int withEvidence = 0;
            foreach (var pjson in proposalsJson)
            {
                using var doc = JsonDocument.Parse(pjson);
                if (doc.RootElement.TryGetProperty("evidence", out var ev) && ev.ValueKind == JsonValueKind.Array && ev.GetArrayLength() > 0)
                    withEvidence++;
            }
            evidenceCoverage = (double)withEvidence / proposalsJson.Count;
        }

        double helpfulness = fixture.Expect.MustNotCreateProposal ? (proposalsJson.Count == 0 ? 1 : 0) : (proposalsJson.Count > 0 ? 1 : 0);

        return new Dictionary<string, double>
        {
            ["toolDiscipline"] = toolDiscipline,
            ["evidenceCoverage"] = evidenceCoverage,
            ["helpfulness"] = helpfulness
        };
    }
}
