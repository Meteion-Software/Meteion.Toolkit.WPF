using Meteion.Toolkit.WPF.Localization.Resolution;
using Meteion.Toolkit.WPF.Localization.Tests.Fakes;
using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.Localization.Tests.Resolution;

public class XamlAssemblyResolverTests
{
    private static readonly System.Reflection.Assembly ThisTestAssembly = typeof(XamlAssemblyResolverTests).Assembly;
    private static readonly System.Reflection.Assembly WpfLocalizationAssembly = typeof(XamlAssemblyResolver).Assembly;

    [Fact]
    public void Resolve_PackUriWithComponentSegment_ResolvesToMatchingLoadedAssembly()
    {
        var resolver = new XamlAssemblyResolver();
        var baseUri = new Uri("pack://application:,,,/Meteion.Toolkit.WPF.Localization;component/Fake.xaml");
        var provider = new FakeProvideValueServiceProvider().WithUriContext(baseUri);

        var resolved = resolver.Resolve(provider);

        Assert.Same(WpfLocalizationAssembly, resolved);
    }

    [Fact]
    public void Resolve_PackUriWithVersionedComponentSegment_StripsVersionAndResolves()
    {
        var resolver = new XamlAssemblyResolver();
        var baseUri = new Uri("pack://application:,,,/Meteion.Toolkit.WPF.Localization;v1.0.0.0;component/Fake.xaml");
        var provider = new FakeProvideValueServiceProvider().WithUriContext(baseUri);

        var resolved = resolver.Resolve(provider);

        Assert.Same(WpfLocalizationAssembly, resolved);
    }

    [Fact]
    public void Resolve_PackUriWithoutComponentSegment_FallsThroughToTargetObject()
    {
        var resolver = new XamlAssemblyResolver();
        // No ";component" segment => "it's the app's own assembly's resx" — the pack-URI
        // parser deliberately punts (returns null) rather than caching a wrong answer,
        // relying on the TargetObject fallback below to supply the real answer.
        var baseUri = new Uri("pack://application:,,,/Views/MainWindow.xaml");
        var target = this; // an object whose .GetType().Assembly is genuinely this test assembly
        var provider = new FakeProvideValueServiceProvider()
            .WithUriContext(baseUri)
            .WithProvideValueTarget(target, null);

        var resolved = resolver.Resolve(provider);

        Assert.Same(ThisTestAssembly, resolved);
    }

    [Fact]
    public void Resolve_PackUriWithoutComponentSegmentAndNoTargetObject_ReturnsNull()
    {
        var resolver = new XamlAssemblyResolver();
        var baseUri = new Uri("pack://application:,,,/Views/MainWindow.xaml");
        var provider = new FakeProvideValueServiceProvider().WithUriContext(baseUri);

        var resolved = resolver.Resolve(provider);

        Assert.Null(resolved);
    }

    [Fact]
    public void Resolve_LooseXamlBaseUri_FallsThroughToTargetObject()
    {
        var resolver = new XamlAssemblyResolver();
        // Loose (runtime XamlReader.Load) XAML produces a non-pack BaseUri. This library
        // doesn't parse it specially — it just falls through to the TargetObject fallback,
        // same as any other non-pack scheme. This test documents that behavior.
        var baseUri = new Uri("file:///C:/Some/Path/Window.xaml");
        var target = this; // an object whose .GetType().Assembly is genuinely this test assembly
        var provider = new FakeProvideValueServiceProvider()
            .WithUriContext(baseUri)
            .WithProvideValueTarget(target, null);

        var resolved = resolver.Resolve(provider);

        Assert.Same(ThisTestAssembly, resolved);
    }

    [Fact]
    public void Resolve_LooseXamlBaseUriAndNoTargetObject_ReturnsNull()
    {
        var resolver = new XamlAssemblyResolver();
        var baseUri = new Uri("file:///C:/Some/Path/Window.xaml");
        var provider = new FakeProvideValueServiceProvider().WithUriContext(baseUri);

        var resolved = resolver.Resolve(provider);

        Assert.Null(resolved);
    }

    [Fact]
    public void Resolve_NoUriContextAndNoProvideValueTarget_ReturnsNull()
    {
        var resolver = new XamlAssemblyResolver();
        var provider = new FakeProvideValueServiceProvider();

        var resolved = resolver.Resolve(provider);

        Assert.Null(resolved);
    }

    // Constructing a real FrameworkElement (Button) requires an STA thread.
    [StaFact]
    public void Resolve_TargetObjectIsPresentationFrameworkType_TreatedAsSharedDpAndFiltered()
    {
        var resolver = new XamlAssemblyResolver();
        // Can't construct the internal System.Windows.SharedDp type directly from a test,
        // but any real PresentationFramework type (e.g. Button) has the same .Assembly,
        // which is exactly what the filter checks against.
        var target = new Button();
        var provider = new FakeProvideValueServiceProvider().WithProvideValueTarget(target, null);

        var resolved = resolver.Resolve(provider);

        Assert.Null(resolved);
    }

    [Fact]
    public void Resolve_TargetObjectIsOrdinaryType_ResolvesToItsAssembly()
    {
        var resolver = new XamlAssemblyResolver();
        var target = this; // an object whose .GetType().Assembly is genuinely this test assembly
        var provider = new FakeProvideValueServiceProvider().WithProvideValueTarget(target, null);

        var resolved = resolver.Resolve(provider);

        Assert.Same(ThisTestAssembly, resolved);
    }

    [Fact]
    public void Resolve_CalledTwiceWithSameBaseUri_ReturnsConsistentResult()
    {
        var resolver = new XamlAssemblyResolver();
        var baseUri = new Uri("pack://application:,,,/Meteion.Toolkit.WPF.Localization;component/Fake.xaml");
        var provider = new FakeProvideValueServiceProvider().WithUriContext(baseUri);

        var first = resolver.Resolve(provider);
        var second = resolver.Resolve(provider);

        Assert.Same(WpfLocalizationAssembly, first);
        Assert.Same(first, second);
    }
}
