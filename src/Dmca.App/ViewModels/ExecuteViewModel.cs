using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dmca.Core.Execution;
using Dmca.Core.Models;

namespace Dmca.App.ViewModels;

/// <summary>
/// Execute screen with dry-run, two-step confirmation, and progress display.
/// </summary>
public sealed partial class ExecuteViewModel : PageViewModel
{
    private readonly ActionQueueBuilder _queueBuilder;
    private readonly ExecutionEngine _executionEngine;
    private readonly AuditLogger _auditLogger;

    public override string Title => "Execute";

    [ObservableProperty]
    private Guid _sessionId;

    [ObservableProperty]
    private ActionQueue? _queue;

    [ObservableProperty]
    private bool _isExecuting;

    [ObservableProperty]
    private bool _isDryRunComplete;

    [ObservableProperty]
    private bool _acknowledgeRisk;

    [ObservableProperty]
    private string _confirmationText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private int _progressCurrent;

    [ObservableProperty]
    private int _progressTotal;

    [ObservableProperty]
    private string _currentActionDescription = string.Empty;

    public ObservableCollection<ExecutionAction> Actions { get; } = [];
    public ObservableCollection<AuditLogEntry> AuditEntries { get; } = [];

    /// <summary>
    /// Two-step confirmation: EXECUTE must be typed and risk acknowledged.
    /// </summary>
    public bool CanExecuteLive => IsDryRunComplete && AcknowledgeRisk && ConfirmationText == "EXECUTE";

    private CancellationTokenSource? _cts;

    public ExecuteViewModel(ActionQueueBuilder queueBuilder, ExecutionEngine executionEngine, AuditLogger auditLogger)
    {
        _queueBuilder = queueBuilder;
        _executionEngine = executionEngine;
        _auditLogger = auditLogger;
    }

    partial void OnAcknowledgeRiskChanged(bool value) => OnPropertyChanged(nameof(CanExecuteLive));
    partial void OnConfirmationTextChanged(string value) => OnPropertyChanged(nameof(CanExecuteLive));
    partial void OnIsDryRunCompleteChanged(bool value) => OnPropertyChanged(nameof(CanExecuteLive));

    [RelayCommand]
    private async Task BuildQueueAsync()
    {
        Queue = await _queueBuilder.BuildAsync(SessionId, ExecutionMode.DRY_RUN);
        Actions.Clear();
        foreach (var a in Queue.Actions)
            Actions.Add(a);
        StatusMessage = $"Queue built with {Queue.Actions.Count} actions. Ready for dry run.";
    }

    [RelayCommand]
    private async Task DryRunAsync()
    {
        if (Queue is null)
        {
            StatusMessage = "Build the queue first.";
            return;
        }

        await RunQueueAsync(Queue.Id);
        IsDryRunComplete = Queue.OverallStatus is ActionStatus.DRY_RUN or ActionStatus.COMPLETED;
        StatusMessage = IsDryRunComplete
            ? "Dry run complete. Review results, then proceed to live execution."
            : $"Dry run ended with status: {Queue.OverallStatus}";
    }

    [RelayCommand]
    private async Task ExecuteLiveAsync()
    {
        if (!CanExecuteLive) return;

        // Build a new queue for live execution
        Queue = await _queueBuilder.BuildAsync(SessionId, ExecutionMode.LIVE);
        Actions.Clear();
        foreach (var a in Queue.Actions)
            Actions.Add(a);

        await RunQueueAsync(Queue.Id);
        StatusMessage = $"Execution {Queue.OverallStatus}.";

        // Refresh audit entries
        var entries = await _auditLogger.GetBySessionAsync(SessionId);
        AuditEntries.Clear();
        foreach (var e in entries) AuditEntries.Add(e);
    }

    [RelayCommand]
    private void CancelExecution()
    {
        _cts?.Cancel();
        StatusMessage = "Cancellation requested...";
    }

    private async Task RunQueueAsync(Guid queueId)
    {
        IsExecuting = true;
        _cts = new CancellationTokenSource();

        _executionEngine.ActionStarting += OnActionStarting;
        _executionEngine.ActionCompleted += OnActionCompleted;

        try
        {
            Queue = await _executionEngine.ExecuteAsync(queueId, _cts.Token);
        }
        finally
        {
            _executionEngine.ActionStarting -= OnActionStarting;
            _executionEngine.ActionCompleted -= OnActionCompleted;
            IsExecuting = false;
            _cts.Dispose();
            _cts = null;
        }
    }

    private void OnActionStarting(object? sender, ActionProgressEventArgs e)
    {
        ProgressCurrent = e.Current;
        ProgressTotal = e.Total;
        CurrentActionDescription = $"({e.Current}/{e.Total}) {e.Action.DisplayName}";
    }

    private void OnActionCompleted(object? sender, ActionProgressEventArgs e)
    {
        // Update the observable action in the list
        var idx = Actions.IndexOf(Actions.FirstOrDefault(a => a.Id == e.Action.Id)!);
        if (idx >= 0) Actions[idx] = e.Action;
    }
}
