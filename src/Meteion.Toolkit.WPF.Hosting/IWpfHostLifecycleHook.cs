using System;
using System.Windows;

namespace Meteion.Toolkit.WPF.Hosting;

/// <summary>
/// A generic extensibility point into <see cref="WpfApplicationHost"/>'s startup. Register any number of
/// implementations as <see cref="IWpfHostLifecycleHook"/> services; the host resolves them all at the start of
/// <see cref="WpfApplicationHost.StartAsync"/>. Both members have empty default implementations, so implement
/// only the ones you need.
/// </summary>
public interface IWpfHostLifecycleHook
{
    /// <summary>
    /// Called on the UI thread after the launch window has been resolved and the base host has started, immediately
    /// before <c>Application.Run</c>.
    /// </summary>
    /// <param name="launchWindow">The resolved launch window (not yet shown).</param>
    void OnLaunchWindowCreated(Window launchWindow) { }

    /// <summary>
    /// Called when startup threw before <c>Application.Run</c> began. The host rethrows <paramref name="exception"/>
    /// afterwards. An exception thrown from this method is logged and never replaces the original.
    /// </summary>
    /// <param name="exception">The exception that stopped startup.</param>
    void OnStartupFailed(Exception exception) { }
}
