using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;
using Dmca.Core.Services;

namespace Dmca.App.ViewModels;

/// <summary>
/// Inventory table — displays all inventory items with filtering and sorting.
/// </summary>
public sealed partial class InventoryViewModel : PageViewModel
{
    private readonly ScanService _scanService;
    private readonly ISnapshotRepository _snapshotRepo;
    private readonly SessionService _sessionService;

    public override string Title => "Inventory";

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private string _filterText = string.Empty;

    [ObservableProperty]
    private InventoryItemType? _filterType;

    [ObservableProperty]
    private InventoryItem? _selectedItem;

    [ObservableProperty]
    private InventorySnapshot? _snapshot;

    public ObservableCollection<InventoryItem> Items { get; } = [];
    public ObservableCollection<InventoryItem> FilteredItems { get; } = [];

    public InventoryViewModel(ScanService scanService, ISnapshotRepository snapshotRepo, SessionService sessionService)
    {
        _scanService = scanService;
        _snapshotRepo = snapshotRepo;
        _sessionService = sessionService;
    }

    partial void OnFilterTextChanged(string value) => ApplyFilter();
    partial void OnFilterTypeChanged(InventoryItemType? value) => ApplyFilter();

    [RelayCommand]
    private async Task ScanAsync()
    {
        var session = await _sessionService.GetCurrentSessionAsync();
        if (session is null) return;

        IsScanning = true;
        try
        {
            Snapshot = await _scanService.ScanAsync(session.Id);
            LoadItems(Snapshot);
        }
        finally
        {
            IsScanning = false;
        }
    }

    public void LoadItems(InventorySnapshot snapshot)
    {
        Snapshot = snapshot;
        Items.Clear();
        foreach (var item in snapshot.Items)
            Items.Add(item);
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        FilteredItems.Clear();
        foreach (var item in Items)
        {
            if (FilterType.HasValue && item.ItemType != FilterType.Value)
                continue;
            if (!string.IsNullOrWhiteSpace(FilterText) &&
                !item.DisplayName.Contains(FilterText, StringComparison.OrdinalIgnoreCase) &&
                !(item.Vendor?.Contains(FilterText, StringComparison.OrdinalIgnoreCase) == true) &&
                !item.ItemId.Contains(FilterText, StringComparison.OrdinalIgnoreCase))
                continue;
            FilteredItems.Add(item);
        }
    }
}
