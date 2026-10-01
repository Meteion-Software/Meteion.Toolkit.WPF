using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Extensions;
using Meteion.Toolkit.WPF.Localization.Tests.Fakes;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Localization.Tests.Extensions;

/// <remarks>
/// <see cref="FakeLocalizationService"/> echoes the requested key when no
/// <see cref="FakeLocalizationService.ValueToReturn"/> is set, which lets these tests assert the
/// key (including any prefix) that actually reached the service. Design mode isn't covered, for
/// the same reason as on <see cref="LocalizedValueExtensionTests"/>.
/// </remarks>
[Collection(ServiceLocatorTestCollection.Name)]
public class LocalizedBindingExtensionTests
{
    private static readonly Assembly SomeAssembly = typeof(LocalizedBindingExtensionTests).Assembly;

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

    /// <summary>Applies the extension's result to a TextBlock the way XAML would for a BindingBase.</summary>
    private static TextBlock Attach(LocalizedBindingExtension extension, object? dataContext)
    {
        var result = extension.ProvideValue(new FakeProvideValueServiceProvider());
        var textBlock = new TextBlock { DataContext = dataContext };
        textBlock.SetBinding(TextBlock.TextProperty, Assert.IsAssignableFrom<BindingBase>(result));
        return textBlock;
    }

    [Fact]
    public void ProvideValue_NoKeyBinding_ThrowsConfigurationException()
    {
        using (UseFakeLocator(new FakeLocalizationService()))
        {
            var extension = new LocalizedBindingExtension();

            Assert.Throws<LocalizationConfigurationException>(
                () => extension.ProvideValue(new FakeProvideValueServiceProvider()));
        }
    }

    [Fact]
    public void ProvideValue_SourceAndAssemblyBothSet_ThrowsConfigurationException()
    {
        using (UseFakeLocator(new FakeLocalizationService()))
        {
            var extension = new LocalizedBindingExtension
            {
                KeyBinding = new Binding("Key"),
                Source = "Some.Assembly/Some.Strings",
                Assembly = SomeAssembly,
            };

            Assert.Throws<LocalizationConfigurationException>(
                () => extension.ProvideValue(new FakeProvideValueServiceProvider()));
        }
    }

    [Fact]
    public void ProvideValue_ReturnsOneWayMultiBindingWithKeyAndCultureTrigger()
    {
        using (UseFakeLocator(new FakeLocalizationService()))
        {
            var extension = new LocalizedBindingExtension { KeyBinding = new Binding("Key") };

            var result = Assert.IsType<MultiBinding>(extension.ProvideValue(new FakeProvideValueServiceProvider()));

            Assert.Equal(BindingMode.OneWay, result.Mode);
            Assert.IsType<DynamicKeyLocalizationConverter>(result.Converter);
            Assert.Equal(2, result.Bindings.Count);
        }
    }

