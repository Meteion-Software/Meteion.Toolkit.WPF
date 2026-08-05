
using Meteion.Toolkit.MVVM.Services;
using Meteion.Toolkit.WPF.Hosting;
using Meteion.Toolkit.WPF.MVVM;
using Meteion.Toolkit.WPF.SampleApp.Services;
using Meteion.Toolkit.WPF.SampleApp.ViewModels;
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

        var builder = new HostApplicationBuilder()
            .ConfigureLaunchWindow<MainWindow>() // Note: this window is not resolved using your chosen IWindowResolutionService.
            .ConfigureApplication<App>();

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

        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        var host = builder.BuildWpfHost();
        host.Run();
    }
}
