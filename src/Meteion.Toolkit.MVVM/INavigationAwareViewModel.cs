namespace Meteion.Toolkit.MVVM;

/// <summary>
/// Implemented by view models that need to react when the navigation service navigates to or away from them.
/// </summary>
public interface INavigationAwareViewModel
{
    /// <summary>Called after navigation has moved away from this view model's page.</summary>
    void OnNavigatedFrom();

    /// <summary>Called after navigation has moved to this view model's page.</summary>
    /// <param name="navigationParameter">The parameter supplied to the navigation request, or <see langword="null"/> if none.</param>
    void OnNavigatedTo(object? navigationParameter);
}

/// <summary>
/// Asynchronous counterpart of <see cref="INavigationAwareViewModel"/>. The navigation service awaits these methods,
/// so long-running work here can trigger the navigation busy indicator.
/// </summary>
public interface IAsyncNavigationAwareViewModel
{
    /// <summary>Called after navigation has moved away from this view model's page.</summary>
    /// <returns>A task that completes when the view model has finished its leave-page work.</returns>
    Task OnNavigatedFromAsync();

    /// <summary>Called after navigation has moved to this view model's page.</summary>
    /// <param name="navigationParameter">The parameter supplied to the navigation request, or <see langword="null"/> if none.</param>
    /// <returns>A task that completes when the view model has finished its enter-page work.</returns>
    Task OnNavigatedToAsync(object? navigationParameter);
}
