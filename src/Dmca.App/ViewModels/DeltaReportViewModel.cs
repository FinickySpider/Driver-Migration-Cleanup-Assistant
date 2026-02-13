using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dmca.Core.Interfaces;
using Dmca.Core.Models;
using Dmca.Core.Reports;
using Dmca.Core.Services;

namespace Dmca.App.ViewModels;

/// <summary>
/// Delta report screen — shows comparison between pre and post execution snapshots.
/// Supports rescan, delta generation, and markdown export.
/// </summary>
public sealed partial class DeltaReportViewModel : PageViewModel
{
    private readonly RescanService _rescanService;
    private readonly ISnapshotRepository _snapshotRepo;

    public override string Title => "Delta Report";

    [ObservableProperty]
    private Guid _sessionId;

    [ObservableProperty]
    private bool _isRescanning;

    [ObservableProperty]
    private DeltaReport? _report;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _exportedMarkdown = string.Empty;

    public ObservableCollection<DeltaReportItem> Items { get; } = [];
    public ObservableCollection<string> NextSteps { get; } = [];

    public DeltaReportViewModel(RescanService rescanService, ISnapshotRepository snapshotRepo)
    {
        _rescanService = rescanService;
        _snapshotRepo = snapshotRepo;
    }

    /// <summary>
    /// Triggers a rescan and generates the delta report.
    /// </summary>
    [RelayCommand]
    private async Task RescanAndGenerateAsync()
    {
        IsRescanning = true;
        StatusMessage = "Running rescan collectors...";

        try
        {
            // Get pre-execution snapshot (first snapshot for session)
            var allSnapshots = await _snapshotRepo.GetAllBySessionIdAsync(SessionId);
            if (allSnapshots.Count == 0)
            {
                StatusMessage = "No initial snapshot found. Run a scan first.";
                return;
            }

            var preSnapshot = allSnapshots[0];

            // Run rescan
            var postSnapshot = await _rescanService.RescanAsync(SessionId);

            // Generate delta
            Report = DeltaReportGenerator.Generate(SessionId, preSnapshot, postSnapshot);
            LoadReport(Report);

            StatusMessage = $"Delta report generated: {Report.Summary.Removed} removed, " +
                            $"{Report.Summary.Changed} changed, {Report.Summary.Unchanged} unchanged.";
        }
        catch (InvalidOperationException ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsRescanning = false;
        }
    }

    /// <summary>
    /// Generates delta from existing snapshots without rescanning.
    /// </summary>
    [RelayCommand]
    private async Task GenerateFromExistingAsync()
    {
        var allSnapshots = await _snapshotRepo.GetAllBySessionIdAsync(SessionId);
        if (allSnapshots.Count < 2)
        {
            StatusMessage = "Need at least two snapshots. Run a rescan first.";
            return;
        }

        var preSnapshot = allSnapshots[0];
        var postSnapshot = allSnapshots[^1];

        Report = DeltaReportGenerator.Generate(SessionId, preSnapshot, postSnapshot);
        LoadReport(Report);

        StatusMessage = $"Delta report generated from existing snapshots: {Report.Summary.Removed} removed, " +
                        $"{Report.Summary.Changed} changed, {Report.Summary.Unchanged} unchanged.";
    }

    /// <summary>
    /// Exports the current report as markdown.
    /// </summary>
    [RelayCommand]
    private void ExportMarkdown()
    {
        if (Report is null)
        {
            StatusMessage = "No report to export.";
            return;
        }

        ExportedMarkdown = DeltaReportGenerator.ExportAsMarkdown(Report);
        StatusMessage = "Markdown exported. Copy from the text area below.";
    }

    private void LoadReport(DeltaReport report)
    {
        Items.Clear();
        foreach (var item in report.Items.Where(i => i.Status != DeltaStatus.UNCHANGED))
            Items.Add(item);

        NextSteps.Clear();
        foreach (var step in report.NextSteps)
            NextSteps.Add(step);
    }
}
