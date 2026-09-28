using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Extensions;
using Meteion.Toolkit.WPF.Localization.Tests.Fakes;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

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

    private static IDisposable UseFakeLocator(ILocalizationService service, IResourceAssemblyResolver resolver, LocalizationOptions? options = null)
    {
        var original = LocalizationServiceLocator.ServiceProviderAccessor;
        var fakeProvider = new FakeServiceProvider()
            .Add<ILocalizationService>(service)
            .Add<IResourceAssemblyResolver>(resolver);
        if (options is not null)
        {
            fakeProvider.Add<Microsoft.Extensions.Options.IOptions<LocalizationOptions>>(Microsoft.Extensions.Options.Options.Create(options));
        }

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

    [Fact]
    public void ProvideValue_KeyBindingWithNoProvideValueTarget_ThrowsConfigurationException()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var resolver = new FakeResourceAssemblyResolver(SomeAssembly);
        using (UseFakeLocator(service, resolver))
        {
            var extension = new LocalizedValueExtension { KeyBinding = new Binding(nameof(KeySource.TitleKey)) };

            // No target info at all to resolve the key source against — must throw rather than
            // silently return an empty string, which would look like a bug in the consuming app.
            Assert.Throws<LocalizationConfigurationException>(
                () => extension.ProvideValue(new FakeProvideValueServiceProvider()));
        }
    }

    [Fact]
    public void ProvideValue_KeyBindingWithPlainClrPropertyTargetAndNoLiveElement_ThrowsConfigurationException()
    {
        // Simulates what WPF hands the extension for a plain CLR property (e.g. Run.Text)
        // inside a DataTemplate/ControlTemplate: TargetObject is not a real, connected
        // DependencyObject (WPF's shared template placeholder in the real scenario), so there's
        // nothing to bind the key source against.
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var resolver = new FakeResourceAssemblyResolver(SomeAssembly);
        using (UseFakeLocator(service, resolver))
        {
            var textProperty = typeof(Run).GetProperty(nameof(Run.Text))!;
            var provider = new FakeProvideValueServiceProvider()
                .WithProvideValueTarget(new object(), textProperty);
            var extension = new LocalizedValueExtension { KeyBinding = new Binding(nameof(KeySource.TitleKey)) };

            Assert.Throws<LocalizationConfigurationException>(() => extension.ProvideValue(provider));
        }
    }

    // Faithful regression test for the real-world bug report: {lx:LocalizedValue
    // KeyBinding=...} used inside an ItemsControl.ItemTemplate rendered blank for every row.
    // This has to go through WPF's real XAML parser and template machinery (not
    // FakeProvideValueServiceProvider) because the bug was specific to
    // IProvideValueTarget.TargetObject being WPF's shared template placeholder
    // (System.Windows.SharedDp) rather than the real per-row element.
    [StaFact]
    public void ProvideValue_KeyBindingInsideDataTemplate_ResolvesEachRowIndependently()
    {
        var service = new FakeLocalizationService();
        var resolver = new FakeResourceAssemblyResolver(SomeAssembly);
        using (UseFakeLocator(service, resolver))
        {
            const string templateXaml = """
                <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                              xmlns:lx="http://wpf.meteion.ca/winfx/xaml/localization">
                    <TextBlock Text="{lx:LocalizedValue KeyBinding={Binding Key}}" />
                </DataTemplate>
                """;
            var itemTemplate = (DataTemplate)XamlReader.Parse(templateXaml);

            var itemsControl = new ItemsControl
            {
                ItemTemplate = itemTemplate,
                ItemsSource = new[] { new KeyedRow("RowA"), new KeyedRow("RowB"), new KeyedRow("RowC") },
            };

            // FakeLocalizationService.GetString echoes the key back when ValueToReturn is
            // unset, so each row's resolved text should equal that row's own key — proving
            // KeyBinding resolved per-row rather than being blank (the bug) or shared/identical
            // across every row.
            var window = new Window
            {
                Content = itemsControl,
                Width = 200,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -3000,
                Top = -3000,
                ShowActivated = false,
            };
            try
            {
                window.Show();
                window.UpdateLayout();

                var rowTexts = Enumerable.Range(0, 3)
                    .Select(i => itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as DependencyObject)
                    .Select(container => container == null ? null : FindVisualChild<TextBlock>(container))
                    .Select(textBlock => textBlock?.Text)
                    .ToArray();

                Assert.Equal(new[] { "RowA", "RowB", "RowC" }, rowTexts);
            }
            finally
            {
                window.Close();
            }
        }
    }

    // Regression test for the enum-flavored variant of the same bug: a KeyBinding source
    // that isn't already a string (e.g. {Binding SomeEnumProperty}, as Outrun's
    // UnitSettingTypeEnum-driven settings list does) resolved fine outside a DataTemplate
    // (a real DependencyProperty binding gets WPF's implicit enum-to-string conversion) but
    // rendered blank for every row inside one, because the MultiBinding converter's
    // `values[0] as string` cast silently misses on a boxed enum with no error of any kind.
    [StaFact]
    public void ProvideValue_KeyBindingInsideDataTemplate_ResolvesEnumSourcePerRow()
    {
        var service = new FakeLocalizationService();
        var resolver = new FakeResourceAssemblyResolver(SomeAssembly);
        using (UseFakeLocator(service, resolver))
        {
            const string templateXaml = """
                <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                              xmlns:lx="http://wpf.meteion.ca/winfx/xaml/localization">
                    <TextBlock Text="{lx:LocalizedValue KeyBinding={Binding Key}}" />
                </DataTemplate>
                """;
            var itemTemplate = (DataTemplate)XamlReader.Parse(templateXaml);

            var itemsControl = new ItemsControl
            {
                ItemTemplate = itemTemplate,
                ItemsSource = new[] { new EnumKeyedRow(SampleEnum.Alpha), new EnumKeyedRow(SampleEnum.Beta) },
            };

            var window = new Window
            {
                Content = itemsControl,
                Width = 200,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -3000,
                Top = -3000,
                ShowActivated = false,
            };
            try
            {
                window.Show();
                window.UpdateLayout();

                var rowTexts = Enumerable.Range(0, 2)
                    .Select(i => itemsControl.ItemContainerGenerator.ContainerFromIndex(i) as DependencyObject)
                    .Select(container => container == null ? null : FindVisualChild<TextBlock>(container))
                    .Select(textBlock => textBlock?.Text)
                    .ToArray();

                // FakeLocalizationService.GetString echoes the key back, so each row's text
                // should equal that row's own enum member name — not blank.
                Assert.Equal(new[] { nameof(SampleEnum.Alpha), nameof(SampleEnum.Beta) }, rowTexts);
            }
            finally
            {
                window.Close();
            }
        }
    }

    private enum SampleEnum { Alpha, Beta }

    private sealed record EnumKeyedRow(SampleEnum Key);

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        var childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typed)
            {
                return typed;
            }

            var descendant = FindVisualChild<T>(child);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
    }

    private sealed record KeyedRow(string Key);

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

    private static readonly string SomeSource = $"{SomeAssembly.GetName().Name}/Some.Strings";

    [Fact]
    public void ProvideValue_QualifiedKey_ResolvesWithoutAssembly()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        using (UseFakeLocator(service, new FakeResourceAssemblyResolver(SomeAssembly)))
        {
            var extension = new LocalizedValueExtension { Key = $"{SomeSource}:Greeting" };

            var result = extension.ProvideValue(new FakeProvideValueServiceProvider());

            Assert.Equal("Hello", result);
            Assert.Equal($"{SomeSource}:Greeting", service.LastRequestedKey);
            Assert.Null(service.LastRequestedAssembly);
            Assert.Null(service.LastRequestedSource);
        }
    }

    [Fact]
    public void ProvideValue_UnqualifiedKeyWithSource_ResolvesFromSourceWithPrefix()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        using (UseFakeLocator(service, new FakeResourceAssemblyResolver(SomeAssembly)))
        {
            var extension = new LocalizedValueExtension { Key = "Info", KeyPrefix = "Notification_", Source = SomeSource };

            extension.ProvideValue(new FakeProvideValueServiceProvider());

            Assert.Equal("Notification_Info", service.LastRequestedKey);
            Assert.Equal(SomeSource, service.LastRequestedSource);
        }
    }

    [Fact]
    public void ProvideValue_QualifiedKeyWithMatchingSource_Resolves()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        using (UseFakeLocator(service, new FakeResourceAssemblyResolver(SomeAssembly)))
        {
            var extension = new LocalizedValueExtension { Key = $"{SomeSource}:Greeting", Source = SomeSource };

            Assert.Equal("Hello", extension.ProvideValue(new FakeProvideValueServiceProvider()));
        }
    }

    public static TheoryData<LocalizedValueExtension> InvalidCombinations => new()
    {
        // Source already names its assembly.
        new LocalizedValueExtension { Key = "Greeting", Source = SomeSource, Assembly = SomeAssembly },
        // A qualified key can't take a prefix.
        new LocalizedValueExtension { Key = $"{SomeSource}:Greeting", KeyPrefix = "Notification_" },
        // Qualified key disagreeing with Source...
        new LocalizedValueExtension { Key = $"{SomeSource}:Greeting", Source = $"{SomeAssembly.GetName().Name}/Other.Strings" },
        // ...or with an explicit Assembly.
        new LocalizedValueExtension { Key = $"{SomeSource}:Greeting", Assembly = typeof(object).Assembly },
    };

    [Theory]
    [MemberData(nameof(InvalidCombinations))]
    public void ProvideValue_InvalidCombination_ThrowsLocalizationConfigurationException(LocalizedValueExtension extension)
    {
        using (UseFakeLocator(new FakeLocalizationService(), new FakeResourceAssemblyResolver(SomeAssembly)))
        {
            Assert.Throws<LocalizationConfigurationException>(() => extension.ProvideValue(new FakeProvideValueServiceProvider()));
        }
    }

    // A bound value that breaks the rules (here: a qualified key combined with KeyPrefix) is
    // handled per MissingKeyBehavior, on both KeyBinding paths.
    [StaTheory]
    [InlineData(MissingResourceBehavior.ReturnKey, "{0}:Greeting")]
    [InlineData(MissingResourceBehavior.ReturnEmptyString, "")]
    public void ProvideValue_KeyBindingProducesInvalidKey_FollowsMissingKeyBehavior(MissingResourceBehavior behavior, string expectedFormat)
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        using (UseFakeLocator(service, new FakeResourceAssemblyResolver(SomeAssembly), new LocalizationOptions { MissingKeyBehavior = behavior }))
        {
            var source = new KeySource { TitleKey = $"{SomeSource}:Greeting" };
            var textBlock = new TextBlock { DataContext = source };
            var provider = new FakeProvideValueServiceProvider()
                .WithProvideValueTarget(textBlock, TextBlock.TextProperty);
            var extension = new LocalizedValueExtension
            {
                KeyBinding = new Binding(nameof(KeySource.TitleKey)),
                KeyPrefix = "Notification_",
            };

            extension.ProvideValue(provider);

            Assert.Equal(string.Format(expectedFormat, SomeSource), textBlock.Text);
            Assert.Equal(0, service.GetStringCallCount);
        }
    }

    [StaFact]
    public void ProvideValue_KeyBindingProducesInvalidKey_ThrowException_Throws()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var options = new LocalizationOptions { MissingKeyBehavior = MissingResourceBehavior.ThrowException };
        using (UseFakeLocator(service, new FakeResourceAssemblyResolver(SomeAssembly), options))
        {
            var source = new KeySource { TitleKey = $"{SomeSource}:Greeting" };
            var textBlock = new TextBlock { DataContext = source };
            var provider = new FakeProvideValueServiceProvider()
                .WithProvideValueTarget(textBlock, TextBlock.TextProperty);
            var extension = new LocalizedValueExtension
            {
                KeyBinding = new Binding(nameof(KeySource.TitleKey)),
                KeyPrefix = "Notification_",
            };

            Assert.Throws<LocalizationConfigurationException>(() => extension.ProvideValue(provider));
        }
    }

    [StaFact]
    public void ProvideValue_NoOptionsRegistered_DefaultsToThrowException()
    {
        using (UseFakeLocator(new FakeLocalizationService(), new FakeResourceAssemblyResolver(SomeAssembly)))
        {
            var source = new KeySource { TitleKey = $"{SomeSource}:Greeting" };
            var textBlock = new TextBlock { DataContext = source };
            var provider = new FakeProvideValueServiceProvider()
                .WithProvideValueTarget(textBlock, TextBlock.TextProperty);
            var extension = new LocalizedValueExtension
            {
                KeyBinding = new Binding(nameof(KeySource.TitleKey)),
                KeyPrefix = "Notification_",
            };

            Assert.Throws<LocalizationConfigurationException>(() => extension.ProvideValue(provider));
        }
    }

    // The in-template path: ProvideValue returns a MultiBinding whose converter does the lookup.
    [Theory]
    [InlineData(MissingResourceBehavior.ReturnKey, "Asm/Asm.Strings:Greeting")]
    [InlineData(MissingResourceBehavior.ReturnEmptyString, "")]
    public void DynamicKeyLocalizationConverter_InvalidKey_FollowsMissingKeyBehavior(MissingResourceBehavior behavior, string expected)
    {
        var request = new LocalizationRequest(new FakeLocalizationService(), SomeAssembly, keyPrefix: "Notification_", missingKeyBehavior: behavior);
        var converter = new DynamicKeyLocalizationConverter(request);

        var result = converter.Convert(["Asm/Asm.Strings:Greeting", 0], typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void DynamicKeyLocalizationConverter_InvalidKey_ThrowException_Throws()
    {
        var request = new LocalizationRequest(new FakeLocalizationService(), SomeAssembly, keyPrefix: "Notification_",
            missingKeyBehavior: MissingResourceBehavior.ThrowException);
        var converter = new DynamicKeyLocalizationConverter(request);

        Assert.Throws<LocalizationConfigurationException>(
            () => converter.Convert(["Asm/Asm.Strings:Greeting", 0], typeof(string), null, CultureInfo.InvariantCulture));
    }

    private sealed class RestoreAccessor(Func<IServiceProvider> original) : IDisposable
    {
        public void Dispose() => LocalizationServiceLocator.ServiceProviderAccessor = original;
    }
}
