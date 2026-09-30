using Meteion.Toolkit.WPF.Hosting.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Meteion.Toolkit.WPF.Hosting.Tests;

/// <summary>
/// Only the startup-failure path is exercised: the success path ends in Application.Run, which blocks until the
/// app shuts down and can only exist once per process. The application type is registered with a factory that
/// throws, so no Application or Window is ever constructed.
/// </summary>
public class LifecycleHookTests
{
    private sealed class ThrowingApp : WpfGenericHostApplication
    {
        public override void PerformInitializeComponent() { }
    }

    private sealed class RecordingHook : IWpfHostLifecycleHook
    {
        public List<Exception> Failures { get; } = [];

        public void OnStartupFailed(Exception exception) => Failures.Add(exception);
    }

    private sealed class ThrowingHook : IWpfHostLifecycleHook
    {
        public void OnStartupFailed(Exception exception) => throw new ApplicationException("hook failure");
    }

    private static WpfApplicationHost BuildFailingHost(params IWpfHostLifecycleHook[] hooks)
    {
        var builder = new HostApplicationBuilder();
        builder.ConfigureLaunchWindow<DummyWindow>().ConfigureApplication<ThrowingApp>();
        builder.Services.AddSingleton<ThrowingApp>(_ => throw new InvalidOperationException("startup boom"));
        foreach (var hook in hooks)
        {
            builder.Services.AddSingleton(hook);
        }

        return builder.BuildWpfHost();
    }

    private static Exception? StartOnStaThread(WpfApplicationHost host)
    {
        Exception? thrown = null;
        var thread = new Thread(() =>
        {
            try
            {
                host.StartAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                thrown = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        return thrown;
    }

    [Fact]
    public void StartupFailure_NotifiesEveryHook_ThenRethrowsTheOriginal()
    {
        var first = new RecordingHook();
        var second = new RecordingHook();

        var thrown = StartOnStaThread(BuildFailingHost(first, second));

        Assert.IsType<InvalidOperationException>(thrown);
        Assert.Equal("startup boom", thrown.Message);
        Assert.Single(first.Failures);
        Assert.Same(thrown, first.Failures[0]);
        Assert.Single(second.Failures);
    }

    [Fact]
    public void ThrowingHook_NeverMasksTheOriginalException_OrStopsOtherHooks()
    {
        var recording = new RecordingHook();

        var thrown = StartOnStaThread(BuildFailingHost(new ThrowingHook(), recording));

        Assert.Equal("startup boom", thrown?.Message);
        Assert.Single(recording.Failures);
    }
}
