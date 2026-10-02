using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace Meteion.Toolkit.WPF.Hosting
{
    /// <summary>
    /// An <see cref="IHost"/> that runs a <see cref="WpfGenericHostApplication"/> on the calling STA thread.
    /// <see cref="StartAsync"/> blocks until the WPF application exits, then stops the wrapped host.
    /// </summary>
    public class WpfApplicationHost : IHost
    {
        private readonly IHost _baseHost;
        private readonly Type _applicationType;
        private readonly Type _startupWindowType;
        private readonly ILogger<WpfApplicationHost>? _logger;

        private WpfGenericHostApplication? _application;

        /// <inheritdoc />
        public IServiceProvider Services => _baseHost.Services;

        /// <summary>
        /// Creates a host that wraps an already built base host. Instances are created by <c>BuildWpfHost</c>.
        /// </summary>
        /// <param name="baseHost">The built generic host that provides services and hosted-service lifetime.</param>
        /// <param name="startupWindowType">The window type resolved and shown as the main window.</param>
        /// <param name="applicationType">The <see cref="WpfGenericHostApplication"/> type to run.</param>
        /// <param name="logger">Optional logger; falls back to one resolved from the base host's services.</param>
        internal WpfApplicationHost(IHost baseHost, Type startupWindowType, Type applicationType, ILogger<WpfApplicationHost>? logger = null)
        {
            _baseHost = baseHost;
            _startupWindowType = startupWindowType;
            _applicationType = applicationType;
            _logger = logger;

            _logger ??= _baseHost.Services.GetService<ILogger<WpfApplicationHost>>();
        }

        /// <inheritdoc />
        public void Dispose()
        {
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// Starts the base host, shows the launch window and runs the WPF application. Despite returning a task, this
        /// method completes synchronously and does not return until the application has shut down.
        /// </summary>
        /// <param name="cancellationToken">Token passed to the base host's start and stop calls.</param>
        /// <returns>A task that completes once the application has exited and the base host has stopped.</returns>
        /// <exception cref="Exception">The calling thread is not an STA thread.</exception>
        public async Task StartAsync(CancellationToken cancellationToken = default)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (System.Threading.Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            {
                throw new Exception("StartAsync thread is not STA, but many components require this.");
            }

            var hooks = _baseHost.Services.GetServices<IWpfHostLifecycleHook>().ToArray();

            try
            {
                _application = (WpfGenericHostApplication)_baseHost.Services.GetRequiredService(_applicationType);
                _application.Host = this;
                _logger?.LogDebug("Calling app to perform initialize component.");
                _application.PerformInitializeComponent();
                var scope = _baseHost.Services.CreateScope();
                _application.MainWindow = (Window)scope.ServiceProvider.GetRequiredService(_startupWindowType);
                _application.ShutdownMode = ShutdownMode.OnLastWindowClose; // TODO: determine if this should be configurable
                _logger?.LogInformation("Calling BaseHost to start app.");
                // Start synchronously: Application.Run blocks this thread, so the base host must be fully started first.
                _baseHost.StartAsync(cancellationToken).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                NotifyHooks(hooks, hook => hook.OnStartupFailed(ex), nameof(IWpfHostLifecycleHook.OnStartupFailed));
                throw;
            }

            var launchWindow = _application.MainWindow;
            NotifyHooks(hooks, hook => hook.OnLaunchWindowCreated(launchWindow), nameof(IWpfHostLifecycleHook.OnLaunchWindowCreated));
            _logger?.LogInformation("Calling application run.");
            _application.Run(_application.MainWindow); // this will hold the thread until the app is shut down
            _logger?.LogInformation("Application run completed. Shutting down.");
            // The application has exited, so stop the base host to let hosted services shut down gracefully.
            _baseHost.StopAsync(cancellationToken).GetAwaiter().GetResult();
            _logger?.LogInformation("Shutdown complete. Exiting StartAsync task.");
        }

        /// <summary>
        /// Invokes <paramref name="action"/> on every hook. A hook that throws is logged and skipped, so one faulty
        /// hook can neither mask a startup exception nor stop the others from running.
        /// </summary>
        private void NotifyHooks(IWpfHostLifecycleHook[] hooks, Action<IWpfHostLifecycleHook> action, string hookName)
        {
            foreach (var hook in hooks)
            {
                try
                {
                    action(hook);
                }
                catch (Exception hookException)
                {
                    _logger?.LogError(hookException, "{Hook}.{Method} threw an exception.", hook.GetType().Name, hookName);
                }
            }
        }

        /// <summary>
        /// Requests shutdown of the running WPF application. The base host is stopped by <see cref="StartAsync"/>
        /// once the application has exited.
        /// </summary>
        /// <param name="cancellationToken">Unused.</param>
        /// <returns>A completed task.</returns>
        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            _application?.Shutdown();
            return Task.CompletedTask;
        }
    }
}
