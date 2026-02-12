using CommunityToolkit.Mvvm.ComponentModel;

namespace Dmca.App.ViewModels;

/// <summary>
/// Base class for page view models.
/// </summary>
public abstract partial class PageViewModel : ObservableObject
{
    /// <summary>
    /// Display title for the page (shown in sidebar/header).
    /// </summary>
    public abstract string Title { get; }

    /// <summary>
    /// Called when the page becomes visible.
    /// </summary>
    public virtual Task OnNavigatedToAsync() => Task.CompletedTask;
}
