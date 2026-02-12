using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dmca.Core.Models;
using Dmca.Core.Services;

namespace Dmca.App.ViewModels;

/// <summary>
/// Interview wizard — multi-step session setup and user facts collection.
/// </summary>
public sealed partial class InterviewViewModel : PageViewModel
{
    private readonly SessionService _sessionService;
    private readonly UserFactsService _userFactsService;

    public override string Title => "Interview";

    [ObservableProperty]
    private int _currentStep;

    [ObservableProperty]
    private int _totalSteps = 3;

    [ObservableProperty]
    private string _previousHardware = string.Empty;

    [ObservableProperty]
    private string _newHardware = string.Empty;

    [ObservableProperty]
    private string _additionalNotes = string.Empty;

    [ObservableProperty]
    private bool _isComplete;

    [ObservableProperty]
    private Session? _session;

    public ObservableCollection<UserFact> CollectedFacts { get; } = [];

    public InterviewViewModel(SessionService sessionService, UserFactsService userFactsService)
    {
        _sessionService = sessionService;
        _userFactsService = userFactsService;
    }

    public override async Task OnNavigatedToAsync()
    {
        if (Session is null)
        {
            Session = await _sessionService.CreateSessionAsync();
        }
    }

    [RelayCommand]
    private void NextStep()
    {
        if (CurrentStep < TotalSteps - 1)
            CurrentStep++;
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (CurrentStep > 0)
            CurrentStep--;
    }

    [RelayCommand]
    private async Task FinishAsync()
    {
        if (Session is null) return;

        // Save user facts
        if (!string.IsNullOrWhiteSpace(PreviousHardware))
        {
            await _userFactsService.AddFactAsync(Session.Id, "previous_hardware", PreviousHardware, FactSource.USER);
        }

        if (!string.IsNullOrWhiteSpace(NewHardware))
        {
            await _userFactsService.AddFactAsync(Session.Id, "new_hardware", NewHardware, FactSource.USER);
        }

        if (!string.IsNullOrWhiteSpace(AdditionalNotes))
        {
            await _userFactsService.AddFactAsync(Session.Id, "additional_notes", AdditionalNotes, FactSource.USER);
        }

        var facts = await _userFactsService.GetFactsAsync(Session.Id);
        CollectedFacts.Clear();
        foreach (var f in facts) CollectedFacts.Add(f);

        IsComplete = true;
    }
}
