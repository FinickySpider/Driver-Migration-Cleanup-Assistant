using Dmca.Core.Interfaces;
using Dmca.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    private readonly ILogger<ScanService> _logger;

    public ScanService(
        IEnumerable<IInventoryCollector> collectors,
        IPlatformInfoCollector platformCollector,
        ISnapshotRepository snapshotRepo,
        SessionService sessionService,
        ILogger<ScanService>? logger = null)
    {
        _collectors = collectors;
        _platformCollector = platformCollector;
        _snapshotRepo = snapshotRepo;
        _sessionService = sessionService;
        _logger = logger ?? NullLogger<ScanService>.Instance;
    }

    /// <summary>
    /// Runs all collectors and persists an immutable snapshot.
    /// Transitions the session to SCANNED on success.
    /// Individual collector failures are logged but do not abort the scan.
    /// </summary>
    public async Task<InventorySnapshot> ScanAsync(Guid sessionId, CancellationToken ct = default)
    {
        using var _ = DmcaLog.BeginTimedOperation(_logger, "ScanService.ScanAsync");

        var allItems = new List<InventoryItem>();
        var collectorErrors = new List<string>();

        foreach (var collector in _collectors)
        {
            try
            {
                _logger.LogDebug(DmcaLog.Events.CollectorStarted, "Running collector {Type}", collector.ItemType);
                var items = await collector.CollectAsync(ct);
                allItems.AddRange(items);
                _logger.LogInformation(DmcaLog.Events.CollectorCompleted,
                    "Collector {Type} found {Count} items", collector.ItemType, items.Count);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(DmcaLog.Events.CollectorFailed, ex,
                    "Collector {Type} failed: {Message}", collector.ItemType, ex.Message);
                collectorErrors.Add($"{collector.ItemType}: {ex.Message}");
            }
        }

        PlatformInfo? platform = null;
        try
        {
            platform = await _platformCollector.CollectAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            collectorErrors.Add($"PlatformInfo: {ex.Message}");
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
        await _sessionService.TransitionAsync(sessionId, SessionStatus.SCANNED);

        return snapshot;
    }
}
