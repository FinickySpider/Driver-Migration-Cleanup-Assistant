using Dmca.Core.Models;
using Dmca.Core.Scoring;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="BaselineScorer"/> score calculation and band assignment.
/// </summary>
public sealed class BaselineScorerTests
{
    private readonly RulesConfig _config;
    private readonly BaselineScorer _scorer;

    public BaselineScorerTests()
    {
        _config = TestRulesFactory.CreateConfig();
        _scorer = new BaselineScorer(_config);
    }

    [Fact]
    public void Score_NonPresentItem_Gets25()
    {
        var item = CreateItem(present: false);
        var result = _scorer.Score(item, []);

        Assert.Equal(25, result.BaselineScore);
        Assert.Equal("KEEP", result.Recommendation);
    }

    [Fact]
    public void Score_NonPresentRunning_GetsZero()
    {
        // +25 non-present, -25 running → 0
        var item = CreateItem(present: false, running: true);
        var result = _scorer.Score(item, []);

        Assert.Equal(0, result.BaselineScore);
        Assert.Equal("KEEP", result.Recommendation);
    }

    [Fact]
    public void Score_NonPresentIntel_WithFact_Gets45()
    {
        // +25 non-present + 20 keyword match = 45
        var item = CreateItem(present: false, vendor: "Intel Corporation");
        var facts = new List<UserFact>
        {
            new()
            {
                SessionId = Guid.NewGuid(),
                Key = "old_platform_vendor",
                Value = "intel",
                Source = FactSource.USER,
                CreatedAt = DateTime.UtcNow,
            },
        };

        var result = _scorer.Score(item, facts);

        Assert.Equal(45, result.BaselineScore);
        Assert.Equal("REVIEW", result.Recommendation);
    }

    [Fact]
    public void Score_NoSignals_GetsZero()
    {
        var item = CreateItem(present: true, vendor: "Realtek");
        var result = _scorer.Score(item, []);

        // present: true → no non_present_device signal
        // running: null → no currently_running_penalty
        // vendor: "Realtek" → no unknown_vendor_penalty, no keyword match
        Assert.Equal(0, result.BaselineScore);
        Assert.Equal("KEEP", result.Recommendation);
    }

    [Fact]
    public void Score_UnknownVendor_GetsZeroClamped()
    {
        // unknown_vendor_penalty = -10 → clamp to 0
        var item = CreateItem(present: null, vendor: null);
        var result = _scorer.Score(item, []);

        Assert.Equal(0, result.BaselineScore); // clamped from -10
    }

    [Fact]
    public void GetRecommendation_BandRanges()
    {
        Assert.Equal("KEEP", _scorer.GetRecommendation(0));
        Assert.Equal("KEEP", _scorer.GetRecommendation(29));
        Assert.Equal("REVIEW", _scorer.GetRecommendation(30));
        Assert.Equal("REVIEW", _scorer.GetRecommendation(54));
        Assert.Equal("REMOVE_STAGE_2", _scorer.GetRecommendation(55));
        Assert.Equal("REMOVE_STAGE_2", _scorer.GetRecommendation(79));
        Assert.Equal("REMOVE_STAGE_1", _scorer.GetRecommendation(80));
        Assert.Equal("REMOVE_STAGE_1", _scorer.GetRecommendation(100));
    }

    [Fact]
    public void ScoreAll_ScoresMultipleItems()
    {
        var items = new InventoryItem[]
        {
            CreateItem(present: false),
            CreateItem(present: true, vendor: "Realtek"),
        };

        var results = _scorer.ScoreAll(items, []);

        Assert.Equal(2, results.Count);
        Assert.Equal(25, results[0].BaselineScore);
        Assert.Equal(0, results[1].BaselineScore);
    }

    private static InventoryItem CreateItem(
        bool? present = null,
        bool? running = null,
        string? vendor = "TestVendor") =>
        new()
        {
            ItemId = $"drv:{Guid.NewGuid():N}",
            ItemType = InventoryItemType.DRIVER,
            DisplayName = "TestDriver",
            Vendor = vendor,
            Present = present,
            Running = running,
            Signature = new SignatureInfo(),
        };
}
