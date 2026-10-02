using System.Windows;
using System.Windows.Controls;

namespace Meteion.Toolkit.WPF;

/// <summary>
/// Extension methods for <see cref="Frame"/> navigation hosts.
/// </summary>
public static class FrameExtensions
{
    /// <summary>
    /// Gets the data context of the frame's current content.
    /// </summary>
    /// <param name="frame">The frame to inspect.</param>
    /// <returns>The content's data context, or null when the content is not a <see cref="FrameworkElement"/>.</returns>
    public static object? GetDataContext(this Frame frame)
    {
        if (frame.Content is FrameworkElement element)
        {
            return element.DataContext;
        }

        return null;
    }

    /// <summary>
    /// Clears the frame's back stack so the user cannot navigate back past the current page.
    /// </summary>
    /// <param name="frame">The frame whose back entries are removed.</param>
    public static void CleanNavigation(this Frame frame)
    {
        while (frame.CanGoBack)
        {
            frame.RemoveBackEntry();
        }
    }
}
