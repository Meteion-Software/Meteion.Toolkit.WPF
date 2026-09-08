using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Extensions;
using Meteion.Toolkit.WPF.Localization.Tests.Fakes;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Localization.Tests.Extensions;

/// <summary>
/// Design-time-mode ("[FormatString]" placeholder) is intentionally not covered here — see
/// the same caveat on <see cref="LocalizedValueExtensionTests"/>.
/// </summary>
[Collection(ServiceLocatorTestCollection.Name)]
public class CultureAwareFormatExtensionTests
{
    private static IDisposable UseFakeLocator(ILocalizationService service)
    {
        var original = LocalizationServiceLocator.ServiceProviderAccessor;
        var fakeProvider = new FakeServiceProvider().Add<ILocalizationService>(service);
        LocalizationServiceLocator.ServiceProviderAccessor = () => fakeProvider;
        return new RestoreAccessor(original);
    }

    [Fact]
    public void ProvideValue_NoValueBindingSet_ThrowsConfigurationException()
    {
        var service = new FakeLocalizationService();
        using (UseFakeLocator(service))
        {
            var extension = new CultureAwareFormatExtension { FormatString = "C" };

            Assert.Throws<LocalizationConfigurationException>(
                () => extension.ProvideValue(new FakeProvideValueServiceProvider()));
        }
    }

    [Fact]
    public void ProvideValue_ReturnsMultiBindingWithValueAndCultureChangeTriggerInputs()
    {
        var service = new FakeLocalizationService();
        using (UseFakeLocator(service))
        {
            var extension = new CultureAwareFormatExtension(new Binding("Total"), "C");

            var result = extension.ProvideValue(new FakeProvideValueServiceProvider());

            var multiBinding = Assert.IsType<MultiBinding>(result);
            Assert.Equal(2, multiBinding.Bindings.Count);
            Assert.Equal(BindingMode.OneWay, multiBinding.Mode);
        }
    }

    // Constructing a real FrameworkElement (TextBlock) requires an STA thread.
    [StaFact]
    public void ProvideValue_BoundValueAndCultureBothChange_ReformatsLive()
    {
        var service = new FakeLocalizationService { CurrentCulture = new CultureInfo("en-US") };
        using (UseFakeLocator(service))
        {
            var source = new AmountSource { Total = 1234.5m };
            var textBlock = new TextBlock { DataContext = source };
            var extension = new CultureAwareFormatExtension(new Binding(nameof(AmountSource.Total)), "N2");

            var binding = extension.ProvideValue(new FakeProvideValueServiceProvider());
            BindingOperations.SetBinding(textBlock, TextBlock.TextProperty, (System.Windows.Data.BindingBase)binding);

            Assert.Equal(1234.5m.ToString("N2", new CultureInfo("en-US")), textBlock.Text);

            // Source value changes...
            source.Total = 42m;
            Assert.Equal(42m.ToString("N2", new CultureInfo("en-US")), textBlock.Text);

            // ...and a culture change alone (no source change) also reformats.
            service.CurrentCulture = new CultureInfo("de-DE");
            service.RaiseCultureChanged(service.CurrentCulture);
            Assert.Equal(42m.ToString("N2", new CultureInfo("de-DE")), textBlock.Text);
        }
    }

    private sealed class AmountSource : System.ComponentModel.INotifyPropertyChanged
    {
        private decimal _total;

        public decimal Total
        {
            get => _total;
            set
            {
                _total = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Total)));
            }
        }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class RestoreAccessor(Func<IServiceProvider> original) : IDisposable
    {
        public void Dispose() => LocalizationServiceLocator.ServiceProviderAccessor = original;
    }
}
