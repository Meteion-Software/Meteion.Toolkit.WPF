
using Meteion.Toolkit.MVVM.Services;
using Meteion.Toolkit.WPF.Hosting;
using Meteion.Toolkit.WPF.Localization;
using Meteion.Toolkit.WPF.MVVM;
using Meteion.Toolkit.WPF.SampleApp.Services;
using Meteion.Toolkit.WPF.SampleApp.ViewModels;
using Meteion.Toolkit.WPF.SplashScreen;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace Meteion.Toolkit.WPF.SampleApp;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
        {
            throw new Exception("Main application thread is not STA, but many components require this.");
        }

        // Show the splash before anything else so it appears as early as possible; the host closes it once MainWindow has rendered.
        var splash = new SplashScreenBuilder()
            .UseImageFromEmbeddedResource("Splash.png")
            .Configure(o =>
            {
                o.ShowProgressBar = true;
                o.ShowStatusText = true;
                o.ProgressBarCornerRadius = 3;
                o.FadeIn = true;
                o.MinimumDisplayTime = TimeSpan.FromSeconds(1);
            })
            .Show();

        var builder = new HostApplicationBuilder()
            .ConfigureLaunchWindow<MainWindow>() // Note: this window is not resolved using your chosen IWindowResolutionService.
            .ConfigureApplication<App>()
            .UseSplashScreen(splash);

        // Fake slow startup work that reports to the splash. Remove in a real app.
        builder.Services.AddHostedService<SimulatedStartupService>();

        // Navigation will scope to the window.
        // Note: ensure you use a scoped serviceprovider when creating a window instance outside of the IWindowResolutionService!
        builder.Services.AddScoped<INavigationService, NavigationService>();
        builder.Services.UseDefaultPageResolutionService(builder =>
        {
            // We can scan from the assembly, but this is slow and uses reflection.
            builder.AddFromAssembly(typeof(Program).Assembly);
        });

        // Allow us to track our current scope id for debugging purposes.
        builder.Services.AddScoped<IScopeIdService, ScopeIdService>();

        builder.Services.UseDefaultWindowResolutionService(builder =>
        {
            // We can also manually add our window <> viewmodel mappings here.
            builder.Add<MainWindowViewModel, MainWindow>();
        });

        builder.Services.AddWpfLocalization(conf =>
        {
            conf.MissingKeyBehavior = Toolkit.Localization.Abstractions.MissingResourceBehavior.ThrowException;
            conf.DefaultAssembly = typeof(Program).Assembly;
            conf.DefaultCulture = new System.Globalization.CultureInfo("en-CA");
        });

        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        var host = builder.BuildWpfHost();
        host.Run();
    }
}
