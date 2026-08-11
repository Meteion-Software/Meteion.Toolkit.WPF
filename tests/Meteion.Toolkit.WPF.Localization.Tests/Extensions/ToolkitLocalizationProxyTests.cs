using Meteion.Toolkit.WPF.Localization.Extensions;
using Meteion.Toolkit.WPF.Localization.Tests.Fakes;
using System.ComponentModel;
using System.Globalization;

namespace Meteion.Toolkit.WPF.Localization.Tests.Extensions;

public class ToolkitLocalizationProxyTests
{
    private static readonly System.Reflection.Assembly SomeAssembly = typeof(ToolkitLocalizationProxyTests).Assembly;

    [Fact]
    public void Constructor_FetchesValueImmediately()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };

        var proxy = new ToolkitLocalizationProxy(service, "Greeting", SomeAssembly);

        Assert.Equal("Hello", proxy.Value);
        Assert.Equal("Greeting", service.LastRequestedKey);
    }

    [Fact]
    public void OnCultureChanged_RefetchesValueAndRaisesPropertyChanged()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var proxy = new ToolkitLocalizationProxy(service, "Greeting", SomeAssembly);

        PropertyChangedEventArgs? raised = null;
        proxy.PropertyChanged += (_, e) => raised = e;

        service.ValueToReturn = "Bonjour";
        service.RaiseCultureChanged(new CultureInfo("fr-CA"));

        Assert.Equal("Bonjour", proxy.Value);
        Assert.Equal(nameof(ToolkitLocalizationProxy.Value), raised?.PropertyName);
    }

    [Fact]
    public void OnCultureChanged_WithNoSubscribers_DoesNotThrow()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var proxy = new ToolkitLocalizationProxy(service, "Greeting", SomeAssembly);

        // No PropertyChanged subscribers attached — the null-conditional invoke must not throw.
        service.RaiseCultureChanged(new CultureInfo("fr-CA"));

        Assert.Equal("Hello", proxy.Value);
    }
}
