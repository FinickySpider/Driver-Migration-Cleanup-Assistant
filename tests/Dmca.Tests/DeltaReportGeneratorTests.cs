using Dmca.Core.Models;
using Dmca.Core.Reports;

namespace Dmca.Tests;

/// <summary>
/// Tests for <see cref="DeltaReportGenerator"/>.
/// Verifies correct categorization of items as removed, changed, unchanged, and added.
/// </summary>
public sealed class DeltaReportGeneratorTests
{
    private static readonly Guid SessionId = Guid.NewGuid();

    // ── Helper factories ──

    private static InventorySnapshot MakeSnapshot(Guid id, params InventoryItem[] items)
    {
        return new InventorySnapshot
        {
            Id = id,
            SessionId = SessionId,
            CreatedAt = DateTime.UtcNow,
            Summary = new SnapshotSummary
            {
                Drivers = items.Count(i => i.ItemType == InventoryItemType.DRIVER),
                Services = items.Count(i => i.ItemType == InventoryItemType.SERVICE),
                Packages = items.Count(i => i.ItemType == InventoryItemType.DRIVER_PACKAGE),
                Apps = items.Count(i => i.ItemType == InventoryItemType.APP),
            },
            Items = items.ToList().AsReadOnly(),
        };
    }

    private static InventoryItem MakeDriver(
        string id,
        string displayName,
        string? vendor = null,
        string? version = null,
        bool? present = null,
        string? driverInf = null) => new()
    {
        ItemId = id,
        ItemType = InventoryItemType.DRIVER,
        DisplayName = displayName,
        Vendor = vendor,
        Version = version,
        Present = present,
        DriverInf = driverInf,
    };

    private static InventoryItem MakeService(
        string id,
        string displayName,
        bool? running = null,
        int? startType = null) => new()
    {
        ItemId = id,
        ItemType = InventoryItemType.SERVICE,
        DisplayName = displayName,
        Running = running,
        StartType = startType,
    };

    // ── Removed Items ──

    [Fact]
    public void Generate_ItemInPreOnly_MarkedAsRemoved()
    {
        var preId = Guid.NewGuid();
        var postId = Guid.NewGuid();

        var pre = MakeSnapshot(preId,
            MakeDriver("drv:intel_mei.inf", "Intel MEI Driver", vendor: "Intel"));
        var post = MakeSnapshot(postId); // empty

        var report = DeltaReportGenerator.Generate(SessionId, pre, post);

        Assert.Single(report.Items);
        Assert.Equal(DeltaStatus.REMOVED, report.Items[0].Status);
        Assert.Equal("drv:intel_mei.inf", report.Items[0].ItemId);
        Assert.Equal(1, report.Summary.Removed);
        Assert.Equal(0, report.Summary.Added);
        Assert.Equal(0, report.Summary.Changed);
        Assert.Equal(0, report.Summary.Unchanged);
    }

    [Fact]
    public void Generate_MultipleRemoved_AllTracked()
    {
        var pre = MakeSnapshot(Guid.NewGuid(),
            MakeDriver("drv:a.inf", "Driver A"),
            MakeDriver("drv:b.inf", "Driver B"),
            MakeService("svc:OldSvc", "Old Service"));
        var post = MakeSnapshot(Guid.NewGuid());

        var report = DeltaReportGenerator.Generate(SessionId, pre, post);

        Assert.Equal(3, report.Summary.Removed);
        Assert.All(report.Items, i => Assert.Equal(DeltaStatus.REMOVED, i.Status));
    }

    // ── Added Items ──

    [Fact]
    public void Generate_ItemInPostOnly_MarkedAsAdded()
    {
        var pre = MakeSnapshot(Guid.NewGuid());
        var post = MakeSnapshot(Guid.NewGuid(),
            MakeDriver("drv:new_realtek.inf", "New Realtek Driver"));

        var report = DeltaReportGenerator.Generate(SessionId, pre, post);

        Assert.Single(report.Items);
        Assert.Equal(DeltaStatus.ADDED, report.Items[0].Status);
        Assert.Equal(1, report.Summary.Added);
    }

    // ── Unchanged Items ──

    [Fact]
    public void Generate_IdenticalItem_MarkedAsUnchanged()
    {
        var item = MakeDriver("drv:same.inf", "Same Driver", vendor: "TestVendor", version: "1.0");

        var pre = MakeSnapshot(Guid.NewGuid(), item);
        var post = MakeSnapshot(Guid.NewGuid(), item);

        var report = DeltaReportGenerator.Generate(SessionId, pre, post);

        Assert.Single(report.Items);
        Assert.Equal(DeltaStatus.UNCHANGED, report.Items[0].Status);
        Assert.Equal(1, report.Summary.Unchanged);
        Assert.Empty(report.Items[0].Changes);
    }

    // ── Changed Items ──

