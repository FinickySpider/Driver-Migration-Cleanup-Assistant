using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dmca.Core.Models;
using Dmca.Core.Services;

namespace Dmca.App.ViewModels;

/// <summary>
/// Proposal review screen — diff view showing proposal changes for approval/rejection.
/// </summary>
public sealed partial class ProposalReviewViewModel : PageViewModel
{
    private readonly ProposalService _proposalService;
    private readonly PlanMergeService _planMergeService;

    public override string Title => "Proposals";

    [ObservableProperty]
    private Proposal? _selectedProposal;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private Guid _sessionId;

    public ObservableCollection<Proposal> Proposals { get; } = [];

    public ProposalReviewViewModel(ProposalService proposalService, PlanMergeService planMergeService)
    {
        _proposalService = proposalService;
        _planMergeService = planMergeService;
    }

    public override async Task OnNavigatedToAsync()
    {
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (SessionId == Guid.Empty) return;
        var proposals = await _proposalService.ListBySessionAsync(SessionId);
        Proposals.Clear();
        foreach (var p in proposals) Proposals.Add(p);
    }

    [RelayCommand]
    private async Task ApproveAsync(Proposal proposal)
    {
        await _proposalService.ApproveAsync(proposal.Id);
        StatusMessage = $"Proposal '{proposal.Title}' approved.";
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task RejectAsync(Proposal proposal)
    {
        await _proposalService.RejectAsync(proposal.Id);
        StatusMessage = $"Proposal '{proposal.Title}' rejected.";
        await RefreshAsync();
    }
}
