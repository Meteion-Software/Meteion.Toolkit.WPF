namespace Meteion.Toolkit.MVVM.Services;

/// <summary>
/// The direction of a navigation relative to the frame's history.
/// </summary>
public enum NavigationDirection
{
    /// <summary>A new page was navigated to.</summary>
    Forward,

    /// <summary>The previous page in the history was returned to.</summary>
    Back
}
