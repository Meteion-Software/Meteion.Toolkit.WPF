using Meteion.Toolkit.WPF.MVVM.Services;
using Meteion.Toolkit.WPF.MVVM.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Meteion.Toolkit.WPF.MVVM.Tests.Services;

public class WindowResolutionServiceTests
{
    private static WindowResolutionService CreateService(IServiceProvider? provider = null)
        => new(provider ?? new ServiceCollection().BuildServiceProvider());

    [Fact]
    public void AddWindow_DuplicateViewModelKey_Throws()
    {
        var service = CreateService();
        service.AddWindow<FakeViewModelA, FakeWindowA>();

        Assert.Throws<ArgumentException>(() => service.AddWindow<FakeViewModelA, FakeWindowB>());
    }

    [Fact]
    public void AddWindow_DuplicateViewType_Throws()
    {
        var service = CreateService();
        service.AddWindow<FakeViewModelA, FakeWindowA>();

        Assert.Throws<ArgumentException>(() => service.AddWindow<FakeViewModelB, FakeWindowA>());
    }

    [Fact]
    public void GetNewScopedWindowInstance_UnknownViewModel_Throws()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() => service.GetNewScopedWindowInstance(typeof(FakeViewModelA)));
    }

    [StaFact]
    public void GetNewScopedWindowInstance_ResolvesRealWindowFromANewScope()
    {
        var services = new ServiceCollection();
        services.AddTransient<FakeWindowA>();
        var service = CreateService(services.BuildServiceProvider());
        service.AddWindow<FakeViewModelA, FakeWindowA>();

        var window = service.GetNewScopedWindowInstance(typeof(FakeViewModelA));

        Assert.IsType<FakeWindowA>(window);
    }

    [StaFact]
    public void GetNewScopedWindowInstance_UsesOwnScope_DisposedWhenWindowCloses()
    {
        // This is the test the original stub ("Test_WindowCreation_UsesOwnScope") named
        // but never implemented — WindowResolutionService.GetNewScopedWindowInstance is
        // documented to create a scope and dispose it when the window closes.
        var services = new ServiceCollection();
        services.AddScoped<DisposableTracker>();
        services.AddTransient<FakeWindowWithScopedDependency>();
        var service = CreateService(services.BuildServiceProvider());
        service.AddWindow<FakeViewModelA, FakeWindowWithScopedDependency>();

        var window = (FakeWindowWithScopedDependency)service.GetNewScopedWindowInstance(typeof(FakeViewModelA));
        Assert.False(window.Tracker.IsDisposed);

        window.Close();

        Assert.True(window.Tracker.IsDisposed);
    }

    [StaFact]
    public void GetNewScopedWindowInstance_CalledTwice_EachGetsItsOwnScope()
    {
        var services = new ServiceCollection();
        services.AddScoped<DisposableTracker>();
        services.AddTransient<FakeWindowWithScopedDependency>();
        var service = CreateService(services.BuildServiceProvider());
        service.AddWindow<FakeViewModelA, FakeWindowWithScopedDependency>();

        var first = (FakeWindowWithScopedDependency)service.GetNewScopedWindowInstance(typeof(FakeViewModelA));
        var second = (FakeWindowWithScopedDependency)service.GetNewScopedWindowInstance(typeof(FakeViewModelA));

        Assert.NotSame(first.Tracker, second.Tracker);

        first.Close();

        Assert.True(first.Tracker.IsDisposed);
        Assert.False(second.Tracker.IsDisposed);
    }
}
