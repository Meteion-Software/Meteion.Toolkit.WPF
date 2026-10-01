using Meteion.Toolkit.MVVM;
using Meteion.Toolkit.WPF.Controls;

namespace Meteion.Toolkit.WPF.MVVM.Tests;

public class ViewStateTests
{
    private static ViewState Create(out List<string?> changes)
    {
        var state = new ViewState();
        var list = new List<string?>();
        state.PropertyChanged += (_, e) => list.Add(e.PropertyName);
        changes = list;
        return state;
    }

    [Fact]
    public void InitialState_IsLoadingWithNoError()
    {
        var state = new ViewState();

        Assert.Equal(ViewStatus.Loading, state.Status);
        Assert.Null(state.ErrorMessage);
        Assert.Null(state.Exception);
    }

    [Fact]
    public async Task RunAsync_Success_EndsLoaded()
    {
        var state = new ViewState();

        await state.RunAsync(_ => Task.CompletedTask);

        Assert.Equal(ViewStatus.Loaded, state.Status);
    }

    [Fact]
    public async Task RunAsync_ShowsLoadingWhileWorkRuns()
    {
        var state = new ViewState();
        state.SetLoaded();
        var gate = new TaskCompletionSource();

        var run = state.RunAsync(_ => gate.Task);
        Assert.Equal(ViewStatus.Loading, state.Status);

        gate.SetResult();
        await run;
        Assert.Equal(ViewStatus.Loaded, state.Status);
    }

    [Fact]
    public async Task RunAsync_WorkThrows_ErrorUsesExceptionMessageAndExposesException()
    {
        var state = new ViewState();
        var boom = new InvalidOperationException("boom");

        await state.RunAsync(_ => throw boom);

        Assert.Equal(ViewStatus.Error, state.Status);
        Assert.Equal("boom", state.ErrorMessage);
        Assert.Same(boom, state.Exception);
    }

    [Fact]
    public async Task RunAsync_WorkThrowsAfterAwait_IsCaught()
    {
        var state = new ViewState();

        await state.RunAsync(async _ =>
        {
            await Task.Yield();
            throw new InvalidOperationException("later");
        });

        Assert.Equal(ViewStatus.Error, state.Status);
        Assert.Equal("later", state.ErrorMessage);
    }

    [Fact]
    public async Task RunAsync_ErrorMessageOverload_UsesGivenMessage()
    {
        var state = new ViewState();

        await state.RunAsync(_ => throw new InvalidOperationException("boom"), "Couldn't load orders.");

        Assert.Equal("Couldn't load orders.", state.ErrorMessage);
    }

    [Fact]
    public async Task RunAsync_OnException_ReceivesExceptionAndSuppliesMessage()
    {
        var state = new ViewState();
        var boom = new InvalidOperationException("boom");
        Exception? seen = null;

        await state.RunAsync(_ => throw boom, ex =>
        {
            seen = ex;
            return "friendly";
        });

        Assert.Same(boom, seen);
        Assert.Equal("friendly", state.ErrorMessage);
        Assert.Same(boom, state.Exception);
    }

    [Fact]
    public async Task RunAsync_OnExceptionReturnsNull_FallsBackToExceptionMessage()
    {
        var state = new ViewState();

        await state.RunAsync(_ => throw new InvalidOperationException("boom"), _ => null);

        Assert.Equal("boom", state.ErrorMessage);
    }

    [Fact]
    public async Task RunAsync_OnExceptionThrows_FallsBackToExceptionMessage()
    {
        var state = new ViewState();

        await state.RunAsync(_ => throw new InvalidOperationException("boom"), _ => throw new ArgumentException("handler"));

        Assert.Equal(ViewStatus.Error, state.Status);
        Assert.Equal("boom", state.ErrorMessage);
    }

    [Fact]
    public async Task RunAsync_AfterError_SuccessClearsErrorAndException()
    {
        var state = new ViewState();
        await state.RunAsync(_ => throw new InvalidOperationException("boom"));

        await state.RunAsync(_ => Task.CompletedTask);

        Assert.Equal(ViewStatus.Loaded, state.Status);
        Assert.Null(state.ErrorMessage);
        Assert.Null(state.Exception);
    }

