using Meteion.Toolkit.MVVM.Services;
using Meteion.Toolkit.WPF.MVVM.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Meteion.Toolkit.WPF.MVVM.Tests.Services;

public class PageResolutionServiceTests
{
    private static PageResolutionService CreateService(IServiceProvider? provider = null)
        => new(provider ?? new ServiceCollection().BuildServiceProvider());

    [Fact]
    public void AddPage_DuplicateViewModelKey_Throws()
    {
        var service = CreateService();
        service.AddPage<FakeViewModelA, FakePageA>();

        Assert.Throws<ArgumentException>(() => service.AddPage<FakeViewModelA, FakePageB>());
    }

    [Fact]
    public void AddPage_DuplicateViewType_Throws()
    {
        var service = CreateService();
        service.AddPage<FakeViewModelA, FakePageA>();

        Assert.Throws<ArgumentException>(() => service.AddPage<FakeViewModelB, FakePageA>());
    }

    [Fact]
    public void GetPageFor_UnknownViewModel_Throws()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() => service.GetPageFor(typeof(FakeViewModelA)));
    }

    [Fact]
    public void GetPageFor_KnownViewModel_ReturnsRegisteredPageType()
    {
        var service = CreateService();
        service.AddPage<FakeViewModelA, FakePageA>();

        Assert.Equal(typeof(FakePageA), service.GetPageFor(typeof(FakeViewModelA)));
    }

    [StaFact]
    public void GetPageInstance_RegisteredInDi_ResolvesRealInstance()
    {
        var services = new ServiceCollection();
        services.AddTransient<FakePageA>();
        var service = CreateService(services.BuildServiceProvider());
        service.AddPage<FakeViewModelA, FakePageA>();

        var page = service.GetPageInstance(typeof(FakeViewModelA));

        Assert.IsType<FakePageA>(page);
    }

    [Fact]
    public void GetPageInstance_NotRegisteredInDi_ThrowsWithHelpfulMessage()
    {
        var service = CreateService(); // empty container — FakePageA never registered
        service.AddPage<FakeViewModelA, FakePageA>();

        var ex = Assert.Throws<Exception>(() => service.GetPageInstance(typeof(FakeViewModelA)));
        Assert.Contains("Could not create instance", ex.Message);
    }

    [Fact]
    public void GetViewModelInstance_RegisteredInDi_ResolvesRealInstance()
    {
        var services = new ServiceCollection();
        services.AddTransient<FakeViewModelA>();
        var service = CreateService(services.BuildServiceProvider());
        service.AddPage<FakeViewModelA, FakePageA>();

        var vm = service.GetViewModelInstance(typeof(FakeViewModelA));

        Assert.IsType<FakeViewModelA>(vm);
    }

    [Fact]
    public void GetViewModelInstance_UnknownViewModel_Throws()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() => service.GetViewModelInstance(typeof(FakeViewModelA)));
    }

    [Fact]
    public void GetViewModelInstance_KnownButNotRegisteredInDi_ThrowsWithHelpfulMessage()
    {
        var service = CreateService(); // empty container — FakeViewModelA never registered
        service.AddPage<FakeViewModelA, FakePageA>();

        var ex = Assert.Throws<Exception>(() => service.GetViewModelInstance(typeof(FakeViewModelA)));
        Assert.Contains("Could not create instance", ex.Message);
    }
}
