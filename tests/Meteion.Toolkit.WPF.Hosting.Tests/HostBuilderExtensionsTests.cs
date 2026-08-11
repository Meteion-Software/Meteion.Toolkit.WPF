using Meteion.Toolkit.WPF.Hosting.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Meteion.Toolkit.WPF.Hosting.Tests;

/// <summary>
/// None of these construct DummyWindow/DummyApp — WpfApplicationHost's constructor
/// only stores the types and resolves them later, inside StartAsync (never called
/// here), so nothing here needs an STA thread or touches System.Windows.Application's
/// one-instance-per-process semantics.
/// </summary>
public class HostBuilderExtensionsTests
{
    public class HostApplicationBuilderApi
    {
        [Fact]
        public void ConfigureLaunchWindow_RegistersStartupWindowTypeAndScopedWindow()
        {
            var builder = new HostApplicationBuilder();

            builder.ConfigureLaunchWindow<DummyWindow>();

            Assert.Contains(builder.Services, sd => sd.ServiceType == typeof(DummyWindow) && sd.Lifetime == ServiceLifetime.Scoped);
        }

        [Fact]
        public void ConfigureApplication_RegistersApplicationTypeAndSingletonApp()
        {
            var builder = new HostApplicationBuilder();

            builder.ConfigureApplication<DummyApp>();

            Assert.Contains(builder.Services, sd => sd.ServiceType == typeof(DummyApp) && sd.Lifetime == ServiceLifetime.Singleton);
        }

        [Fact]
        public void BuildWpfHost_NeitherConfigured_ThrowsHelpfulMessageNotARawDiException()
        {
            var builder = new HostApplicationBuilder();

            var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildWpfHost());
            Assert.Contains("ConfigureLaunchWindow", ex.Message);
        }

        [Fact]
        public void BuildWpfHost_OnlyApplicationConfigured_ThrowsAboutMissingLaunchWindow()
        {
            var builder = new HostApplicationBuilder();
            builder.ConfigureApplication<DummyApp>();

            var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildWpfHost());
            Assert.Contains("ConfigureLaunchWindow", ex.Message);
        }

        [Fact]
        public void BuildWpfHost_OnlyLaunchWindowConfigured_ThrowsAboutMissingApplication()
        {
            var builder = new HostApplicationBuilder();
            builder.ConfigureLaunchWindow<DummyWindow>();

            var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildWpfHost());
            Assert.Contains("ConfigureApplication", ex.Message);
        }

        [Fact]
        public void BuildWpfHost_BothConfigured_ReturnsHostWithWorkingServices()
        {
            // Deliberately doesn't resolve DummyApp/DummyWindow here — constructing a
            // second System.Windows.Application in this process throws ("Cannot create
            // more than one Application instance"), and only one test per process gets
            // to do that safely. Checking Services is non-null is enough to prove
            // BuildWpfHost actually returned a working host on the success path.
            var builder = new HostApplicationBuilder();
            builder.ConfigureLaunchWindow<DummyWindow>();
            builder.ConfigureApplication<DummyApp>();

            using var host = builder.BuildWpfHost();

            Assert.NotNull(host.Services);
        }
    }

    public class LegacyHostBuilderApi
    {
        [Fact]
        public void ConfigureLaunchWindow_RegistersStartupWindowTypeAndScopedWindow()
        {
            // Checks DI registration metadata rather than resolving DummyWindow —
            // constructing a real Window requires an STA thread, which a plain [Fact]
            // doesn't run on.
            IServiceCollection? capturedServices = null;
            var builder = Host.CreateDefaultBuilder()
                .ConfigureServices((_, services) => capturedServices = services);

            builder.ConfigureLaunchWindow<DummyWindow>();
            using var host = builder.Build();

            Assert.NotNull(capturedServices);
            Assert.Contains(capturedServices, sd => sd.ServiceType == typeof(DummyWindow) && sd.Lifetime == ServiceLifetime.Scoped);
        }

        [Fact]
        public void BuildWpfHost_NeitherConfigured_ThrowsHelpfulMessageNotARawDiException()
        {
            var builder = Host.CreateDefaultBuilder();

            var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildWpfHost());
            Assert.Contains("ConfigureLaunchWindow", ex.Message);
        }

        [Fact]
        public void BuildWpfHost_OnlyApplicationConfigured_ThrowsAboutMissingLaunchWindow()
        {
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureApplication<DummyApp>();

            var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildWpfHost());
            Assert.Contains("ConfigureLaunchWindow", ex.Message);
        }

        [Fact]
        public void BuildWpfHost_OnlyLaunchWindowConfigured_ThrowsAboutMissingApplication()
        {
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureLaunchWindow<DummyWindow>();

            var ex = Assert.Throws<InvalidOperationException>(() => builder.BuildWpfHost());
            Assert.Contains("ConfigureApplication", ex.Message);
        }

        [Fact]
        public void BuildWpfHost_BothConfigured_ReturnsHostWithWorkingServices()
        {
            var builder = Host.CreateDefaultBuilder();
            builder.ConfigureLaunchWindow<DummyWindow>();
            builder.ConfigureApplication<DummyApp>();

            using var host = builder.BuildWpfHost();

            Assert.NotNull(host.Services);
        }
    }
}
