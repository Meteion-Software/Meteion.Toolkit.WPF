using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Extensions;
using Meteion.Toolkit.WPF.Localization.Tests.Fakes;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;

namespace Meteion.Toolkit.WPF.Localization.Tests.Extensions;

/// <summary>
/// Format arguments (<c>Args</c> / <c>Arg0</c>..<c>Arg9</c>) on <see cref="LocalizedValueExtension"/>,
/// <see cref="LocalizedBindingExtension"/> and the <c>Values</c> composite mode of
/// <see cref="CultureAwareFormatExtension"/>. Most go through the real XAML parser so the
/// property names and nested-binding syntax are exercised the way a view would use them.
/// </summary>
[Collection(ServiceLocatorTestCollection.Name)]
public class FormattedLocalizationTests
{
    private static readonly System.Reflection.Assembly SomeAssembly = typeof(FormattedLocalizationTests).Assembly;

    private const string Namespaces =
        """xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:lx="http://wpf.meteion.ca/winfx/xaml/localization" """;

    private static IDisposable UseFakeLocator(ILocalizationService service, LocalizationOptions? options = null)
    {
        var original = LocalizationServiceLocator.ServiceProviderAccessor;
        var fakeProvider = new FakeServiceProvider()
            .Add<ILocalizationService>(service)
            .Add<IResourceAssemblyResolver>(new FakeResourceAssemblyResolver(SomeAssembly));
        if (options is not null)
        {
            fakeProvider.Add<Microsoft.Extensions.Options.IOptions<LocalizationOptions>>(Microsoft.Extensions.Options.Options.Create(options));
        }

        LocalizationServiceLocator.ServiceProviderAccessor = () => fakeProvider;
        return new RestoreAccessor(original);
    }

    // A MultiBinding only activates once the element has been through layout.
    private static Button ParseButton(string contentAttribute, object dataContext)
    {
        var button = (Button)XamlReader.Parse($"<Button {Namespaces} {contentAttribute} />");
        button.DataContext = dataContext;
        button.Measure(new Size(100, 100));
        button.Arrange(new Rect(0, 0, 100, 100));
        button.UpdateLayout();
        return button;
    }

