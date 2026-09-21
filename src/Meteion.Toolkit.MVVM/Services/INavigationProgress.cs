namespace Meteion.Toolkit.MVVM.Services;

/// <summary>
/// Optional companion to <see cref="INavigationService"/> that reports when a navigation is taking long
/// enough to warrant showing a busy indicator. Kept as a separate interface so existing
/// <see cref="INavigationService"/> implementations and consumers are not broken.
/// </summary>
public interface INavigationProgress
{
    /// <summary>
    /// Raised when a navigation's post-navigation work (e.g. <see cref="IAsyncNavigationAwareViewModel.OnNavigatedToAsync"/>)
    /// has not completed within <see cref="NavigationIndicatorDelay"/>. Not raised for navigations that finish
    /// before the delay elapses.
    /// </summary>
    event EventHandler<NavigationProgressEventArgs>? NavigationStarted;

    /// <summary>
    /// Raised when a navigation completes, but only for navigations that raised <see cref="NavigationStarted"/>.
    /// </summary>
    event EventHandler<NavigationProgressEventArgs>? NavigationCompleted;

    /// <summary>
    /// How long a navigation's post-navigation work must run before <see cref="NavigationStarted"/> is raised.
    /// Defaults to 200ms.
    /// </summary>
    TimeSpan NavigationIndicatorDelay { get; set; }
}
