namespace Meteion.Toolkit.MVVM.Services;

/// <summary>
/// Describes a navigation reported by <see cref="INavigationProgress"/>.
/// </summary>
/// <param name="viewModelType">The view model type being navigated to.</param>
/// <param name="navigationParameter">The parameter supplied to the navigation, if any.</param>
/// <param name="direction">Whether the navigation is forward or back.</param>
public sealed class NavigationProgressEventArgs(Type viewModelType, object? navigationParameter, NavigationDirection direction) : EventArgs
{
    /// <summary>Gets the view model type being navigated to.</summary>
    public Type ViewModelType { get; } = viewModelType;

    /// <summary>Gets the parameter supplied to the navigation, if any.</summary>
    public object? NavigationParameter { get; } = navigationParameter;

    /// <summary>Gets whether the navigation is forward or back.</summary>
    public NavigationDirection Direction { get; } = direction;
}
