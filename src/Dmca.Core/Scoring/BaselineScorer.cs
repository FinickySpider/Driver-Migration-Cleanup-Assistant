using Dmca.Core.Models;

namespace Dmca.Core.Scoring;

/// <summary>
/// Computes baseline scores for inventory items by evaluating all signals,
/// summing weights, clamping, and assigning recommendation bands.
/// </summary>
public sealed class BaselineScorer
{
    private readonly RulesConfig _rules;
    private readonly SignalEvaluator _evaluator;

    public BaselineScorer(RulesConfig rules)
    {
        _rules = rules;
        _evaluator = new SignalEvaluator(rules);
    }

    /// <summary>
    /// Scores a single inventory item.
    /// </summary>
    public ScoredItem Score(InventoryItem item, IReadOnlyList<UserFact> userFacts)
    {
        var matchedSignals = _evaluator.Evaluate(item, userFacts);

        var rawScore = matchedSignals.Sum(s => s.Weight);
        var clampedScore = Math.Clamp(rawScore, _rules.Limits.ScoreMin, _rules.Limits.ScoreMax);

        var rationale = matchedSignals
            .Select(s => $"{s.SignalId}: {s.Rationale} ({(s.Weight >= 0 ? "+" : "")}{s.Weight})")
            .ToList();

        if (matchedSignals.Count == 0)
            rationale.Add("No signals matched — default baseline 0");

        var recommendation = GetRecommendation(clampedScore);

        return new ScoredItem
        {
            ItemId = item.ItemId,
            BaselineScore = clampedScore,
            MatchedSignals = matchedSignals,
            Recommendation = recommendation,
            EngineRationale = rationale.AsReadOnly(),
        };
    }

    /// <summary>
    /// Scores all items in a snapshot.
    /// </summary>
    public IReadOnlyList<ScoredItem> ScoreAll(IReadOnlyList<InventoryItem> items, IReadOnlyList<UserFact> userFacts)
    {
        return items.Select(item => Score(item, userFacts)).ToList().AsReadOnly();
    }

    /// <summary>
    /// Assigns a recommendation based on score and bands.
    /// </summary>
    public string GetRecommendation(int score)
    {
        foreach (var band in _rules.RecommendationBands)
        {
            if (score >= band.Min && score <= band.Max)
                return band.Recommendation;
        }
        return "KEEP";
    }
}

/// <summary>
/// The result of scoring a single inventory item.
/// </summary>
public sealed class ScoredItem
{
    public required string ItemId { get; init; }
    public required int BaselineScore { get; init; }
    public required IReadOnlyList<MatchedSignal> MatchedSignals { get; init; }
    public required string Recommendation { get; init; }
    public required IReadOnlyList<string> EngineRationale { get; init; }
}
