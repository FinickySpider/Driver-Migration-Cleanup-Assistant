using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dmca.Core.AI;
using Dmca.Core.Models;
using Dmca.Core.Services;

namespace Dmca.App.ViewModels;

/// <summary>
/// AI chat pane — allows the user to interact with the AI advisor.
/// </summary>
public sealed partial class AiChatViewModel : PageViewModel
{
    private readonly AiAdvisorService? _aiService;
    private readonly SessionService _sessionService;

    public override string Title => "AI Advisor";

    [ObservableProperty]
    private string _userMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private Guid _sessionId;

    public ObservableCollection<ChatEntry> ChatHistory { get; } = [];

    public AiChatViewModel(SessionService sessionService, AiAdvisorService? aiService = null)
    {
        _sessionService = sessionService;
        _aiService = aiService;
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        if (string.IsNullOrWhiteSpace(UserMessage) || _aiService is null) return;

        var message = UserMessage;
        UserMessage = string.Empty;
        ChatHistory.Add(new ChatEntry("User", message));

        IsBusy = true;
        try
        {
            var result = await _aiService.ChatAsync(message, SessionId);
            ChatHistory.Add(new ChatEntry("AI Advisor", result.Content));
        }
        catch (Exception ex)
        {
            ChatHistory.Add(new ChatEntry("System", $"Error: {ex.Message}"));
        }
        finally
        {
            IsBusy = false;
        }
    }
}

/// <summary>
/// A single entry in the chat history.
/// </summary>
public sealed record ChatEntry(string Sender, string Message);