    [StaFact]
    public void LocalizedValue_ArgShorthand_FormatsWithBoundAndConstantArgs()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello {0}, you have {1} items", CurrentCulture = new CultureInfo("en-US") };
        using (UseFakeLocator(service))
        {
            var button = ParseButton(
                "Content=\"{lx:LocalizedValue Key=Greeting, Arg0={Binding Name}, Arg1=5}\"",
                new Person { Name = "Ada", Count = 1 });

            Assert.Equal("Hello Ada, you have 5 items", button.Content);
        }
    }

    [StaFact]
    public void LocalizedValue_ArgsPropertyElement_FormatsWithEachChildBinding()
    {
        var service = new FakeLocalizationService { ValueToReturn = "{1}/{0}", CurrentCulture = new CultureInfo("en-US") };
        using (UseFakeLocator(service))
        {
            var button = (Button)XamlReader.Parse($"""
                <Button {Namespaces}>
                    <Button.Content>
                        <lx:LocalizedValue Key="Pair">
                            <lx:LocalizedValue.Args>
                                <MultiBinding>
                                    <Binding Path="Name" />
                                    <Binding Path="Count" />
                                </MultiBinding>
                            </lx:LocalizedValue.Args>
                        </lx:LocalizedValue>
                    </Button.Content>
                </Button>
                """);
            button.DataContext = new Person { Name = "Ada", Count = 7 };
            button.Measure(new Size(100, 100));
            button.Arrange(new Rect(0, 0, 100, 100));
            button.UpdateLayout();

            Assert.Equal("7/Ada", button.Content);
        }
    }

    [StaFact]
    public void LocalizedValue_ArgChanges_ReformatsLive()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello {0}", CurrentCulture = new CultureInfo("en-US") };
        using (UseFakeLocator(service))
        {
            var person = new Person { Name = "Ada" };
            var button = ParseButton("Content=\"{lx:LocalizedValue Key=Greeting, Arg0={Binding Name}}\"", person);

            person.Name = "Grace";

            Assert.Equal("Hello Grace", button.Content);
        }
    }

    [StaFact]
    public void LocalizedValue_CultureChanges_ReformatsWithNewCulture()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Total {0:N0}", CurrentCulture = new CultureInfo("en-US") };
        using (UseFakeLocator(service))
        {
            var button = ParseButton(
                "Content=\"{lx:LocalizedValue Key=Total, Arg0={Binding Count}}\"",
                new Person { Count = 1234567 });
            Assert.Equal("Total 1,234,567", button.Content);

            service.CurrentCulture = new CultureInfo("de-DE");
            service.ValueToReturn = "Summe {0:N0}";
            service.RaiseCultureChanged(service.CurrentCulture);

            Assert.Equal("Summe 1.234.567", button.Content);
        }
    }

    [StaFact]
    public void LocalizedValue_KeyBindingWithArgs_ResolvesKeyThenFormats()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hi {0}", CurrentCulture = new CultureInfo("en-US") };
        using (UseFakeLocator(service))
        {
            var person = new Person { Name = "Ada", Key = "Greeting" };
            var button = ParseButton(
                "Content=\"{lx:LocalizedValue KeyBinding={Binding Key}, Arg0={Binding Name}}\"", person);

            Assert.Equal("Greeting", service.LastRequestedKey);
            Assert.Equal("Hi Ada", button.Content);

            service.ValueToReturn = "Bye {0}";
            person.Key = "Farewell";
            Assert.Equal("Farewell", service.LastRequestedKey);
            Assert.Equal("Bye Ada", button.Content);
        }
    }

    [StaFact]
    public void LocalizedValue_NullArg_RendersEmpty()
    {
        var service = new FakeLocalizationService { ValueToReturn = "[{0}]", CurrentCulture = new CultureInfo("en-US") };
        using (UseFakeLocator(service))
        {
            var button = ParseButton(
                "Content=\"{lx:LocalizedValue Key=Wrapped, Arg0={Binding Name}}\"", new Person { Name = null });

            Assert.Equal("[]", button.Content);
        }
    }

    [StaFact]
    public void LocalizedValue_NoArgs_DoesNotFormat_SoBracesStayLiteral()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Use {braces} freely" };
        using (UseFakeLocator(service))
        {
            var button = ParseButton("Content=\"{lx:LocalizedValue Key=Literal}\"", new Person());

            Assert.Equal("Use {braces} freely", button.Content);
        }
    }

    [StaFact]
    public void LocalizedValue_PlainClrPropertyTarget_FormatsWithArgs()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello {0}", CurrentCulture = new CultureInfo("en-US") };
        using (UseFakeLocator(service))
        {
            var textBlock = (TextBlock)XamlReader.Parse($$$"""
                <TextBlock {{{Namespaces}}}><Run Text="{lx:LocalizedValue Key=Greeting, Arg0={Binding Name}}" /></TextBlock>
                """);
            textBlock.DataContext = new Person { Name = "Ada" };
            textBlock.Measure(new Size(100, 100));
            textBlock.Arrange(new Rect(0, 0, 100, 100));
            textBlock.UpdateLayout();
            textBlock.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, () => { });

            Assert.Equal("Hello Ada",((System.Windows.Documents.Run)textBlock.Inlines.FirstInline!).Text);
        }
    }

    [Fact]
    public void LocalizedValue_ArgsCombinedWithArgShorthand_ThrowsConfigurationException()
    {
        using (UseFakeLocator(new FakeLocalizationService()))
        {
            var args = new MultiBinding();
            args.Bindings.Add(new Binding("Name"));
            var extension = new LocalizedValueExtension { Key = "Greeting", Args = args, Arg0 = new Binding("Name") };

            Assert.Throws<LocalizationConfigurationException>(
                () => extension.ProvideValue(new FakeProvideValueServiceProvider()));
        }
    }

    [Fact]
    public void LocalizedValue_ArgGap_ThrowsConfigurationException()
    {
        using (UseFakeLocator(new FakeLocalizationService()))
        {
            var extension = new LocalizedValueExtension { Key = "Greeting", Arg1 = new Binding("Name") };

            Assert.Throws<LocalizationConfigurationException>(
                () => extension.ProvideValue(new FakeProvideValueServiceProvider()));
        }
    }

    [Fact]
    public void LocalizedValue_ArgsWithNoLiveTarget_ThrowsConfigurationException()
    {
        using (UseFakeLocator(new FakeLocalizationService()))
        {
            var extension = new LocalizedValueExtension { Key = "Greeting", Arg0 = new Binding("Name") };

            // Nothing to bind the arguments against - must not silently show the raw template.
            Assert.Throws<LocalizationConfigurationException>(
                () => extension.ProvideValue(new FakeProvideValueServiceProvider()));
        }
    }

    [StaTheory]
    [InlineData(MissingResourceBehavior.ReturnKey, "Hello {0} {1}")]
    [InlineData(MissingResourceBehavior.ReturnEmptyString, "")]
    public void LocalizedValue_TooFewArguments_FollowsMissingKeyBehavior(MissingResourceBehavior behavior, string expected)
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello {0} {1}" };
        using (UseFakeLocator(service, new LocalizationOptions { MissingKeyBehavior = behavior }))
        {
            var button = ParseButton("Content=\"{lx:LocalizedValue Key=Greeting, Arg0={Binding Name}}\"", new Person { Name = "Ada" });

            Assert.Equal(expected, button.Content);
        }
    }

    [StaFact]
    public void LocalizedBinding_Args_FormatsKeyBindingResult()
    {
        var service = new FakeLocalizationService { ValueToReturn = "{0} items", CurrentCulture = new CultureInfo("en-US") };
        using (UseFakeLocator(service))
        {
            var button = ParseButton(
                "Content=\"{lx:LocalizedBinding KeyBinding={Binding Key}, Arg0={Binding Count}}\"",
                new Person { Key = "Items", Count = 3 });

            Assert.Equal("3 items", button.Content);
        }
    }

    [StaFact]
    public void CultureAwareFormat_Values_FormatsAllWithCompositeStringAndReformatsOnCultureChange()
    {
        var service = new FakeLocalizationService { CurrentCulture = new CultureInfo("en-US") };
        using (UseFakeLocator(service))
        {
            var button = (Button)XamlReader.Parse($$"""
                <Button {{Namespaces}}>
                    <Button.Content>
                        <lx:CultureAwareFormat FormatString="{}{0:N1} of {1:N1}">
                            <lx:CultureAwareFormat.Values>
                                <MultiBinding>
                                    <Binding Path="Count" />
                                    <Binding Path="Total" />
                                </MultiBinding>
                            </lx:CultureAwareFormat.Values>
                        </lx:CultureAwareFormat>
                    </Button.Content>
                </Button>
                """);
            button.DataContext = new Person { Count = 1, Total = 2.5m };
            button.Measure(new Size(100, 100));
            button.Arrange(new Rect(0, 0, 100, 100));
            button.UpdateLayout();
            Assert.Equal("1.0 of 2.5", button.Content);

            service.CurrentCulture = new CultureInfo("de-DE");
            service.RaiseCultureChanged(service.CurrentCulture);
            Assert.Equal("1,0 of 2,5", button.Content);
        }
    }

    [Fact]
    public void CultureAwareFormat_ValueAndValuesBothSet_ThrowsConfigurationException()
    {
        using (UseFakeLocator(new FakeLocalizationService()))
        {
            var values = new MultiBinding();
            values.Bindings.Add(new Binding("A"));
            var extension = new CultureAwareFormatExtension(new Binding("A"), "{0}") { Values = values };

            Assert.Throws<LocalizationConfigurationException>(
                () => extension.ProvideValue(new FakeProvideValueServiceProvider()));
        }
    }

    [Fact]
    public void CultureAwareFormat_ValuesWithoutFormatString_ThrowsConfigurationException()
    {
        using (UseFakeLocator(new FakeLocalizationService()))
        {
            var values = new MultiBinding();
            values.Bindings.Add(new Binding("A"));
            var extension = new CultureAwareFormatExtension { Values = values };

            Assert.Throws<LocalizationConfigurationException>(
                () => extension.ProvideValue(new FakeProvideValueServiceProvider()));
        }
    }

    [Fact]
    public void FormatArguments_Format_UnsetValueAndDoNothingRenderEmpty()
    {
        var result = FormatArguments.Format(
            CultureInfo.InvariantCulture, "[{0}][{1}][{2}]", [DependencyProperty.UnsetValue, Binding.DoNothing, 5]);

        Assert.Equal("[][][5]", result);
    }

    [Fact]
    public void FormatArguments_Format_BadTemplate_ThrowsConfigurationException()
    {
        Assert.Throws<LocalizationConfigurationException>(
            () => FormatArguments.Format(CultureInfo.InvariantCulture, "{0", ["x"]));
    }

    [Fact]
    public void FormatArguments_FormatForBinding_ThrowExceptionBehavior_Rethrows()
    {
        Assert.Throws<LocalizationConfigurationException>(
            () => FormatArguments.FormatForBinding(CultureInfo.InvariantCulture, "{1}", ["x"], MissingResourceBehavior.ThrowException));
    }

    private sealed class Person : INotifyPropertyChanged
    {
        private string? _name;
        private string? _key;

        public string? Name { get => _name; set { _name = value; Raise(nameof(Name)); } }

        public string? Key { get => _key; set { _key = value; Raise(nameof(Key)); } }

        public int Count { get; set; }

        public decimal Total { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    private sealed class RestoreAccessor(Func<IServiceProvider> original) : IDisposable
    {
        public void Dispose() => LocalizationServiceLocator.ServiceProviderAccessor = original;
    }
}