    [Fact]
    public async Task RunAsync_NewRunCancelsPreviousAndStaleResultIsIgnored()
    {
        var state = new ViewState();
        var firstToken = default(CancellationToken);
        var firstGate = new TaskCompletionSource();
        var first = state.RunAsync(async ct =>
        {
            firstToken = ct;
            await firstGate.Task;
            throw new InvalidOperationException("stale failure");
        });

        var second = state.RunAsync(_ => Task.CompletedTask);

        Assert.True(firstToken.IsCancellationRequested);
        Assert.Equal(ViewStatus.Loaded, state.Status);

        firstGate.SetResult();
        await first;
        await second;

        Assert.Equal(ViewStatus.Loaded, state.Status);
        Assert.Null(state.ErrorMessage);
    }

    [Fact]
    public async Task RunAsync_StaleSuccessDoesNotOverwriteNewerError()
    {
        var state = new ViewState();
        var firstGate = new TaskCompletionSource();
        var first = state.RunAsync(_ => firstGate.Task);

        await state.RunAsync(_ => throw new InvalidOperationException("newer"));
        firstGate.SetResult();
        await first;

        Assert.Equal(ViewStatus.Error, state.Status);
        Assert.Equal("newer", state.ErrorMessage);
    }

    [Fact]
    public async Task Cancel_CancelsInFlightRun_WithoutChangingStateOrReportingError()
    {
        var state = new ViewState();
        var run = state.RunAsync(ct => Task.Delay(Timeout.Infinite, ct));

        state.Cancel();
        await run;

        Assert.Equal(ViewStatus.Loading, state.Status);
        Assert.Null(state.ErrorMessage);
    }

    [Fact]
    public async Task SetLoaded_CancelsInFlightRun()
    {
        var state = new ViewState();
        var run = state.RunAsync(ct => Task.Delay(Timeout.Infinite, ct));

        state.SetLoaded();
        await run;

        Assert.Equal(ViewStatus.Loaded, state.Status);
    }

    [Fact]
    public void SetError_NullMessage_UsesExceptionMessage()
    {
        var state = new ViewState();
        var ex = new InvalidOperationException("boom");

        state.SetError(null, ex);

        Assert.Equal(ViewStatus.Error, state.Status);
        Assert.Equal("boom", state.ErrorMessage);
        Assert.Same(ex, state.Exception);
    }

    [Fact]
    public void SetLoading_AfterError_ClearsErrorDetails()
    {
        var state = new ViewState();
        state.SetError("bad", new Exception("x"));

        state.SetLoading();

        Assert.Equal(ViewStatus.Loading, state.Status);
        Assert.Null(state.ErrorMessage);
        Assert.Null(state.Exception);
    }

    [Fact]
    public void PropertyChanged_RaisedOnlyForPropertiesThatChanged()
    {
        var state = Create(out var changes);

        state.SetLoading(); // already Loading with no error: nothing changes
        Assert.Empty(changes);

        state.SetLoaded();
        Assert.Equal([nameof(ViewState.Status)], changes);

        changes.Clear();
        state.SetError("bad");
        Assert.Contains(nameof(ViewState.Status), changes);
        Assert.Contains(nameof(ViewState.ErrorMessage), changes);
        Assert.DoesNotContain(nameof(ViewState.Exception), changes);
    }

    [Fact]
    public void PropertyChanged_StatusEventIsRaisedLast_SoHandlersSeeConsistentState()
    {
        var state = new ViewState();
        state.SetLoaded();
        string? messageWhenStatusRaised = null;
        state.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ViewState.Status))
            {
                messageWhenStatusRaised = state.ErrorMessage;
            }
        };

        state.SetError("bad");

        Assert.Equal("bad", messageWhenStatusRaised);
    }

    [Fact]
    public async Task RunAsync_NullWork_Throws()
    {
        var state = new ViewState();

        await Assert.ThrowsAsync<ArgumentNullException>(() => state.RunAsync(null!));
    }
}
