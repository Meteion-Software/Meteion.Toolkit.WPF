using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace Meteion.Toolkit.WPF.SplashScreen.Tests;

/// <summary>
/// These create a real layered window on a real splash thread, so they need an interactive window station
/// (true on developer machines and GitHub's windows runners).
/// </summary>
public class SplashScreenLifecycleTests
{
    private static readonly Assembly TestAssembly = typeof(SplashScreenLifecycleTests).Assembly;
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    private static SplashScreenBuilder Builder(Action<SplashScreenOptions>? configure = null)
    {
        var builder = new SplashScreenBuilder().UseImageFromEmbeddedResource("Unique.png", TestAssembly);
        return configure is null ? builder : builder.Configure(configure);
    }

    [Fact]
    public async Task ShowReportClose_CompletesWithoutFailing()
    {
        var splash = Builder(o => { o.ShowProgressBar = true; o.ShowStatusText = true; o.FadeInDuration = TimeSpan.FromMilliseconds(30); o.FadeIn = true; })
            .Show();

        splash.SetProgress(0.3);
        splash.Report(0.6, "Working…");
        splash.SetIndeterminate();
        splash.Report(SplashProgress.Status(""));
        await Task.Delay(100);

        await splash.Close().WaitAsync(Timeout);

        Assert.False(((SplashScreenHandle)splash).IsFailed);
    }

    [Fact]
    public async Task Close_TwiceReturnsTheSameTask()
    {
        var splash = Builder().Show();

        var first = splash.Close();
        var second = splash.Close();

        Assert.Same(first, second);
        await first.WaitAsync(Timeout);
    }

    [Fact]
    public async Task Dispose_SkipsMinimumDisplayTimeAndFade()
    {
        var splash = (SplashScreenHandle)Builder(o => o.MinimumDisplayTime = TimeSpan.FromSeconds(30)).Show();
        await Task.Delay(100);

        var stopwatch = Stopwatch.StartNew();
        splash.Dispose();
        await splash.Exited.WaitAsync(Timeout);

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Dispose took {stopwatch.Elapsed}");
        Assert.False(splash.IsFailed);
    }

    [Fact]
    public async Task Dispose_ImmediatelyAfterShow_TearsDownCleanly()
    {
        var splash = (SplashScreenHandle)Builder().Show();

        splash.Dispose();

        await splash.Exited.WaitAsync(Timeout);
        Assert.False(splash.IsFailed);
    }

    [Fact]
    public async Task Close_HonorsMinimumDisplayTime()
    {
        var splash = Builder(o => { o.MinimumDisplayTime = TimeSpan.FromMilliseconds(700); o.FadeOut = false; }).Show();
        var stopwatch = Stopwatch.StartNew();

        await splash.Close().WaitAsync(Timeout);

        Assert.True(stopwatch.ElapsedMilliseconds >= 600, $"closed after only {stopwatch.ElapsedMilliseconds} ms");
    }

    [Fact]
    public async Task ReportsAfterClose_AreIgnored()
    {
        var splash = Builder(o => o.ShowProgressBar = true).Show();
        var closing = splash.Close();

        splash.SetProgress(1);
        splash.SetStatus("late");
        splash.Report(1, "late");

        await closing.WaitAsync(Timeout);
        splash.Dispose(); // safe after Close()
    }

    [Fact]
    public async Task BadImage_FailsQuietly_AndBecomesANoOp()
    {
        var path = Path.Combine(Path.GetTempPath(), $"not-a-png-{Guid.NewGuid():N}.png");
        await File.WriteAllTextAsync(path, "this is not a png");
        try
        {
            var logged = new List<string>();
            var splash = (SplashScreenHandle)new SplashScreenBuilder()
                .UseImageFromFilesystem(path)
                .UseLogger(new ListLogger(logged))
                .Show();

            await splash.Exited.WaitAsync(Timeout);

            Assert.True(splash.IsFailed);
            Assert.Contains(logged, m => m.Contains("failed"));
            splash.SetProgress(0.5); // ignored
            Assert.True(splash.Close().IsCompleted);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FailureBeforeALoggerExists_IsBufferedThenFlushedWhenOneIsAttached()
    {
        var log = new SplashLog();
        log.Error("boom", new InvalidOperationException("x"));

        var logged = new List<string>();
        log.Attach(new ListLogger(logged));

        Assert.Contains(logged, m => m == "boom");
    }
}
