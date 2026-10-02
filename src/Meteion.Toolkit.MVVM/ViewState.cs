using Meteion.Toolkit.WPF.Controls;
using System.ComponentModel;

namespace Meteion.Toolkit.MVVM;

/// <summary>
/// Observable loading/loaded/error state for a <see cref="StatefulContainer"/>, with a helper
/// (<see cref="RunAsync(Func{CancellationToken, Task}, string?)"/>) that moves through the states around a piece of work.
/// </summary>
/// <remarks>
/// Not thread-safe: create it on, and call it from, the UI thread so awaited continuations resume there.
/// A new run, <see cref="Cancel"/> or any of the manual setters cancels the run in flight, and a cancelled run
/// never changes the state afterwards, so a stale result cannot overwrite a newer one.
/// </remarks>
public sealed class ViewState : IViewState
{
    private static readonly PropertyChangedEventArgs StatusChanged = new(nameof(Status));
    private static readonly PropertyChangedEventArgs ErrorMessageChanged = new(nameof(ErrorMessage));
    private static readonly PropertyChangedEventArgs ExceptionChanged = new(nameof(Exception));

    private CancellationTokenSource? _currentRun;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the current state. Starts as <see cref="ViewStatus.Loading"/> so nothing flashes before the first load begins.</summary>
    public ViewStatus Status { get; private set; } = ViewStatus.Loading;

    /// <summary>Gets the user-facing error message while <see cref="Status"/> is <see cref="ViewStatus.Error"/>.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Gets the exception behind the current error, if one was supplied.</summary>
    public Exception? Exception { get; private set; }

    /// <summary>
    /// Runs <paramref name="work"/>, showing <see cref="ViewStatus.Loading"/> meanwhile, then <see cref="ViewStatus.Loaded"/>,
    /// or <see cref="ViewStatus.Error"/> if it throws. Work failures are never rethrown.
    /// </summary>
    /// <param name="work">The work to run; it should honour the token.</param>
    /// <param name="errorMessage">The message to show on failure; when <see langword="null"/>, the exception's message is used.</param>
    /// <returns>A task that completes when the work has finished and the state has been updated.</returns>
    public Task RunAsync(Func<CancellationToken, Task> work, string? errorMessage = null)
        => RunCoreAsync(work, errorMessage is null ? null : _ => errorMessage);

    /// <summary>
    /// Like <see cref="RunAsync(Func{CancellationToken, Task}, string?)"/>, but on failure calls
    /// <paramref name="onException"/> with the exception (to log it, for example) and shows the message it returns.
    /// </summary>
    /// <param name="work">The work to run; it should honour the token.</param>
    /// <param name="onException">Returns the user-facing message; returning <see langword="null"/> (or throwing) falls back to the exception's message.</param>
    /// <returns>A task that completes when the work has finished and the state has been updated.</returns>
    public Task RunAsync(Func<CancellationToken, Task> work, Func<Exception, string?> onException)
    {
        ArgumentNullException.ThrowIfNull(onException);
        return RunCoreAsync(work, onException);
    }

    /// <summary>
    /// Cancels the run in flight, if any. The state is left as it is; set it explicitly or start another run.
    /// </summary>
    public void Cancel() => CancelCurrentRun();

    /// <summary>Cancels any run in flight and shows the loading state.</summary>
    public void SetLoading()
    {
        CancelCurrentRun();
        Apply(ViewStatus.Loading, null, null);
    }

    /// <summary>Cancels any run in flight and shows the loaded content.</summary>
    public void SetLoaded()
    {
        CancelCurrentRun();
        Apply(ViewStatus.Loaded, null, null);
    }

    /// <summary>Cancels any run in flight and shows the error state.</summary>
    /// <param name="message">The message to show; when <see langword="null"/>, <paramref name="exception"/>'s message is used.</param>
    /// <param name="exception">The exception behind the error, exposed to custom error templates.</param>
    public void SetError(string? message, Exception? exception = null)
    {
        CancelCurrentRun();
        Apply(ViewStatus.Error, message ?? exception?.Message, exception);
    }

    private async Task RunCoreAsync(Func<CancellationToken, Task> work, Func<Exception, string?>? onException)
    {
        ArgumentNullException.ThrowIfNull(work);

        CancelCurrentRun();
        var run = new CancellationTokenSource();
        _currentRun = run;
        Apply(ViewStatus.Loading, null, null);

        try
        {
            await work(run.Token);

            if (!run.IsCancellationRequested)
            {
                Apply(ViewStatus.Loaded, null, null);
            }
        }
        catch (Exception ex)
        {
            // A superseded or cancelled run is stale whatever it threw, so it must not touch the state.
            if (!run.IsCancellationRequested)
            {
                Apply(ViewStatus.Error, ResolveMessage(ex, onException), ex);
            }
        }
        finally
        {
            if (ReferenceEquals(_currentRun, run))
            {
                _currentRun = null;
            }

            run.Dispose();
        }
    }

    private static string? ResolveMessage(Exception ex, Func<Exception, string?>? onException)
    {
        if (onException is null)
        {
            return ex.Message;
        }

        try
        {
            return onException(ex) ?? ex.Message;
        }
        catch
        {
            // A faulty handler must not turn a handled failure into an unhandled one.
            return ex.Message;
        }
    }

    private void CancelCurrentRun()
    {
        // Disposal is left to the run itself (in its finally) so it never uses a disposed source.
        _currentRun?.Cancel();
        _currentRun = null;
    }

    private void Apply(ViewStatus status, string? errorMessage, Exception? exception)
    {
        var statusChanged = Status != status;
        var messageChanged = ErrorMessage != errorMessage;
        var exceptionChanged = !ReferenceEquals(Exception, exception);

        Status = status;
        ErrorMessage = errorMessage;
        Exception = exception;

        // All fields are set before any event fires, so a handler always sees a consistent state.
        if (messageChanged)
        {
            PropertyChanged?.Invoke(this, ErrorMessageChanged);
        }

        if (exceptionChanged)
        {
            PropertyChanged?.Invoke(this, ExceptionChanged);
        }

        if (statusChanged)
        {
            PropertyChanged?.Invoke(this, StatusChanged);
        }
    }
}