    [StaFact]
    public void ProvideValue_BindsLiveToKeyAndCulture()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        using (UseFakeLocator(service))
        {
            var source = new KeySource { Key = "Greeting" };
            var textBlock = Attach(new LocalizedBindingExtension { KeyBinding = new Binding(nameof(KeySource.Key)) }, source);

            Assert.Equal("Hello", textBlock.Text);

            service.ValueToReturn = "Goodbye";
            source.Key = "Farewell";
            Assert.Equal("Goodbye", textBlock.Text);

            service.ValueToReturn = "Au revoir";
            service.RaiseCultureChanged(new CultureInfo("fr-CA"));
            Assert.Equal("Au revoir", textBlock.Text);
            Assert.Equal("Farewell", service.LastRequestedKey);
        }
    }

    [StaFact]
    public void ProvideValue_KeyPrefix_IsAppliedToBoundKey()
    {
        var service = new FakeLocalizationService();
        using (UseFakeLocator(service))
        {
            var textBlock = Attach(
                new LocalizedBindingExtension { KeyBinding = new Binding(nameof(KeySource.Key)), KeyPrefix = "Status_" },
                new KeySource { Key = "Active" });

            Assert.Equal("Status_Active", textBlock.Text);
        }
    }

    [StaFact]
    public void ProvideValue_Source_IsUsedForLookup()
    {
        var service = new FakeLocalizationService();
        using (UseFakeLocator(service))
        {
            Attach(
                new LocalizedBindingExtension { KeyBinding = new Binding(nameof(KeySource.Key)), Source = "Some.Assembly/Some.Strings" },
                new KeySource { Key = "Active" });

            Assert.Equal("Some.Assembly/Some.Strings", service.LastRequestedSource);
        }
    }

    [StaFact]
    public void ProvideValue_UnqualifiedKeyWithNoSource_ResolvesAgainstContextAssembly()
    {
        var service = new FakeLocalizationService();
        using (UseFakeLocator(service))
        {
            Attach(
                new LocalizedBindingExtension { KeyBinding = new Binding(nameof(KeySource.Key)) },
                new KeySource { Key = "Active" });

            Assert.Same(SomeAssembly, service.LastRequestedAssembly);
        }
    }

    [StaFact]
    public void ProvideValue_EnumKey_ResolvesViaToString()
    {
        var service = new FakeLocalizationService();
        using (UseFakeLocator(service))
        {
            var textBlock = Attach(
                new LocalizedBindingExtension { KeyBinding = new Binding(nameof(EnumSource.State)) },
                new EnumSource { State = RowState.Active });

            Assert.Equal("Active", textBlock.Text);
        }
    }

    [StaFact]
    public void ProvideValue_NullKey_RendersEmptyStringWithoutLookup()
    {
        var service = new FakeLocalizationService();
        using (UseFakeLocator(service))
        {
            var textBlock = Attach(
                new LocalizedBindingExtension { KeyBinding = new Binding(nameof(KeySource.Key)) },
                new KeySource { Key = null });

            Assert.Equal(string.Empty, textBlock.Text);
            Assert.Equal(0, service.GetStringCallCount);
        }
    }

    [StaFact]
    public void ProvideValue_TargetNullValue_IsLocalizedAsKey()
    {
        var service = new FakeLocalizationService();
        using (UseFakeLocator(service))
        {
            var textBlock = Attach(
                new LocalizedBindingExtension { KeyBinding = new Binding(nameof(KeySource.Key)), TargetNullValue = "Unknown" },
                new KeySource { Key = null });

            Assert.Equal("Unknown", service.LastRequestedKey);
            Assert.Equal("Unknown", textBlock.Text);
        }
    }

    [StaFact]
    public void ProvideValue_FallbackValue_IsLocalizedAsKey()
    {
        var service = new FakeLocalizationService();
        using (UseFakeLocator(service))
        {
            // The key path doesn't exist on the DataContext, so the binding fails and falls back.
            var textBlock = Attach(
                new LocalizedBindingExtension { KeyBinding = new Binding("NoSuchProperty"), FallbackValue = "Unknown" },
                new KeySource { Key = "Active" });

            Assert.Equal("Unknown", service.LastRequestedKey);
            Assert.Equal("Unknown", textBlock.Text);
        }
    }

    [StaFact]
    public void ProvideValue_FallbackAndNullValuesAlreadyOnKeyBinding_AreNotOverridden()
    {
        var service = new FakeLocalizationService();
        using (UseFakeLocator(service))
        {
            var keyBinding = new Binding(nameof(KeySource.Key)) { TargetNullValue = "FromBinding" };
            var textBlock = Attach(
                new LocalizedBindingExtension { KeyBinding = keyBinding, TargetNullValue = "FromExtension" },
                new KeySource { Key = null });

            Assert.Equal("FromBinding", textBlock.Text);
        }
    }

    [StaFact]
    public void ProvideValue_InsideDataGridColumn_ResolvesPerRow()
    {
        var service = new FakeLocalizationService();
        using (UseFakeLocator(service))
        {
            var extension = new LocalizedBindingExtension { KeyBinding = new Binding(nameof(KeySource.Key)), KeyPrefix = "Status_" };
            var column = new DataGridTextColumn
            {
                Binding = Assert.IsAssignableFrom<BindingBase>(extension.ProvideValue(new FakeProvideValueServiceProvider())),
            };

            var cell = BuildCell(column, new KeySource { Key = "Active" });

            // A real grid pumps the dispatcher, which is when the cell's binding activates.
            cell.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, () => { });

            Assert.Equal("Status_Active", cell.Text);
        }
    }

    // DataGridColumn.GenerateElement is protected; invoke it the way the grid would for a row.
    private static TextBlock BuildCell(DataGridTextColumn column, object item)
    {
        var method = typeof(DataGridTextColumn).GetMethod(
            "GenerateElement", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var cell = new DataGridCell { DataContext = item };
        var element = (TextBlock)method.Invoke(column, [cell, item])!;

        // The grid parents the generated element in the cell, so it inherits the row's DataContext.
        cell.Content = element;
        element.DataContext = item;
        return element;
    }

    // Parses real XAML, so the extension's name, constructor/property binding and nested
    // KeyBinding syntax are exercised the way a view would use them.
    [StaFact]
    public void Xaml_DataGridColumnWithLocalizedBinding_LoadsAndResolves()
    {
        var service = new FakeLocalizationService();
        using (UseFakeLocator(service))
        {
            const string xaml = """
                <DataGridTextColumn xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                                    xmlns:lx="http://wpf.meteion.ca/winfx/xaml/localization"
                                    Binding="{lx:LocalizedBinding KeyPrefix=Status_, KeyBinding={Binding Key}}" />
                """;

            var column = (DataGridTextColumn)System.Windows.Markup.XamlReader.Parse(xaml);
            var cell = BuildCell(column, new KeySource { Key = "Active" });
            cell.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, () => { });

            Assert.Equal("Status_Active", cell.Text);
        }
    }

    private enum RowState { Active }

    private sealed class EnumSource
    {
        public RowState State { get; init; }
    }

    private sealed class KeySource : INotifyPropertyChanged
    {
        private string? _key;

        public string? Key
        {
            get => _key;
            set
            {
                _key = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Key)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class RestoreAccessor(Func<IServiceProvider> original) : IDisposable
    {
        public void Dispose() => LocalizationServiceLocator.ServiceProviderAccessor = original;
    }
}
