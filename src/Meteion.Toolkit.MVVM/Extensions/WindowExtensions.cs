using System.Windows;
using System.Windows.Controls;
using Meteion.Toolkit.WPF;

namespace Meteion.Toolkit.MVVM
{
    /// <summary>
    /// Extension methods for <see cref="Window"/> that support the navigation shell pattern.
    /// </summary>
    /// <summary>
    /// Extension methods for <see cref="Window"/> that support the navigation shell pattern.
    /// </summary>
    /// <summary>
    /// Extension methods for <see cref="Window"/> that support the navigation shell pattern.
    /// </summary>
    /// <summary>
    /// Extension methods for <see cref="Window"/> that support the navigation shell pattern.
    /// </summary>
    public static class WindowExtensions
    {
        /// <summary>
        /// Retrieve the data context from the frame contained within the window.
        /// </summary>
        /// <param name="window">The window whose content frame is inspected.</param>
        /// <returns>The frame's current data context, or <see langword="null"/> if the window's content is not a frame.</returns>
        public static object? GetDataContext(this Window window)
        {
            if (window.Content is Frame frame)
            {
                return frame.GetDataContext();
            }

            return null;
        }
    }
}
