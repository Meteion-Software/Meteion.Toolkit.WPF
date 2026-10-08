using System.ComponentModel;
using System.Windows.Controls;

namespace Meteion.Toolkit.MVVM.Services;

/// <summary>
/// Navigates a frame between pages, selecting each page from the view model type requested.
/// </summary>
public interface INavigationService
{
    /// <summary>Gets or sets whether navigation is blocked, for example while a form has unsaved changes.</summary>
    bool IsNavigationLocked { get; set; }

    /// <summary>Gets whether there is a page to go back to and navigation is not locked.</summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Attaches the service to the frame that pages are shown in.
    /// </summary>
    /// <param name="shellFrame">The frame to navigate.</param>
    void Initialize(Frame shellFrame);

    /// <summary>
    /// Navigates to the page registered for a view model.
    /// </summary>
    /// <typeparam name="TViewModel">The view model whose page to show.</typeparam>
    /// <param name="navigationParameter">Optional data passed to the view model's navigation callbacks.</param>
    /// <returns><see langword="true"/> if navigation occurred; otherwise <see langword="false"/>.</returns>
    Task<bool> NavigateTo<TViewModel>(object? navigationParameter = null)
        where TViewModel : INotifyPropertyChanged;

    /// <summary>
    /// Navigates to the page registered for a view model.
    /// </summary>
    /// <param name="viewModel">The view model type whose page to show.</param>
    /// <param name="navigationParameter">Optional data passed to the view model's navigation callbacks.</param>
    /// <returns><see langword="true"/> if navigation occurred; otherwise <see langword="false"/>.</returns>
    Task<bool> NavigateTo(Type viewModel, object? navigationParameter = null);

    /// <summary>
    /// Navigates to the previous page. The service keeps its own back stack of view model types and parameters
    /// (not page instances), so the page and view model are resolved again: the same instances for Scoped
    /// registrations, new ones for Transient. The destination receives the parameter it was originally navigated with.
    /// </summary>
    /// <returns><see langword="true"/> if navigation occurred; otherwise <see langword="false"/>.</returns>
    Task<bool> GoBack();

    /// <summary>Clears the back stack so the user cannot navigate back past the current page.</summary>
    void CleanNavigation();
}
