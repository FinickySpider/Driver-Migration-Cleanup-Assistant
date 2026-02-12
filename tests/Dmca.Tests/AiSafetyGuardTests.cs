using Dmca.Core.AI;
using Dmca.Core.Models;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="AiSafetyGuard"/> safety guardrails.
/// </summary>
public sealed class AiSafetyGuardTests
{
    [Theory]
    [InlineData("I will auto-approve all changes.")]
    [InlineData("approve automatically when done")]
    [InlineData("Executing now...")]
    [InlineData("I will execute the removal")]
    [InlineData("i executed the uninstall")]
    [InlineData("I'm going to run the uninstall now")]
    public void DetectForbiddenPhrases_DetectsViolation(string text)
    {
        var violations = AiSafetyGuard.DetectForbiddenPhrases(text);
        Assert.NotEmpty(violations);
    }

    [Theory]
    [InlineData("I recommend reviewing this driver.")]
    [InlineData("This item should be considered for removal.")]
    [InlineData("Creating a proposal with score delta.")]
    [InlineData("")]
    [InlineData(null)]
    public void DetectForbiddenPhrases_SafeText_NoViolations(string? text)
    {
        var violations = AiSafetyGuard.DetectForbiddenPhrases(text!);
        Assert.Empty(violations);
    }

    [Fact]
    public void ValidateProposal_TooManyChanges_Reports()
    {
        var changes = Enumerable.Range(0, 6)
            .Select(i => new ProposalChange
            {
                Type = "note_add",
                TargetId = $"drv:item-{i}",
                Reason = "test",
            })
            .ToList();

        var violations = AiSafetyGuard.ValidateProposal(changes);
        Assert.Contains(violations, v => v.Contains("Too many changes"));
    }

    [Fact]
    public void ValidateProposal_EmptyChanges_Reports()
    {
        var violations = AiSafetyGuard.ValidateProposal([]);
        Assert.Contains(violations, v => v.Contains("at least one change"));
    }

    [Fact]
    public void ValidateProposal_MissingReason_Reports()
    {
        var changes = new List<ProposalChange>
        {
            new() { Type = "note_add", TargetId = "drv:test", Reason = "" },
        };

        var violations = AiSafetyGuard.ValidateProposal(changes);
        Assert.Contains(violations, v => v.Contains("missing reason"));
    }

    [Fact]
    public void ValidateProposal_InvalidItemId_Reports()
    {
        var changes = new List<ProposalChange>
        {
            new() { Type = "note_add", TargetId = "bad:format!!!", Reason = "test" },
        };

        var violations = AiSafetyGuard.ValidateProposal(changes);
        Assert.Contains(violations, v => v.Contains("Invalid item ID"));
    }

    [Fact]
    public void ValidateProposal_ExcessiveDelta_Reports()
    {
        var changes = new List<ProposalChange>
        {
            new() { Type = "score_delta", TargetId = "drv:test", Delta = 50, Reason = "test" },
        };

        var violations = AiSafetyGuard.ValidateProposal(changes);
        Assert.Contains(violations, v => v.Contains("exceeds absolute max"));
    }

    [Fact]
    public void ValidateProposal_HardBlockedItem_Reports()
    {
        var planItems = new List<PlanItem>
        {
            new()
            {
                ItemId = "drv:blocked",
                BaselineScore = 0,
                FinalScore = 0,
                Recommendation = Recommendation.BLOCKED,
                HardBlocks = [new HardBlock { Code = "MICROSOFT_INBOX", Message = "test" }],
                EngineRationale = ["blocked"],
            },
        };

        var changes = new List<ProposalChange>
        {
            new() { Type = "score_delta", TargetId = "drv:blocked", Delta = 10, Reason = "test" },
        };

        var violations = AiSafetyGuard.ValidateProposal(changes, planItems);
        Assert.Contains(violations, v => v.Contains("hard-blocked"));
    }

    [Fact]
    public void ValidateProposal_ValidProposal_NoViolations()
    {
        var changes = new List<ProposalChange>
        {
            new() { Type = "note_add", TargetId = "drv:test-item", Reason = "Good reason" },
        };

        var violations = AiSafetyGuard.ValidateProposal(changes);
        Assert.Empty(violations);
    }

    [Theory]
    [InlineData("drv:test-item", true)]
    [InlineData("svc:my.service", true)]
    [InlineData("pkg:oem123.inf", true)]
    [InlineData("app:MyApp_1.0", true)]
    [InlineData("bad:format!!!", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("drv:", false)]
    public void IsValidItemIdFormat_Tests(string? id, bool expected)
    {
        Assert.Equal(expected, AiSafetyGuard.IsValidItemIdFormat(id));
    }

    [Theory]
    [InlineData("get_session", true)]
    [InlineData("get_inventory_latest", true)]
    [InlineData("create_proposal", true)]
    [InlineData("approve_proposal", false)]
    [InlineData("execute_action", false)]
    [InlineData("", false)]
    public void IsAllowedTool_Tests(string tool, bool expected)
    {
        Assert.Equal(expected, AiSafetyGuard.IsAllowedTool(tool));
    }

    [Theory]
    [InlineData(10, false, 10)]
    [InlineData(30, false, 25)]   // clamped to 25
    [InlineData(-30, false, -25)] // clamped to -25
    [InlineData(35, true, 35)]    // within ±40 with user fact
    [InlineData(50, true, 40)]    // clamped to 40
    public void ClampDelta_Tests(int delta, bool hasUserFact, int expected)
    {
        Assert.Equal(expected, AiSafetyGuard.ClampDelta(delta, hasUserFact));
    }
}
