using System.Text.Json.Serialization;

namespace Dmca.Evals.Models;

public sealed class Fixture
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("userMessage")] public string UserMessage { get; set; } = "";
    [JsonPropertyName("userFacts")] public Dictionary<string, string> UserFacts { get; set; } = new();

    [JsonPropertyName("inventory")] public Inventory Inventory { get; set; } = new();
    [JsonPropertyName("plan")] public Plan Plan { get; set; } = new();
    [JsonPropertyName("expect")] public Expect Expect { get; set; } = new();

    public sealed class Inventory
    {
        [JsonPropertyName("items")] public List<InventoryItem> Items { get; set; } = new();
    }

    public sealed class Plan
    {
        [JsonPropertyName("items")] public List<PlanItem> Items { get; set; } = new();
    }

    public sealed class Expect
    {
        [JsonPropertyName("mustUseTools")] public List<string>? MustUseTools { get; set; }
        [JsonPropertyName("mustNotTargetIds")] public List<string>? MustNotTargetIds { get; set; }
        [JsonPropertyName("shouldTargetIds")] public List<string>? ShouldTargetIds { get; set; }

        [JsonPropertyName("mustNotCreateRemovalProposal")] public bool MustNotCreateRemovalProposal { get; set; }
        [JsonPropertyName("mustNotMentionAutoApprove")] public bool MustNotMentionAutoApprove { get; set; }
        [JsonPropertyName("mustExplainPolicy")] public bool MustExplainPolicy { get; set; }
        [JsonPropertyName("mustExplainBlocked")] public bool MustExplainBlocked { get; set; }

        [JsonPropertyName("maxChangesPerProposal")] public int? MaxChangesPerProposal { get; set; }
        [JsonPropertyName("minProposals")] public int? MinProposals { get; set; }
        [JsonPropertyName("mustIncludeEvidence")] public bool MustIncludeEvidence { get; set; }

        [JsonPropertyName("mustPreferFactRequestOrReview")] public bool MustPreferFactRequestOrReview { get; set; }
        [JsonPropertyName("mustNotForceStage1Removal")] public bool MustNotForceStage1Removal { get; set; }
        [JsonPropertyName("shouldReduceRemovalConfidence")] public bool ShouldReduceRemovalConfidence { get; set; }

        [JsonPropertyName("preferMultipleProposalsIfNeeded")] public bool PreferMultipleProposalsIfNeeded { get; set; }

        [JsonPropertyName("forceOfflineBadModel")] public bool ForceOfflineBadModel { get; set; }
        [JsonPropertyName("mustFailIfNoEvidence")] public bool MustFailIfNoEvidence { get; set; }

        [JsonPropertyName("simulateToolFailure")] public bool SimulateToolFailure { get; set; }
        [JsonPropertyName("mustNotCreateProposal")] public bool MustNotCreateProposal { get; set; }
        [JsonPropertyName("mustAskToRetry")] public bool MustAskToRetry { get; set; }

        [JsonPropertyName("allowedActions")] public List<string>? AllowedActions { get; set; }

        [JsonPropertyName("mustNotProposeRemovalForHardBlocked")] public bool MustNotProposeRemovalForHardBlocked { get; set; }
    }
}

public sealed class InventoryItem
{
    [JsonPropertyName("itemId")] public string ItemId { get; set; } = "";
    [JsonPropertyName("itemType")] public string ItemType { get; set; } = "";
    [JsonPropertyName("displayName")] public string DisplayName { get; set; } = "";
    [JsonPropertyName("vendor")] public string? Vendor { get; set; }
    [JsonPropertyName("present")] public bool? Present { get; set; }
    [JsonPropertyName("running")] public bool? Running { get; set; }
    [JsonPropertyName("startType")] public int? StartType { get; set; }
    [JsonPropertyName("signature")] public Signature? Signature { get; set; }
    [JsonPropertyName("tags")] public Dictionary<string, object>? Tags { get; set; }

    public sealed class Signature
    {
        [JsonPropertyName("signed")] public bool? Signed { get; set; }
        [JsonPropertyName("isMicrosoft")] public bool? IsMicrosoft { get; set; }
        [JsonPropertyName("signer")] public string? Signer { get; set; }
    }
}

public sealed class PlanItem
{
    [JsonPropertyName("itemId")] public string ItemId { get; set; } = "";
    [JsonPropertyName("baselineScore")] public int BaselineScore { get; set; }
    [JsonPropertyName("aiScoreDelta")] public int AiScoreDelta { get; set; }
    [JsonPropertyName("finalScore")] public int FinalScore { get; set; }
    [JsonPropertyName("recommendation")] public string Recommendation { get; set; } = "KEEP";
    [JsonPropertyName("hardBlocks")] public List<HardBlock> HardBlocks { get; set; } = new();
    [JsonPropertyName("engineRationale")] public List<string> EngineRationale { get; set; } = new();

    public sealed class HardBlock
    {
        [JsonPropertyName("code")] public string Code { get; set; } = "";
        [JsonPropertyName("message")] public string Message { get; set; } = "";
    }
}
