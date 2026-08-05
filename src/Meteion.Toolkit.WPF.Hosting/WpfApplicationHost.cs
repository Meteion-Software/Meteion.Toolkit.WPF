using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Meteion.Toolkit.WPF.Hosting
{
    public class WpfApplicationHost : IHost
    {
        private readonly IHost _baseHost;
        private readonly Type _applicationType;
        private readonly Type _startupWindowType;
        private readonly ILogger<WpfApplicationHost>? _logger;

        private WpfGenericHostApplication? _application;

        public IServiceProvider Services => _baseHost.Services;

        internal WpfApplicationHost(IHost baseHost, Type startupWindowType, Type applicationType, ILogger<WpfApplicationHost>? logger = null)
        {
            _baseHost = baseHost;
            _startupWindowType = startupWindowType;
            _applicationType = applicationType;
            _logger = logger;

            _logger ??= _baseHost.Services.GetService<ILogger<WpfApplicationHost>>();
        }

        public void Dispose()
        {
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        public async Task StartAsync(CancellationToken cancellationToken = default)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (System.Threading.Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new Exception("StartAsync thread is not STA, but many components require this.");
            }

            _application = (WpfGenericHostApplication)_baseHost.Services.GetRequiredService(_applicationType);
            _application.Host = this;
            _logger?.LogDebug("Calling app to perform initialize component.");
            _application.PerformInitializeComponent();
            var scope = _baseHost.Services.CreateScope();
            _application.MainWindow = (Window)scope.ServiceProvider.GetRequiredService(_startupWindowType);
            _application.ShutdownMode = ShutdownMode.OnLastWindowClose; // TODO: determine if this should be configurable
            _logger?.LogInformation("Calling BaseHost to start app.");
            // We need to call StartAsync synchronously here because the WPF application run will block the thread and we need to ensure that the base host is started before we run the application.
            _baseHost.StartAsync(cancellationToken).GetAwaiter().GetResult();
            _logger?.LogInformation("Calling application run.");
            _application.Run(_application.MainWindow); // this will hold the thread until the app is shut down
            _logger?.LogInformation("Application run completed. Shutting down.");
            // Now that the application has exited, we can call StopAsync on the base host to ensure that any hosted services are stopped gracefully.
            _baseHost.StopAsync(cancellationToken).GetAwaiter().GetResult();
            _logger?.LogInformation("Shutdown complete. Exiting StartAsync task.");
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            _application?.Shutdown();
            return Task.CompletedTask;
        }
    }
}
