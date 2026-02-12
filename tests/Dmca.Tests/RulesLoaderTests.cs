using Dmca.Core.Scoring;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="RulesLoader"/> YAML parsing and validation.
/// </summary>
public sealed class RulesLoaderTests
{
    private const string ValidYaml = """
        version: 1
        limits:
          score_min: 0
          score_max: 100
          ai_delta_min: -25
          ai_delta_max: 25
          ai_delta_max_with_user_fact: 40
        recommendation_bands:
          - min: 80
            max: 100
            recommendation: REMOVE_STAGE_1
          - min: 55
            max: 79
            recommendation: REMOVE_STAGE_2
          - min: 30
            max: 54
            recommendation: REVIEW
          - min: 0
            max: 29
            recommendation: KEEP
        hard_blocks:
          - code: MICROSOFT_INBOX
            when:
              any:
                - path: item.signature.isMicrosoft
                  op: eq
                  value: true
            message: "Microsoft inbox / core component"
        keyword_sets:
          intel_platform: ["intel","rst","mei"]
        signals:
          - id: non_present_device
            weight: 25
            when:
              all:
                - path: item.present
                  op: eq
                  value: false
            rationale: "Device is not present"
        post_rules:
          clamp_score: true
          compute_final_score: true
        """;

    [Fact]
    public void Load_ValidYaml_ReturnsRulesConfig()
    {
        var config = RulesLoader.Load(ValidYaml);

        Assert.NotNull(config);
        Assert.Equal(1, config.Version);
        Assert.Equal(0, config.Limits.ScoreMin);
        Assert.Equal(100, config.Limits.ScoreMax);
        Assert.Equal(-25, config.Limits.AiDeltaMin);
        Assert.Equal(25, config.Limits.AiDeltaMax);
        Assert.Equal(40, config.Limits.AiDeltaMaxWithUserFact);
    }

    [Fact]
    public void Load_ParsesBands()
    {
        var config = RulesLoader.Load(ValidYaml);

        Assert.Equal(4, config.RecommendationBands.Count);
        Assert.Equal("REMOVE_STAGE_1", config.RecommendationBands[0].Recommendation);
        Assert.Equal(80, config.RecommendationBands[0].Min);
        Assert.Equal(100, config.RecommendationBands[0].Max);
    }

    [Fact]
    public void Load_ParsesHardBlocks()
    {
        var config = RulesLoader.Load(ValidYaml);

        Assert.Single(config.HardBlocks);
        Assert.Equal("MICROSOFT_INBOX", config.HardBlocks[0].Code);
        Assert.NotNull(config.HardBlocks[0].When.Any);
    }

    [Fact]
    public void Load_ParsesKeywordSets()
    {
        var config = RulesLoader.Load(ValidYaml);

        Assert.True(config.KeywordSets.ContainsKey("intel_platform"));
        Assert.Equal(3, config.KeywordSets["intel_platform"].Count);
        Assert.Contains("intel", config.KeywordSets["intel_platform"]);
    }

    [Fact]
    public void Load_ParsesSignals()
    {
        var config = RulesLoader.Load(ValidYaml);

        Assert.Single(config.Signals);
        Assert.Equal("non_present_device", config.Signals[0].Id);
        Assert.Equal(25, config.Signals[0].Weight);
    }

    [Fact]
    public void Load_ParsesPostRules()
    {
        var config = RulesLoader.Load(ValidYaml);

        Assert.True(config.PostRules.ClampScore);
        Assert.True(config.PostRules.ComputeFinalScore);
    }

    [Fact]
    public void Load_InvalidScoreRange_Throws()
    {
        var yaml = ValidYaml.Replace("score_min: 0", "score_min: 200");
        Assert.Throws<InvalidOperationException>(() => RulesLoader.Load(yaml));
    }

    [Fact]
    public void Load_NoSignals_Throws()
    {
        var yaml = """
            version: 1
            limits:
              score_min: 0
              score_max: 100
              ai_delta_min: -25
              ai_delta_max: 25
              ai_delta_max_with_user_fact: 40
            recommendation_bands:
              - min: 0
                max: 100
                recommendation: KEEP
            hard_blocks: []
            keyword_sets: {}
            signals: []
            post_rules:
              clamp_score: true
              compute_final_score: true
            """;

        Assert.Throws<InvalidOperationException>(() => RulesLoader.Load(yaml));
    }

    [Fact]
    public void Load_NoBands_Throws()
    {
        var yaml = """
            version: 1
            limits:
              score_min: 0
              score_max: 100
              ai_delta_min: -25
              ai_delta_max: 25
              ai_delta_max_with_user_fact: 40
            recommendation_bands: []
            hard_blocks: []
            keyword_sets: {}
            signals:
              - id: test
                weight: 10
                when:
                  all:
                    - path: item.present
                      op: eq
                      value: false
                rationale: "test"
            post_rules:
              clamp_score: true
              compute_final_score: true
            """;

        Assert.Throws<InvalidOperationException>(() => RulesLoader.Load(yaml));
    }
}