    [Fact]
    public void Generate_VersionChanged_MarkedAsChangedWithPropertyDiff()
    {
        var preItem = MakeDriver("drv:test.inf", "Test Driver", version: "1.0");
        var postItem = MakeDriver("drv:test.inf", "Test Driver", version: "2.0");

        var pre = MakeSnapshot(Guid.NewGuid(), preItem);
        var post = MakeSnapshot(Guid.NewGuid(), postItem);

        var report = DeltaReportGenerator.Generate(SessionId, pre, post);

        Assert.Single(report.Items);
        var item = report.Items[0];
        Assert.Equal(DeltaStatus.CHANGED, item.Status);
        Assert.Single(item.Changes);
        Assert.Equal("Version", item.Changes[0].Property);
        Assert.Equal("1.0", item.Changes[0].OldValue);
        Assert.Equal("2.0", item.Changes[0].NewValue);
    }

    [Fact]
    public void Generate_ServiceStartTypeChanged_TracksPropertyChange()
    {
        var preItem = MakeService("svc:Test", "Test Service", running: true, startType: 2);
        var postItem = MakeService("svc:Test", "Test Service", running: false, startType: 4);

        var pre = MakeSnapshot(Guid.NewGuid(), preItem);
        var post = MakeSnapshot(Guid.NewGuid(), postItem);

        var report = DeltaReportGenerator.Generate(SessionId, pre, post);

        var item = report.Items[0];
        Assert.Equal(DeltaStatus.CHANGED, item.Status);
        Assert.Equal(2, item.Changes.Count);

        var runningChange = item.Changes.First(c => c.Property == "Running");
        Assert.Equal("True", runningChange.OldValue);
        Assert.Equal("False", runningChange.NewValue);

        var startTypeChange = item.Changes.First(c => c.Property == "StartType");
        Assert.Equal("2", startTypeChange.OldValue);
        Assert.Equal("4", startTypeChange.NewValue);
    }

    [Fact]
    public void Generate_VendorChanged_TracksPropertyChange()
    {
        var preItem = MakeDriver("drv:x.inf", "Driver X", vendor: "OldVendor");
        var postItem = MakeDriver("drv:x.inf", "Driver X", vendor: "NewVendor");

        var pre = MakeSnapshot(Guid.NewGuid(), preItem);
        var post = MakeSnapshot(Guid.NewGuid(), postItem);

        var report = DeltaReportGenerator.Generate(SessionId, pre, post);

        var change = Assert.Single(report.Items[0].Changes);
        Assert.Equal("Vendor", change.Property);
    }

    // ── Mixed scenario ──

    [Fact]
    public void Generate_MixedScenario_CorrectSummary()
    {
        var pre = MakeSnapshot(Guid.NewGuid(),
            MakeDriver("drv:removed.inf", "Removed Driver"),
            MakeDriver("drv:same.inf", "Same Driver", vendor: "V1"),
            MakeService("svc:changed", "Changed Svc", startType: 2));

        var post = MakeSnapshot(Guid.NewGuid(),
            MakeDriver("drv:same.inf", "Same Driver", vendor: "V1"),
            MakeService("svc:changed", "Changed Svc", startType: 4),
            MakeDriver("drv:new.inf", "New Driver"));

        var report = DeltaReportGenerator.Generate(SessionId, pre, post);

        Assert.Equal(1, report.Summary.Removed);
        Assert.Equal(1, report.Summary.Changed);
        Assert.Equal(1, report.Summary.Unchanged);
        Assert.Equal(1, report.Summary.Added);
        Assert.Equal(4, report.Items.Count);
    }

    // ── CompareItems (internal) ──

    [Fact]
    public void CompareItems_IdenticalItems_NoChanges()
    {
        var item = MakeDriver("drv:x.inf", "X", vendor: "V", version: "1.0", present: true);
        var changes = DeltaReportGenerator.CompareItems(item, item);
        Assert.Empty(changes);
    }

    [Fact]
    public void CompareItems_MultipleFieldsChanged_AllTracked()
    {
        var pre = MakeDriver("drv:x.inf", "X", vendor: "OldV", version: "1.0", present: true, driverInf: "old.inf");
        var post = MakeDriver("drv:x.inf", "X", vendor: "NewV", version: "2.0", present: false, driverInf: "new.inf");

        var changes = DeltaReportGenerator.CompareItems(pre, post);

        Assert.Equal(4, changes.Count);
        Assert.Contains(changes, c => c.Property == "Version");
        Assert.Contains(changes, c => c.Property == "Vendor");
        Assert.Contains(changes, c => c.Property == "Present");
        Assert.Contains(changes, c => c.Property == "DriverInf");
    }

    [Fact]
    public void CompareItems_NullToValue_TrackedAsChange()
    {
        var pre = MakeDriver("drv:x.inf", "X");
        var post = MakeDriver("drv:x.inf", "X", vendor: "NewVendor");

        var changes = DeltaReportGenerator.CompareItems(pre, post);

        var vendorChange = Assert.Single(changes);
        Assert.Equal("Vendor", vendorChange.Property);
        Assert.Null(vendorChange.OldValue);
        Assert.Equal("NewVendor", vendorChange.NewValue);
    }

    // ── NextSteps ──

