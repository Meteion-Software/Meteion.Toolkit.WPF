using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Tests.Fakes;
using System.Globalization;
using System.Reflection;

namespace Meteion.Toolkit.WPF.Localization.Tests;

[Collection(ServiceLocatorTestCollection.Name)]
public class ToolkitLocalizerTests
{
    private static readonly Assembly ThisTestAssembly = typeof(ToolkitLocalizerTests).Assembly;

    private static IDisposable UseFakeLocator(FakeLocalizationService service)
    {
        var original = LocalizationServiceLocator.ServiceProviderAccessor;
        var fakeProvider = new FakeServiceProvider().Add<ILocalizationService>(service);
        LocalizationServiceLocator.ServiceProviderAccessor = () => fakeProvider;
        return new RestoreAccessor(original);
    }

    [Fact]
    public void Get_NoExplicitAssembly_FallsBackToCallingAssembly()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        using (UseFakeLocator(service))
        {
            // Called directly from this test method — GetCallingAssembly() should
            // resolve to this test assembly, matching the "call this directly" caveat
            // documented on ToolkitLocalizer.
            var result = ToolkitLocalizer.Get("Greeting");

            Assert.Equal("Hello", result);
            Assert.Same(ThisTestAssembly, service.LastRequestedAssembly);
        }
    }

    [Fact]
    public void Get_ExplicitAssembly_UsesItInstead()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        using (UseFakeLocator(service))
        {
            var otherAssembly = typeof(object).Assembly;
            ToolkitLocalizer.Get("Greeting", otherAssembly);

            Assert.Same(otherAssembly, service.LastRequestedAssembly);
        }
    }

    [Fact]
    public void CurrentCulture_Get_PassesThroughToLocalizationService()
    {
        var service = new FakeLocalizationService { CurrentCulture = new CultureInfo("ja-JP") };
        using (UseFakeLocator(service))
        {
            Assert.Equal(new CultureInfo("ja-JP"), ToolkitLocalizer.CurrentCulture);
        }
    }

    [Fact]
    public void CurrentCulture_Set_PassesThroughToLocalizationService()
    {
        var service = new FakeLocalizationService();
        using (UseFakeLocator(service))
        {
            ToolkitLocalizer.CurrentCulture = new CultureInfo("fr-CA");

            Assert.Equal(new CultureInfo("fr-CA"), service.CurrentCulture);
        }
    }

    private sealed class RestoreAccessor(Func<IServiceProvider> original) : IDisposable
    {
        public void Dispose() => LocalizationServiceLocator.ServiceProviderAccessor = original;
    }
}
