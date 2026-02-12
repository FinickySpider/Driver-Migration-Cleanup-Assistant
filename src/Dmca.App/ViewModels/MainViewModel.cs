using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Dmca.App.ViewModels;

/// <summary>
/// Root view model for the main window. Manages sidebar navigation and page hosting.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private PageViewModel? _currentPage;

    [ObservableProperty]
    private string _statusText = "Ready";

    public ObservableCollection<NavigationItem> NavigationItems { get; } = [];

    /// <summary>
    /// Registers all page view models and sets the initial page.
    /// </summary>
    public void Initialize(IEnumerable<NavigationItem> items)
    {
        NavigationItems.Clear();
        foreach (var item in items)
            NavigationItems.Add(item);

        if (NavigationItems.Count > 0)
            NavigateToCommand.Execute(NavigationItems[0]);
    }

    [RelayCommand]
    private async Task NavigateToAsync(NavigationItem item)
    {
        if (item.ViewModel == CurrentPage) return;
        CurrentPage = item.ViewModel;

        foreach (var nav in NavigationItems)
            nav.IsActive = nav == item;

        await item.ViewModel.OnNavigatedToAsync();
    }
}

/// <summary>
/// Represents a sidebar navigation entry.
/// </summary>
public sealed partial class NavigationItem : ObservableObject
{
    public required string Label { get; init; }
    public required string Icon { get; init; }
    public required PageViewModel ViewModel { get; init; }

    [ObservableProperty]
    private bool _isActive;
}
