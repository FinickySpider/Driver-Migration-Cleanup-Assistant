using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.Core.Services;

/// <summary>
/// Orchestrates all inventory collectors to produce an immutable InventorySnapshot.
/// </summary>
public sealed class ScanService
{
    private readonly IEnumerable<IInventoryCollector> _collectors;
    private readonly IPlatformInfoCollector _platformCollector;
    private readonly ISnapshotRepository _snapshotRepo;
    private readonly SessionService _sessionService;

    public ScanService(
        IEnumerable<IInventoryCollector> collectors,
        IPlatformInfoCollector platformCollector,
        ISnapshotRepository snapshotRepo,
        SessionService sessionService)
    {
        _collectors = collectors;
        _platformCollector = platformCollector;
        _snapshotRepo = snapshotRepo;
        _sessionService = sessionService;
    }

    /// <summary>
    /// Runs all collectors and persists an immutable snapshot.
    /// Transitions the session to SCANNED on success.
    /// </summary>
    public async Task<InventorySnapshot> ScanAsync(Guid sessionId, CancellationToken ct = default)
    {
        var allItems = new List<InventoryItem>();

        foreach (var collector in _collectors)
        {
            var items = await collector.CollectAsync(ct);
            allItems.AddRange(items);
        }

        var platform = await _platformCollector.CollectAsync(ct);

        var summary = new SnapshotSummary
        {
            Drivers = allItems.Count(i => i.ItemType == InventoryItemType.DRIVER),
            Services = allItems.Count(i => i.ItemType == InventoryItemType.SERVICE),
            Packages = allItems.Count(i => i.ItemType == InventoryItemType.DRIVER_PACKAGE),
            Apps = allItems.Count(i => i.ItemType == InventoryItemType.APP),
            Platform = platform,
        };

        var snapshot = new InventorySnapshot
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow,
            Summary = summary,
            Items = allItems.AsReadOnly(),
        };

        await _snapshotRepo.CreateAsync(snapshot);
        await _sessionService.TransitionAsync(sessionId, SessionStatus.SCANNED);

        return snapshot;
    }
}
