using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Tests.Fakes;

namespace Meteion.Toolkit.WPF.Localization.Tests;

[Collection(ServiceLocatorTestCollection.Name)]
public class LocalizationServiceLocatorTests
{
    [Fact]
    public void ServiceProviderAccessor_IsSwappable_ForTestingWithoutARealApplication()
    {
        var original = LocalizationServiceLocator.ServiceProviderAccessor;
        try
        {
            var fakeService = new FakeLocalizationService();
            var fakeProvider = new FakeServiceProvider().Add<ILocalizationService>(fakeService);
            LocalizationServiceLocator.ServiceProviderAccessor = () => fakeProvider;

            var resolved = LocalizationServiceLocator.Resolve<ILocalizationService>();

            Assert.Same(fakeService, resolved);
        }
        finally
        {
            LocalizationServiceLocator.ServiceProviderAccessor = original;
        }
    }

    [Fact]
    public void Resolve_ServiceNotRegistered_Throws()
    {
        var original = LocalizationServiceLocator.ServiceProviderAccessor;
        try
        {
            var emptyProvider = new FakeServiceProvider();
            LocalizationServiceLocator.ServiceProviderAccessor = () => emptyProvider;

            Assert.ThrowsAny<Exception>(() => LocalizationServiceLocator.Resolve<ILocalizationService>());
        }
        finally
        {
            LocalizationServiceLocator.ServiceProviderAccessor = original;
        }
    }
}
