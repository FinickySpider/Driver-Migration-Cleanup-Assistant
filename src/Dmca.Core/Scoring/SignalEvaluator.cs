using Dmca.Core.Models;

namespace Dmca.Core.Scoring;

/// <summary>
/// Evaluates signal conditions against an inventory item and user facts.
/// Returns the list of matched signals with their weights and rationale.
/// </summary>
public sealed class SignalEvaluator
{
    private readonly RulesConfig _rules;

    public SignalEvaluator(RulesConfig rules) => _rules = rules;

    /// <summary>
    /// Evaluates all signals against a single inventory item.
    /// Returns matched signals.
    /// </summary>
    public IReadOnlyList<MatchedSignal> Evaluate(InventoryItem item, IReadOnlyList<UserFact> userFacts)
    {
        var matched = new List<MatchedSignal>();

        foreach (var signal in _rules.Signals)
        {
            // Check type filter
            if (signal.AppliesToTypes is not null &&
                !signal.AppliesToTypes.Contains(item.ItemType.ToString(), StringComparer.OrdinalIgnoreCase))
                continue;

            // Check user-fact requirements
            if (signal.RequiresUserFact is not null &&
                !EvaluateUserFactConditions(signal.RequiresUserFact, userFacts))
                continue;

            // Check item conditions
            if (EvaluateConditionGroup(signal.When, item))
            {
                matched.Add(new MatchedSignal
                {
                    SignalId = signal.Id,
                    Weight = signal.Weight,
                    Rationale = signal.Rationale,
                });
            }
        }

        return matched.AsReadOnly();
    }

    internal bool EvaluateConditionGroup(ConditionGroup group, InventoryItem item)
    {
        if (group.All is not null)
            return group.All.All(c => EvaluateCondition(c, item));

        if (group.Any is not null)
            return group.Any.Any(c => EvaluateCondition(c, item));

        return false;
    }

    internal bool EvaluateCondition(Condition condition, InventoryItem item)
    {
        var fieldValue = ResolveFieldValue(condition.Path, item);

        return condition.Op?.ToLowerInvariant() switch
        {
            "eq" => EvalEquals(fieldValue, condition.Value),
            "contains_i" => EvalContainsI(fieldValue, condition.Value),
            "matches_keywords" => EvalMatchesKeywords(fieldValue, condition.Keywords),
            "missing_or_empty" => EvalMissingOrEmpty(fieldValue),
            _ => false,
        };
    }

    private static bool EvalEquals(object? fieldValue, object? expected)
    {
        if (fieldValue is null && expected is null) return true;
        if (fieldValue is null || expected is null) return false;

        // Handle bool comparison
        if (expected is bool expectedBool)
            return fieldValue is bool fv && fv == expectedBool;

        // Handle int comparison
        if (expected is int expectedInt)
            return fieldValue is int fvInt && fvInt == expectedInt;

        // Handle long from YAML (YamlDotNet may parse ints as long)
        if (expected is long expectedLong)
        {
            if (fieldValue is int fvI) return fvI == expectedLong;
            if (fieldValue is long fvL) return fvL == expectedLong;
        }

        // String comparison
        return string.Equals(fieldValue.ToString(), expected.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool EvalContainsI(object? fieldValue, object? substring)
    {
        if (fieldValue is not string str || substring is not string sub) return false;
        return str.Contains(sub, StringComparison.OrdinalIgnoreCase);
    }

    private bool EvalMatchesKeywords(object? fieldValue, string? keywordSetName)
    {
        if (fieldValue is not string str || string.IsNullOrWhiteSpace(keywordSetName)) return false;
        if (!_rules.KeywordSets.TryGetValue(keywordSetName, out var keywords)) return false;

        return keywords.Any(kw => str.Contains(kw, StringComparison.OrdinalIgnoreCase));
    }

    private static bool EvalMissingOrEmpty(object? fieldValue)
    {
        return fieldValue is null || (fieldValue is string s && string.IsNullOrWhiteSpace(s));
    }

    private static bool EvaluateUserFactConditions(ConditionGroup group, IReadOnlyList<UserFact> facts)
    {
        if (group.Any is not null)
        {
            return group.Any.Any(c =>
            {
                if (c.Key is null || c.EqualsI is null) return false;
                return facts.Any(f =>
                    string.Equals(f.Key, c.Key, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(f.Value, c.EqualsI, StringComparison.OrdinalIgnoreCase));
            });
        }

        if (group.All is not null)
        {
            return group.All.All(c =>
            {
                if (c.Key is null || c.EqualsI is null) return false;
                return facts.Any(f =>
                    string.Equals(f.Key, c.Key, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(f.Value, c.EqualsI, StringComparison.OrdinalIgnoreCase));
            });
        }

        return false;
    }

    /// <summary>
    /// Resolves a dot-path into the item's field value.
    /// Supports paths like: "item.present", "item.vendor", "item.signature.isMicrosoft", etc.
    /// </summary>
    internal static object? ResolveFieldValue(string? path, InventoryItem item)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;

        // Strip leading "item." prefix
        var field = path.StartsWith("item.", StringComparison.OrdinalIgnoreCase)
            ? path["item.".Length..]
            : path;

        return field.ToLowerInvariant() switch
        {
            "present" => item.Present,
            "running" => item.Running,
            "vendor" => item.Vendor,
            "displayname" => item.DisplayName,
            "provider" => item.Provider,
            "version" => item.Version,
            "itemtype" => item.ItemType.ToString(),
            "starttype" => item.StartType,
            "driverinf" => item.DriverInf,
            "signature.ismicrosoft" => item.Signature?.IsMicrosoft,
            "signature.signed" => item.Signature?.Signed,
            "signature.signer" => item.Signature?.Signer,
            "signature.iswhql" => item.Signature?.IsWHQL,
            "tags.bootcriticalinuse" => false, // tag system not yet implemented; default false
            _ => null,
        };
    }
}

/// <summary>
/// A signal that matched during evaluation.
/// </summary>
public sealed class MatchedSignal
{
    public required string SignalId { get; init; }
    public required int Weight { get; init; }
    public required string Rationale { get; init; }
}
