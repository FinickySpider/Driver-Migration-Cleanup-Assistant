using Dmca.Core.Models;

namespace Dmca.Core.Scoring;

/// <summary>
/// Evaluates hard-block rules against inventory items.
/// Hard blocks are non-overridable in v1 and force recommendation to BLOCKED.
/// </summary>
public sealed class HardBlockEvaluator
{
    private readonly RulesConfig _rules;

    public HardBlockEvaluator(RulesConfig rules) => _rules = rules;

    /// <summary>
    /// Evaluates all hard-block rules against a single inventory item.
    /// Returns the list of triggered hard blocks (may be empty).
    /// </summary>
    public IReadOnlyList<HardBlock> Evaluate(InventoryItem item)
    {
        var blocks = new List<HardBlock>();

        foreach (var def in _rules.HardBlocks)
        {
            // Check type filter
            if (def.AppliesToTypes is not null &&
                !def.AppliesToTypes.Contains(item.ItemType.ToString(), StringComparer.OrdinalIgnoreCase))
                continue;

            if (EvaluateConditionGroup(def.When, item))
            {
                blocks.Add(new HardBlock
                {
                    Code = def.Code,
                    Message = def.Message,
                });
            }
        }

        return blocks.AsReadOnly();
    }

    private static bool EvaluateConditionGroup(ConditionGroup group, InventoryItem item)
    {
        if (group.All is not null)
            return group.All.All(c => EvaluateCondition(c, item));

        if (group.Any is not null)
            return group.Any.Any(c => EvaluateCondition(c, item));

        return false;
    }

    private static bool EvaluateCondition(Condition condition, InventoryItem item)
    {
        var fieldValue = SignalEvaluator.ResolveFieldValue(condition.Path, item);

        return condition.Op?.ToLowerInvariant() switch
        {
            "eq" => EvalEquals(fieldValue, condition.Value),
            "contains_i" => EvalContainsI(fieldValue, condition.Value),
            _ => false,
        };
    }

    private static bool EvalEquals(object? fieldValue, object? expected)
    {
        if (fieldValue is null && expected is null) return true;
        if (fieldValue is null || expected is null) return false;

        if (expected is bool expectedBool)
            return fieldValue is bool fv && fv == expectedBool;

        if (expected is int expectedInt)
            return fieldValue is int fvInt && fvInt == expectedInt;

        if (expected is long expectedLong)
        {
            if (fieldValue is int fvI) return fvI == expectedLong;
            if (fieldValue is long fvL) return fvL == expectedLong;
        }

        return string.Equals(fieldValue.ToString(), expected.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool EvalContainsI(object? fieldValue, object? substring)
    {
        if (fieldValue is not string str || substring is not string sub) return false;
        return str.Contains(sub, StringComparison.OrdinalIgnoreCase);
    }
}
