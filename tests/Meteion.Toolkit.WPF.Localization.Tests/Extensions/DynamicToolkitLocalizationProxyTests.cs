using Meteion.Toolkit.WPF.Localization.Extensions;
using Meteion.Toolkit.WPF.Localization.Tests.Fakes;
using System.ComponentModel;
using System.Globalization;

namespace Meteion.Toolkit.WPF.Localization.Tests.Extensions;

public class DynamicToolkitLocalizationProxyTests
{
    private static readonly System.Reflection.Assembly SomeAssembly = typeof(DynamicToolkitLocalizationProxyTests).Assembly;

    [StaFact]
    public void Constructor_NoKeyYet_ValueIsEmpty()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };

        var proxy = new DynamicToolkitLocalizationProxy(service, SomeAssembly);

        Assert.Equal(string.Empty, proxy.Value);
        Assert.Equal(0, service.GetStringCallCount);
    }

    [StaFact]
    public void KeySet_ResolvesValueAndRaisesPropertyChanged()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var proxy = new DynamicToolkitLocalizationProxy(service, SomeAssembly);

        PropertyChangedEventArgs? raised = null;
        proxy.PropertyChanged += (_, e) => raised = e;

        proxy.Key = "Greeting";

        Assert.Equal("Hello", proxy.Value);
        Assert.Equal("Greeting", service.LastRequestedKey);
        Assert.Equal(nameof(DynamicToolkitLocalizationProxy.Value), raised?.PropertyName);
    }

    [StaFact]
    public void KeySetToSameValue_DoesNotRecompute()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var proxy = new DynamicToolkitLocalizationProxy(service, SomeAssembly) { Key = "Greeting" };
        var callCountAfterFirstSet = service.GetStringCallCount;

        proxy.Key = "Greeting";

        Assert.Equal(callCountAfterFirstSet, service.GetStringCallCount);
    }

    [StaFact]
    public void KeyChangedToDifferentValue_ReResolvesUsingNewKey()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var proxy = new DynamicToolkitLocalizationProxy(service, SomeAssembly) { Key = "Greeting" };

        service.ValueToReturn = "Goodbye";
        proxy.Key = "Farewell";

        Assert.Equal("Goodbye", proxy.Value);
        Assert.Equal("Farewell", service.LastRequestedKey);
    }

    [StaFact]
    public void OnCultureChanged_RefetchesValueUsingCurrentKey()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var proxy = new DynamicToolkitLocalizationProxy(service, SomeAssembly) { Key = "Greeting" };

        service.ValueToReturn = "Bonjour";
        service.RaiseCultureChanged(new CultureInfo("fr-CA"));

        Assert.Equal("Bonjour", proxy.Value);
        Assert.Equal("Greeting", service.LastRequestedKey);
    }

    [StaFact]
    public void OnCultureChanged_WithNoKeySetYet_StaysEmptyAndDoesNotCallService()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var proxy = new DynamicToolkitLocalizationProxy(service, SomeAssembly);

        service.RaiseCultureChanged(new CultureInfo("fr-CA"));

        Assert.Equal(string.Empty, proxy.Value);
        Assert.Equal(0, service.GetStringCallCount);
    }
}
