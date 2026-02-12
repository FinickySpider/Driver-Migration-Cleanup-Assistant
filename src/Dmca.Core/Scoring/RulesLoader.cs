using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dmca.Core.Scoring;

/// <summary>
/// Loads and validates rules.yml into a strongly-typed <see cref="RulesConfig"/>.
/// </summary>
public static class RulesLoader
{
    /// <summary>
    /// Parses a rules YAML string into a validated <see cref="RulesConfig"/>.
    /// </summary>
    public static RulesConfig Load(string yaml)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yaml);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var raw = deserializer.Deserialize<RawRulesYaml>(yaml)
            ?? throw new InvalidOperationException("Failed to deserialize rules.yml: result was null.");

        var config = MapToConfig(raw);
        Validate(config);
        return config;
    }

    /// <summary>
    /// Loads rules from a file path.
    /// </summary>
    public static RulesConfig LoadFromFile(string path)
    {
        var yaml = File.ReadAllText(path);
        return Load(yaml);
    }

    private static RulesConfig MapToConfig(RawRulesYaml raw)
    {
        var limits = new Limits
        {
            ScoreMin = raw.Limits?.ScoreMin ?? 0,
            ScoreMax = raw.Limits?.ScoreMax ?? 100,
            AiDeltaMin = raw.Limits?.AiDeltaMin ?? -25,
            AiDeltaMax = raw.Limits?.AiDeltaMax ?? 25,
            AiDeltaMaxWithUserFact = raw.Limits?.AiDeltaMaxWithUserFact ?? 40,
        };

        var bands = (raw.RecommendationBands ?? [])
            .Select(b => new RecommendationBand
            {
                Min = b.Min,
                Max = b.Max,
                Recommendation = b.Recommendation ?? "KEEP",
            })
            .ToList()
            .AsReadOnly();

        var hardBlocks = (raw.HardBlocks ?? [])
            .Select(hb => new HardBlockDefinition
            {
                Code = hb.Code ?? throw new InvalidOperationException("Hard block missing 'code'."),
                When = MapConditionGroup(hb.When) ?? throw new InvalidOperationException($"Hard block '{hb.Code}' missing 'when'."),
                Message = hb.Message ?? "",
                AppliesToTypes = hb.AppliesToTypes?.AsReadOnly(),
            })
            .ToList()
            .AsReadOnly();

        var keywordSets = (raw.KeywordSets ?? new Dictionary<string, List<string>>())
            .ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<string>)kvp.Value.AsReadOnly())
            as IReadOnlyDictionary<string, IReadOnlyList<string>>;

        var signals = (raw.Signals ?? [])
            .Select(s => new SignalDefinition
            {
                Id = s.Id ?? throw new InvalidOperationException("Signal missing 'id'."),
                Weight = s.Weight,
                When = MapConditionGroup(s.When) ?? throw new InvalidOperationException($"Signal '{s.Id}' missing 'when'."),
                Rationale = s.Rationale ?? "",
                AppliesToTypes = s.AppliesToTypes?.AsReadOnly(),
                RequiresUserFact = MapConditionGroup(s.RequiresUserFact),
            })
            .ToList()
            .AsReadOnly();

        var postRules = new PostRules
        {
            ClampScore = raw.PostRules?.ClampScore ?? true,
            ComputeFinalScore = raw.PostRules?.ComputeFinalScore ?? true,
        };

        return new RulesConfig
        {
            Version = raw.Version,
            Limits = limits,
            RecommendationBands = bands,
            HardBlocks = hardBlocks,
            KeywordSets = keywordSets,
            Signals = signals,
            PostRules = postRules,
        };
    }

    private static ConditionGroup? MapConditionGroup(RawConditionGroup? raw)
    {
        if (raw is null) return null;

        return new ConditionGroup
        {
            All = raw.All?.Select(MapCondition).ToList().AsReadOnly(),
            Any = raw.Any?.Select(MapCondition).ToList().AsReadOnly(),
        };
    }

    private static Condition MapCondition(RawCondition raw) => new()
    {
        Path = raw.Path,
        Op = raw.Op,
        Value = raw.Value,
        Keywords = raw.Keywords,
        Key = raw.Key,
        EqualsI = raw.EqualsI,
    };

    private static void Validate(RulesConfig config)
    {
        if (config.Limits.ScoreMin > config.Limits.ScoreMax)
            throw new InvalidOperationException("score_min must be <= score_max");

        if (config.RecommendationBands.Count == 0)
            throw new InvalidOperationException("At least one recommendation band is required.");

        if (config.Signals.Count == 0)
            throw new InvalidOperationException("At least one signal is required.");
    }

    // ── Raw YAML deserialization models ──

    internal sealed class RawRulesYaml
    {
        public int Version { get; set; }
        public RawLimits? Limits { get; set; }
        public List<RawBand>? RecommendationBands { get; set; }
        public List<RawHardBlock>? HardBlocks { get; set; }
        public Dictionary<string, List<string>>? KeywordSets { get; set; }
        public List<RawSignal>? Signals { get; set; }
        public RawPostRules? PostRules { get; set; }
    }

    internal sealed class RawLimits
    {
        public int ScoreMin { get; set; }
        public int ScoreMax { get; set; }
        public int AiDeltaMin { get; set; }
        public int AiDeltaMax { get; set; }
        public int AiDeltaMaxWithUserFact { get; set; }
    }

    internal sealed class RawBand
    {
        public int Min { get; set; }
        public int Max { get; set; }
        public string? Recommendation { get; set; }
    }

    internal sealed class RawHardBlock
    {
        public string? Code { get; set; }
        public RawConditionGroup? When { get; set; }
        public string? Message { get; set; }
        public List<string>? AppliesToTypes { get; set; }
    }

    internal sealed class RawSignal
    {
        public string? Id { get; set; }
        public int Weight { get; set; }
        public RawConditionGroup? When { get; set; }
        public string? Rationale { get; set; }
        public List<string>? AppliesToTypes { get; set; }
        public RawConditionGroup? RequiresUserFact { get; set; }
    }

    internal sealed class RawConditionGroup
    {
        public List<RawCondition>? All { get; set; }
        public List<RawCondition>? Any { get; set; }
    }

    internal sealed class RawCondition
    {
        public string? Path { get; set; }
        public string? Op { get; set; }
        public object? Value { get; set; }
        public string? Keywords { get; set; }
        public string? Key { get; set; }
        public string? EqualsI { get; set; }
    }

    internal sealed class RawPostRules
    {
        public bool ClampScore { get; set; }
        public bool ComputeFinalScore { get; set; }
    }
}