    [Fact]
    public void GenerateNextSteps_WithRemovals_IncludesRebootAdvice()
    {
        var items = new List<DeltaReportItem>
        {
            new()
            {
                ItemId = "drv:removed.inf",
                ItemType = InventoryItemType.DRIVER,
                DisplayName = "Removed",
                Status = DeltaStatus.REMOVED,
            },
        };
        var summary = new DeltaReportSummary { Removed = 1 };

        var steps = DeltaReportGenerator.GenerateNextSteps(items, summary);

        Assert.Contains(steps, s => s.Contains("successfully removed"));
        Assert.Contains(steps, s => s.Contains("reboot", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void GenerateNextSteps_WithServiceStartTypeChange_IncludesServiceAdvice()
    {
        var items = new List<DeltaReportItem>
        {
            new()
            {
                ItemId = "svc:test",
                ItemType = InventoryItemType.SERVICE,
                DisplayName = "Test",
                Status = DeltaStatus.CHANGED,
                Changes = [new PropertyChange { Property = "StartType", OldValue = "2", NewValue = "4" }],
            },
        };
        var summary = new DeltaReportSummary { Changed = 1 };

        var steps = DeltaReportGenerator.GenerateNextSteps(items, summary);

        Assert.Contains(steps, s => s.Contains("start type changed"));
    }

    [Fact]
    public void GenerateNextSteps_NothingChanged_IncludesReviewAdvice()
    {
        var items = new List<DeltaReportItem>
        {
            new()
            {
                ItemId = "drv:same.inf",
                ItemType = InventoryItemType.DRIVER,
                DisplayName = "Same",
                Status = DeltaStatus.UNCHANGED,
            },
        };
        var summary = new DeltaReportSummary { Unchanged = 1 };

        var steps = DeltaReportGenerator.GenerateNextSteps(items, summary);

        Assert.Contains(steps, s => s.Contains("No items were removed or changed"));
        Assert.Contains(steps, s => s.Contains("final system check"));
    }

    // ── ExportAsMarkdown ──

    [Fact]
    public void ExportAsMarkdown_ContainsExpectedSections()
    {
        var pre = MakeSnapshot(Guid.NewGuid(),
            MakeDriver("drv:removed.inf", "Removed Driver"),
            MakeDriver("drv:changed.inf", "Changed Driver", version: "1.0"));

        var post = MakeSnapshot(Guid.NewGuid(),
            MakeDriver("drv:changed.inf", "Changed Driver", version: "2.0"),
            MakeDriver("drv:new.inf", "New Driver"));

        var report = DeltaReportGenerator.Generate(SessionId, pre, post);
        var markdown = DeltaReportGenerator.ExportAsMarkdown(report);

        Assert.Contains("# Delta Report", markdown);
        Assert.Contains("## Summary", markdown);
        Assert.Contains("## Removed Items", markdown);
        Assert.Contains("## Changed Items", markdown);
        Assert.Contains("## New Items", markdown);
        Assert.Contains("## Next Steps", markdown);
        Assert.Contains("Removed Driver", markdown);
        Assert.Contains("Changed Driver", markdown);
        Assert.Contains("New Driver", markdown);
    }

    [Fact]
    public void ExportAsMarkdown_UnchangedOnly_NoRemovedOrChangedSections()
    {
        var item = MakeDriver("drv:same.inf", "Same Driver");
        var pre = MakeSnapshot(Guid.NewGuid(), item);
        var post = MakeSnapshot(Guid.NewGuid(), item);

        var report = DeltaReportGenerator.Generate(SessionId, pre, post);
        var markdown = DeltaReportGenerator.ExportAsMarkdown(report);

        Assert.Contains("# Delta Report", markdown);
        Assert.DoesNotContain("## Removed Items", markdown);
        Assert.DoesNotContain("## Changed Items", markdown);
        Assert.DoesNotContain("## New Items", markdown);
    }

    // ── Report metadata ──

    [Fact]
    public void Generate_SetsReportMetadata()
    {
        var preId = Guid.NewGuid();
        var postId = Guid.NewGuid();

        var pre = MakeSnapshot(preId, MakeDriver("drv:a.inf", "A"));
        var post = MakeSnapshot(postId, MakeDriver("drv:a.inf", "A"));

        var report = DeltaReportGenerator.Generate(SessionId, pre, post);

        Assert.NotEqual(Guid.Empty, report.Id);
        Assert.Equal(SessionId, report.SessionId);
        Assert.Equal(preId, report.PreSnapshotId);
        Assert.Equal(postId, report.PostSnapshotId);
        Assert.True(report.CreatedAt <= DateTime.UtcNow);
    }

    // ── Empty snapshots ──

    [Fact]
    public void Generate_BothEmpty_EmptyReport()
    {
        var pre = MakeSnapshot(Guid.NewGuid());
        var post = MakeSnapshot(Guid.NewGuid());

        var report = DeltaReportGenerator.Generate(SessionId, pre, post);

        Assert.Empty(report.Items);
        Assert.Equal(0, report.Summary.Removed);
        Assert.Equal(0, report.Summary.Changed);
        Assert.Equal(0, report.Summary.Unchanged);
        Assert.Equal(0, report.Summary.Added);
    }
}
