using Meteion.Toolkit.MVVM;
using System.ComponentModel;

namespace Meteion.Toolkit.WPF.MVVM.Tests.Fixtures;

// Required by INotifyPropertyChanged for the generic constraints these fixtures exist
// to satisfy; never raised since nothing here reacts to property changes.
#pragma warning disable CS0067

public class FakeViewModelA : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
}

public class FakeViewModelB : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
}

#pragma warning restore CS0067

/// <summary>
/// Implements both navigation-aware interfaces and appends a tagged entry to a shared log for
/// every callback, so tests can assert the exact interleaving of sync/async OnNavigatedFrom/To
/// calls across the "from" and "to" view models of a single navigation. Async callbacks can
/// optionally be held open via the Gate properties to control timing (e.g. for asserting
/// NavigationStarted/NavigationCompleted only fire once NavigationIndicatorDelay elapses).
/// </summary>
public class FakeNavigationAwareViewModel(string name, List<string> sharedLog) :
    INotifyPropertyChanged, INavigationAwareViewModel, IAsyncNavigationAwareViewModel
{
    // Required by INotifyPropertyChanged; never raised since nothing here reacts to property changes.
#pragma warning disable CS0067
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067

    public object? LastNavigatedToParameter { get; private set; }

    public TaskCompletionSource<bool>? NavigatedFromAsyncGate { get; set; }

    public TaskCompletionSource<bool>? NavigatedToAsyncGate { get; set; }

    public void OnNavigatedFrom() => sharedLog.Add($"{name}.{nameof(OnNavigatedFrom)}");

    public void OnNavigatedTo(object? navigationParameter)
    {
        sharedLog.Add($"{name}.{nameof(OnNavigatedTo)}");
        LastNavigatedToParameter = navigationParameter;
    }

    public async Task OnNavigatedFromAsync()
    {
        sharedLog.Add($"{name}.{nameof(OnNavigatedFromAsync)}:start");
        if (NavigatedFromAsyncGate != null)
        {
            await NavigatedFromAsyncGate.Task;
        }
        sharedLog.Add($"{name}.{nameof(OnNavigatedFromAsync)}:end");
    }

    public async Task OnNavigatedToAsync(object? navigationParameter)
    {
        sharedLog.Add($"{name}.{nameof(OnNavigatedToAsync)}:start");
        if (NavigatedToAsyncGate != null)
        {
            await NavigatedToAsyncGate.Task;
        }
        sharedLog.Add($"{name}.{nameof(OnNavigatedToAsync)}:end");
    }
}
