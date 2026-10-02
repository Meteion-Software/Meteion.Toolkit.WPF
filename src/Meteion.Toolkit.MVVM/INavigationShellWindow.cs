using System.Windows.Controls;

namespace Meteion.Toolkit.MVVM;

/// <summary>
/// A window that hosts a <see cref="Frame"/> into which the navigation service loads pages.
/// </summary>
public interface INavigationShellWindow
{
    /// <summary>Gets the frame that pages are navigated within.</summary>
    /// <returns>The shell's navigation frame.</returns>
    Frame GetNavigationFrame();

    /// <summary>Shows the window.</summary>
    void Show();

    /// <summary>Closes the window.</summary>
    void Close();
}
