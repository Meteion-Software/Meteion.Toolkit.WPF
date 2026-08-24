using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Extensions;
using Meteion.Toolkit.WPF.Localization.Tests.Fakes;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace Meteion.Toolkit.WPF.Localization.Tests.Extensions;

/// <summary>
/// Design-time-mode ("[Key]" placeholder) is intentionally not covered here — WPF's
/// DesignerProperties.GetIsInDesignMode default value can only be forced to true via a
/// one-time, irreversible DependencyProperty.OverrideMetadata call that would leak into
/// every other test in this assembly for the rest of the process. Not worth the risk for
/// a one-line branch.
/// </summary>
[Collection(ServiceLocatorTestCollection.Name)]
public class LocalizedValueExtensionTests
{
    private static readonly Assembly SomeAssembly = typeof(LocalizedValueExtensionTests).Assembly;

    private static IDisposable UseFakeLocator(ILocalizationService service, IResourceAssemblyResolver resolver)
    {
        var original = LocalizationServiceLocator.ServiceProviderAccessor;
        var fakeProvider = new FakeServiceProvider()
            .Add<ILocalizationService>(service)
            .Add<IResourceAssemblyResolver>(resolver);
        LocalizationServiceLocator.ServiceProviderAccessor = () => fakeProvider;
        return new RestoreAccessor(original);
    }

    [Fact]
    public void ProvideValue_KeyIsNull_ReturnsNoKeyPlaceholder()
    {
        var extension = new LocalizedValueExtension { Key = null };

        var result = extension.ProvideValue(new FakeProvideValueServiceProvider());

        Assert.Equal("NOKEY", result);
    }

    // Constructing a real FrameworkElement (TextBlock) requires an STA thread.
    [StaFact]
    public void ProvideValue_DependencyPropertyTarget_BindsLiveAndReturnsInitialValue()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var resolver = new FakeResourceAssemblyResolver(SomeAssembly);
        using (UseFakeLocator(service, resolver))
        {
            var textBlock = new TextBlock();
            var provider = new FakeProvideValueServiceProvider()
                .WithProvideValueTarget(textBlock, TextBlock.TextProperty);
            var extension = new LocalizedValueExtension { Key = "Greeting" };

            var result = extension.ProvideValue(provider);

            Assert.Equal("Hello", result);
            Assert.Equal("Hello", textBlock.Text);

            service.ValueToReturn = "Bonjour";
            service.RaiseCultureChanged(new CultureInfo("fr-CA"));

            Assert.Equal("Bonjour", textBlock.Text);
        }
    }

    [Fact]
    public void ProvideValue_PlainClrPropertyTarget_BindsLiveViaReflection()
    {
        // Reproduces the exact real-world crash scenario: a <Run Text="{lx:LocalizedValue ...}"/>,
        // where Run.Text is a plain CLR property, not a DependencyProperty.
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var resolver = new FakeResourceAssemblyResolver(SomeAssembly);
        using (UseFakeLocator(service, resolver))
        {
            var run = new Run();
            var textProperty = typeof(Run).GetProperty(nameof(Run.Text))!;
            var provider = new FakeProvideValueServiceProvider()
                .WithProvideValueTarget(run, textProperty);
            var extension = new LocalizedValueExtension { Key = "Greeting" };

            var result = extension.ProvideValue(provider);

            Assert.Equal("Hello", result);
            Assert.Equal("Hello", run.Text);
        }
    }

    [Fact]
    public void ProvideValue_NoProvideValueTarget_ReturnsResolvedStringDirectlyWithoutBinding()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var resolver = new FakeResourceAssemblyResolver(SomeAssembly);
        using (UseFakeLocator(service, resolver))
        {
            var extension = new LocalizedValueExtension { Key = "Greeting" };

            var result = extension.ProvideValue(new FakeProvideValueServiceProvider());

            Assert.Equal("Hello", result);
        }
    }

    [Fact]
    public void ProvideValue_ExplicitAssembly_IsPassedToResourceAssemblyResolver()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var resolver = new FakeResourceAssemblyResolver(SomeAssembly);
        using (UseFakeLocator(service, resolver))
        {
            var explicitAssembly = typeof(object).Assembly;
            var extension = new LocalizedValueExtension { Key = "Greeting", Assembly = explicitAssembly };

            extension.ProvideValue(new FakeProvideValueServiceProvider());

            Assert.Same(explicitAssembly, resolver.LastExplicitAssembly);
        }
    }

    // Constructing a real FrameworkElement (TextBlock) requires an STA thread.
    [StaFact]
    public void ProvideValue_KeyBindingDependencyPropertyTarget_BindsLiveToSourcePropertyAndCulture()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var resolver = new FakeResourceAssemblyResolver(SomeAssembly);
        using (UseFakeLocator(service, resolver))
        {
            var source = new KeySource { TitleKey = "Greeting" };
            var textBlock = new TextBlock { DataContext = source };
            var provider = new FakeProvideValueServiceProvider()
                .WithProvideValueTarget(textBlock, TextBlock.TextProperty);
            var extension = new LocalizedValueExtension { KeyBinding = new Binding(nameof(KeySource.TitleKey)) };

            var result = extension.ProvideValue(provider);

            Assert.Equal("Hello", result);
            Assert.Equal("Hello", textBlock.Text);

            // Source property changes...
            service.ValueToReturn = "Goodbye";
            source.TitleKey = "Farewell";
            Assert.Equal("Goodbye", textBlock.Text);

            // ...and culture changes, both re-resolve using the current bound key.
            service.ValueToReturn = "Au revoir";
            service.RaiseCultureChanged(new CultureInfo("fr-CA"));
            Assert.Equal("Au revoir", textBlock.Text);
            Assert.Equal("Farewell", service.LastRequestedKey);
        }
    }

    [StaFact]
    public void ProvideValue_KeyBindingAndLiteralKeyBothSet_KeyBindingTakesPrecedence()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var resolver = new FakeResourceAssemblyResolver(SomeAssembly);
        using (UseFakeLocator(service, resolver))
        {
            var source = new KeySource { TitleKey = "FromBinding" };
            var textBlock = new TextBlock { DataContext = source };
            var provider = new FakeProvideValueServiceProvider()
                .WithProvideValueTarget(textBlock, TextBlock.TextProperty);
            var extension = new LocalizedValueExtension
            {
                Key = "FromLiteral",
                KeyBinding = new Binding(nameof(KeySource.TitleKey)),
            };

            extension.ProvideValue(provider);

            Assert.Equal("FromBinding", service.LastRequestedKey);
        }
    }

    private sealed class KeySource : INotifyPropertyChanged
    {
        private string? _titleKey;

        public string? TitleKey
        {
            get => _titleKey;
            set
            {
                _titleKey = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TitleKey)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class RestoreAccessor(Func<IServiceProvider> original) : IDisposable
    {
        public void Dispose() => LocalizationServiceLocator.ServiceProviderAccessor = original;
    }
}
