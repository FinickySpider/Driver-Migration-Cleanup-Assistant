using Dmca.Core.Models;
using Dmca.Core.Scoring;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="HardBlockEvaluator"/> hard-block rule evaluation.
/// </summary>
public sealed class HardBlockEvaluatorTests
{
    private readonly HardBlockEvaluator _evaluator;

    public HardBlockEvaluatorTests()
    {
        var config = TestRulesFactory.CreateConfig();
        _evaluator = new HardBlockEvaluator(config);
    }

    [Fact]
    public void Evaluate_MicrosoftDriver_BlocksMicrosoftInbox()
    {
        var item = CreateItem(isMicrosoft: true);
        var blocks = _evaluator.Evaluate(item);

        Assert.Contains(blocks, b => b.Code == "MICROSOFT_INBOX");
    }

    [Fact]
    public void Evaluate_MicrosoftWindowsSigner_BlocksMicrosoftInbox()
    {
        var item = CreateItem(signer: "Microsoft Windows Hardware Compatibility Publisher");
        var blocks = _evaluator.Evaluate(item);

        Assert.Contains(blocks, b => b.Code == "MICROSOFT_INBOX");
    }

    [Fact]
    public void Evaluate_PresentDriver_BlocksPresentHardware()
    {
        var item = CreateItem(present: true, itemType: InventoryItemType.DRIVER);
        var blocks = _evaluator.Evaluate(item);

        Assert.Contains(blocks, b => b.Code == "PRESENT_HARDWARE_BINDING");
    }

    [Fact]
    public void Evaluate_PresentApp_NoHardwareBindingBlock()
    {
        // PRESENT_HARDWARE_BINDING only applies to DRIVER and SERVICE
        var item = CreateItem(present: true, itemType: InventoryItemType.APP);
        var blocks = _evaluator.Evaluate(item);

        Assert.DoesNotContain(blocks, b => b.Code == "PRESENT_HARDWARE_BINDING");
    }

    [Fact]
    public void Evaluate_NonPresentThirdParty_NoBlocks()
    {
        var item = CreateItem(present: false, isMicrosoft: false);
        var blocks = _evaluator.Evaluate(item);

        Assert.Empty(blocks);
    }

    [Fact]
    public void Evaluate_MultipleBlocks_ReturnsAll()
    {
        // Microsoft + Present → both MICROSOFT_INBOX and PRESENT_HARDWARE_BINDING
        var item = CreateItem(present: true, isMicrosoft: true, itemType: InventoryItemType.DRIVER);
        var blocks = _evaluator.Evaluate(item);

        Assert.Contains(blocks, b => b.Code == "MICROSOFT_INBOX");
        Assert.Contains(blocks, b => b.Code == "PRESENT_HARDWARE_BINDING");
    }

    private static InventoryItem CreateItem(
        bool? present = null,
        bool isMicrosoft = false,
        string? signer = null,
        InventoryItemType itemType = InventoryItemType.DRIVER) =>
        new()
        {
            ItemId = $"drv:{Guid.NewGuid():N}",
            ItemType = itemType,
            DisplayName = "TestItem",
            Vendor = "TestVendor",
            Present = present,
            Signature = new SignatureInfo
            {
                IsMicrosoft = isMicrosoft,
                Signer = signer,
                Signed = signer is not null || isMicrosoft,
            },
        };
}
