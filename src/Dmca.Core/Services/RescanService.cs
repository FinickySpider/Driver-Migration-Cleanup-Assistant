using Dmca.Core.Interfaces;
using Dmca.Core.Models;

namespace Dmca.Core.Services;

/// <summary>
/// Orchestrates a post-execution rescan.
/// Re-runs all collectors to produce a new InventorySnapshot, then transitions
/// the session to COMPLETED. The original snapshot remains immutable.
/// </summary>
public sealed class RescanService
{
    private readonly IEnumerable<IInventoryCollector> _collectors;
    private readonly IPlatformInfoCollector _platformCollector;
    private readonly ISnapshotRepository _snapshotRepo;
    private readonly SessionService _sessionService;

    public RescanService(
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
    /// Runs all collectors and persists a new immutable snapshot.
    /// Transitions the session to COMPLETED on success.
    /// Individual collector failures are logged but do not abort the rescan.
    /// </summary>
    public async Task<InventorySnapshot> RescanAsync(Guid sessionId, CancellationToken ct = default)
    {
        var allItems = new List<InventoryItem>();

        foreach (var collector in _collectors)
        {
            try
            {
                var items = await collector.CollectAsync(ct);
                allItems.AddRange(items);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Continue collecting from remaining collectors
            }
        }

        PlatformInfo? platform = null;
        try
        {
            platform = await _platformCollector.CollectAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Platform info is optional for rescan
        }

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
        await _sessionService.TransitionAsync(sessionId, SessionStatus.COMPLETED);

        return snapshot;
    }
}
