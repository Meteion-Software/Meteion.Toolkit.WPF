using Meteion.Toolkit.WPF.SplashScreen;
using Microsoft.Extensions.Hosting;

namespace Meteion.Toolkit.WPF.SampleApp.Services;

/// <summary>
/// Pretends to be the slow startup work of a large application (opening databases, warming caches, ...) so the
/// splash screen has something to show. Hosted services start before the launch window appears, so the splash is
/// visible for the whole of <see cref="StartAsync"/>. Remove this from <c>Program.cs</c> in a real app.
/// </summary>
public sealed class SimulatedStartupService(IProgress<SplashProgress> splash) : IHostedService
{
    private static readonly (string Status, int DelayMs)[] Steps =
    [
        ("Reading configuration…", 500),
        ("Opening database…", 900),
        ("Warming caches…", 700),
        ("Loading plugins…", 800),
        ("Preparing user interface…", 600),
    ];

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        splash.Report(SplashProgress.Indeterminate("Starting…"));
        await Task.Delay(500, cancellationToken);

        for (var i = 0; i < Steps.Length; i++)
        {
            var (status, delay) = Steps[i];
            splash.Report(SplashProgress.Determinate(i / (double)Steps.Length, status));
            await Task.Delay(delay, cancellationToken);
        }

        splash.Report(SplashProgress.Determinate(1, "Ready"));
        await Task.Delay(300, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
