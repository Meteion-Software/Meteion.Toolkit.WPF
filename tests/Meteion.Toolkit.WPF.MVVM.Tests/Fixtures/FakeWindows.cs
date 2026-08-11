using System.Windows;

namespace Meteion.Toolkit.WPF.MVVM.Tests.Fixtures;

public class FakeWindowA : Window;

public class FakeWindowB : Window;

/// <summary>
/// Tracks whether Dispose has run — used to prove WindowResolutionService actually
/// disposes the DI scope it created when the window closes, by holding a reference to
/// a scoped service resolved from that same scope.
/// </summary>
public class DisposableTracker : IDisposable
{
    public bool IsDisposed { get; private set; }
    public void Dispose() => IsDisposed = true;
}

public class FakeWindowWithScopedDependency(DisposableTracker tracker) : Window
{
    public DisposableTracker Tracker { get; } = tracker;
}
