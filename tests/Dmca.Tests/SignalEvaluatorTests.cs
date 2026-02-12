using Dmca.Core.Models;
using Dmca.Core.Scoring;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="SignalEvaluator"/> condition evaluation.
/// </summary>
public sealed class SignalEvaluatorTests
{
    private readonly RulesConfig _config;
    private readonly SignalEvaluator _evaluator;

    public SignalEvaluatorTests()
    {
        _config = TestRulesFactory.CreateConfig();
        _evaluator = new SignalEvaluator(_config);
    }

    [Fact]
    public void Evaluate_NonPresentDevice_MatchesSignal()
    {
        var item = CreateItem(present: false);
        var result = _evaluator.Evaluate(item, []);

        Assert.Contains(result, s => s.SignalId == "non_present_device");
    }

    [Fact]
    public void Evaluate_PresentDevice_NoNonPresentSignal()
    {
        var item = CreateItem(present: true);
        var result = _evaluator.Evaluate(item, []);

        Assert.DoesNotContain(result, s => s.SignalId == "non_present_device");
    }

    [Fact]
    public void Evaluate_RunningItem_GetsNegativeWeight()
    {
        var item = CreateItem(running: true);
        var result = _evaluator.Evaluate(item, []);

        var signal = result.FirstOrDefault(s => s.SignalId == "currently_running_penalty");
        Assert.NotNull(signal);
        Assert.Equal(-25, signal.Weight);
    }

    [Fact]
    public void Evaluate_KeywordMatch_WithUserFact_Matches()
    {
        var item = CreateItem(vendor: "Intel Corporation", displayName: "Intel MEI");
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

        var result = _evaluator.Evaluate(item, facts);
        Assert.Contains(result, s => s.SignalId == "vendor_matches_old_platform_keywords");
    }

    [Fact]
    public void Evaluate_KeywordMatch_WithoutUserFact_DoesNotMatch()
    {
        var item = CreateItem(vendor: "Intel Corporation", displayName: "Intel MEI");
        var result = _evaluator.Evaluate(item, []);

        Assert.DoesNotContain(result, s => s.SignalId == "vendor_matches_old_platform_keywords");
    }

    [Fact]
    public void Evaluate_UnknownVendor_MatchesPenalty()
    {
        var item = CreateItem(vendor: null);
        var result = _evaluator.Evaluate(item, []);

        Assert.Contains(result, s => s.SignalId == "unknown_vendor_penalty");
    }

    [Fact]
    public void Evaluate_KnownVendor_NoUnknownPenalty()
    {
        var item = CreateItem(vendor: "Realtek");
        var result = _evaluator.Evaluate(item, []);

        Assert.DoesNotContain(result, s => s.SignalId == "unknown_vendor_penalty");
    }

    [Fact]
    public void Evaluate_DisabledService_MatchesSignal()
    {
        var item = CreateItem(itemType: InventoryItemType.SERVICE, startType: 4);
        var result = _evaluator.Evaluate(item, []);

        Assert.Contains(result, s => s.SignalId == "service_disabled");
    }

    [Fact]
    public void Evaluate_ServiceSignal_DoesNotApplyToDriver()
    {
        var item = CreateItem(itemType: InventoryItemType.DRIVER, startType: 4);
        var result = _evaluator.Evaluate(item, []);

        Assert.DoesNotContain(result, s => s.SignalId == "service_disabled");
    }

    [Fact]
    public void ResolveFieldValue_MapsBasicPaths()
    {
        var item = CreateItem(vendor: "TestVendor", present: true);

        Assert.Equal("TestVendor", SignalEvaluator.ResolveFieldValue("item.vendor", item));
        Assert.Equal(true, SignalEvaluator.ResolveFieldValue("item.present", item));
    }

    [Fact]
    public void ResolveFieldValue_MapsSignaturePaths()
    {
        var item = CreateItem(isMicrosoft: true, signer: "Microsoft Windows");

        Assert.Equal(true, SignalEvaluator.ResolveFieldValue("item.signature.isMicrosoft", item));
        Assert.Equal("Microsoft Windows", SignalEvaluator.ResolveFieldValue("item.signature.signer", item));
    }

    [Fact]
    public void ResolveFieldValue_NullPath_ReturnsNull()
    {
        var item = CreateItem();
        Assert.Null(SignalEvaluator.ResolveFieldValue(null, item));
    }

    [Fact]
    public void ResolveFieldValue_UnknownPath_ReturnsNull()
    {
        var item = CreateItem();
        Assert.Null(SignalEvaluator.ResolveFieldValue("item.nonexistent", item));
    }

    private static InventoryItem CreateItem(
        bool? present = null,
        bool? running = null,
        string? vendor = "TestVendor",
        string? displayName = "TestItem",
        InventoryItemType itemType = InventoryItemType.DRIVER,
        int? startType = null,
        bool isMicrosoft = false,
        string? signer = null) =>
        new()
        {
            ItemId = $"drv:{Guid.NewGuid():N}",
            ItemType = itemType,
            DisplayName = displayName ?? "TestItem",
            Vendor = vendor,
            Present = present,
            Running = running,
            StartType = startType,
            Signature = new SignatureInfo
            {
                IsMicrosoft = isMicrosoft,
                Signer = signer,
                Signed = signer is not null,
            },
        };
}
