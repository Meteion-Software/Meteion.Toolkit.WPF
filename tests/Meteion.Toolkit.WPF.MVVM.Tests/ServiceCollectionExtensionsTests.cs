using Meteion.Toolkit.WPF.MVVM;
using Meteion.Toolkit.WPF.MVVM.Tests.Fixtures;
using Meteion.Toolkit.WPF.MVVM.Tests.Fixtures.NamingConvention;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace Meteion.Toolkit.WPF.MVVM.Tests;

public class ServiceCollectionExtensionsTests
{
    private static readonly Regex NamingConventionNamespace = new(@"\.NamingConvention$");

    [Fact]
    public void AddViewModelsFoundIn_RegistersMatchingViewModels_AsScopedByDefault()
    {
        var services = new ServiceCollection();

        services.AddViewModelsFoundIn(typeof(FirstMatchPageViewModel).Assembly, NamingConventionNamespace);

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(FirstMatchPageViewModel));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(typeof(FirstMatchPageViewModel), descriptor.ImplementationType);
    }

    [Fact]
    public void AddViewModelsFoundIn_UsesLifetimeFromViewModelOptions()
    {
        var services = new ServiceCollection();

        services.AddViewModelsFoundIn(typeof(SingletonOptionsPageViewModel).Assembly, NamingConventionNamespace);

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(SingletonOptionsPageViewModel));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddViewModelsFoundIn_SkipsNamespacesThatDoNotMatch()
    {
        var services = new ServiceCollection();

        services.AddViewModelsFoundIn(typeof(FirstMatchPageViewModel).Assembly, new Regex(@"^Nothing\.Matches$"));

        Assert.Empty(services);
    }

    [Fact]
    public void AddViewModelsFoundIn_RequiresViewModelSuffixByDefault()
    {
        var services = new ServiceCollection();

        services.AddViewModelsFoundIn(typeof(FakeViewModelA).Assembly, new Regex(@"\.Tests\.Fixtures$"));

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(FakeViewModelA));
        Assert.Contains(services, d => d.ServiceType == typeof(FakeNavigationAwareViewModel));
    }

    [Fact]
    public void AddViewModelsFoundIn_MustEndInViewModelFalse_RegistersAnyViewModelType()
    {
        var services = new ServiceCollection();

        services.AddViewModelsFoundIn(typeof(FakeViewModelA).Assembly, new Regex(@"\.Tests\.Fixtures$"), mustEndInViewModel: false);

        Assert.Contains(services, d => d.ServiceType == typeof(FakeViewModelA));
    }

    [Fact]
    public void AddViewModelsFoundIn_DoesNotOverrideExistingRegistration()
    {
        var services = new ServiceCollection();
        services.AddTransient<FirstMatchPageViewModel>();

        services.AddViewModelsFoundIn(typeof(FirstMatchPageViewModel).Assembly, NamingConventionNamespace);

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(FirstMatchPageViewModel));
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void UseDefaultPageResolutionService_DoesNotOverrideExistingRegistration()
    {
        var services = new ServiceCollection();
        services.AddTransient<FakeViewModelA>();

        services.UseDefaultPageResolutionService(b => b.Add<FakeViewModelA, FakePageA>(ServiceLifetime.Singleton));

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(FakeViewModelA));
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }
}
