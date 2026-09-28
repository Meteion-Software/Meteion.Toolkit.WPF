using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Extensions;
using Meteion.Toolkit.WPF.Localization.Tests.Fakes;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Localization.Tests.Extensions;

/// <summary>
/// "No key" handling for <see cref="LocalizedValueExtension.KeyBinding"/>: null and
/// <see cref="DependencyProperty.UnsetValue"/> / <see cref="Binding.DoNothing"/> mean "nothing to
/// look up yet" and render as an empty string - they are never a missing-key lookup, whatever
/// <see cref="MissingResourceBehavior"/> is configured. Only UnsetValue (a binding that failed to
/// resolve) is traced; null is legitimate and stays quiet. Empty strings are NOT special-cased.
/// </summary>
/// <remarks>
/// In the collection because the trace tests attach a listener to the process-wide
/// <see cref="PresentationTraceSources.DataBindingSource"/>.
/// </remarks>
[Collection(ServiceLocatorTestCollection.Name)]
public class DynamicKeyLocalizationConverterTests
{
    private static readonly System.Reflection.Assembly SomeAssembly = typeof(DynamicKeyLocalizationConverterTests).Assembly;

    private const string TraceMarker = "Meteion.Toolkit.WPF.Localization";

    private static DynamicKeyLocalizationConverter CreateConverter(
        FakeLocalizationService service,
        MissingResourceBehavior behavior,
        string? keyPrefix = null) =>
        new(new LocalizationRequest(service, SomeAssembly, keyPrefix: keyPrefix, missingKeyBehavior: behavior));

    public static TheoryData<MissingResourceBehavior> AllBehaviors => new()
    {
        MissingResourceBehavior.ThrowException,
        MissingResourceBehavior.ReturnKey,
        MissingResourceBehavior.ReturnEmptyString,
    };

    [Theory]
    [MemberData(nameof(AllBehaviors))]
    public void Convert_NullKey_ReturnsEmptyWithoutLookup(MissingResourceBehavior behavior)
    {
        var service = new FakeLocalizationService();
        var converter = CreateConverter(service, behavior, keyPrefix: "Notification_");

        var result = converter.Convert([null!, 0], typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(string.Empty, result);
        Assert.Equal(0, service.GetStringCallCount);
    }

    [Theory]
    [MemberData(nameof(AllBehaviors))]
    public void Convert_UnsetValueKey_ReturnsEmptyWithoutLookup(MissingResourceBehavior behavior)
    {
        var service = new FakeLocalizationService();
        var converter = CreateConverter(service, behavior, keyPrefix: "Notification_");

        var result = converter.Convert([DependencyProperty.UnsetValue, 0], typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(string.Empty, result);
        Assert.Equal(0, service.GetStringCallCount);
    }

    [Theory]
    [MemberData(nameof(AllBehaviors))]
    public void Convert_DoNothingKey_ReturnsEmptyWithoutLookup(MissingResourceBehavior behavior)
    {
        var service = new FakeLocalizationService();
        var converter = CreateConverter(service, behavior, keyPrefix: "Notification_");

        var result = converter.Convert([Binding.DoNothing, 0], typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(string.Empty, result);
        Assert.Equal(0, service.GetStringCallCount);
    }

    [Fact]
    public void Convert_NoValuesAtAll_ReturnsEmptyWithoutLookup()
    {
        var service = new FakeLocalizationService();
        var converter = CreateConverter(service, MissingResourceBehavior.ThrowException);

        var result = converter.Convert([], typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal(string.Empty, result);
        Assert.Equal(0, service.GetStringCallCount);
    }

    // Pins the decision to leave empty strings as ordinary lookups, so a typo'd or forgotten
    // key still surfaces through MissingKeyBehavior instead of quietly rendering blank.
    [Fact]
    public void Convert_EmptyStringKey_IsStillAnOrdinaryLookup()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Looked up" };
        var converter = CreateConverter(service, MissingResourceBehavior.ThrowException, keyPrefix: "Notification_");

        var result = converter.Convert(["", 0], typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Equal("Looked up", result);
        Assert.Equal(1, service.GetStringCallCount);
        Assert.Equal("Notification_", service.LastRequestedKey);
    }

    [Fact]
    public void Convert_UnsetValueKey_TracesOnce()
    {
        using var capture = new TraceCapture();
        var converter = CreateConverter(new FakeLocalizationService(), MissingResourceBehavior.ReturnKey);

        converter.Convert([DependencyProperty.UnsetValue, 0], typeof(string), null, CultureInfo.InvariantCulture);

        var message = Assert.Single(capture.Messages);
        Assert.Contains("UnsetValue", message);
    }

    [Fact]
    public void Convert_NullKey_DoesNotTrace()
    {
        using var capture = new TraceCapture();
        var converter = CreateConverter(new FakeLocalizationService(), MissingResourceBehavior.ReturnKey);

        converter.Convert([null!, 0], typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Empty(capture.Messages);
    }

    [Fact]
    public void Convert_ValidKey_DoesNotTrace()
    {
        using var capture = new TraceCapture();
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var converter = CreateConverter(service, MissingResourceBehavior.ThrowException);

        converter.Convert(["Greeting", 0], typeof(string), null, CultureInfo.InvariantCulture);

        Assert.Empty(capture.Messages);
    }

    // A key-source binding whose path doesn't resolve (renamed property, wrong DataContext type)
    // reaches the converter as UnsetValue. Uses the real MultiBinding rather than calling Convert.
    [StaFact]
    public void MultiBinding_KeyPathDoesNotResolve_RendersEmptyWithoutLookup()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var multiBinding = new MultiBinding
        {
            Converter = CreateConverter(service, MissingResourceBehavior.ReturnKey, keyPrefix: "Notification_"),
            Mode = BindingMode.OneWay,
        };
        multiBinding.Bindings.Add(new Binding("NoSuchProperty"));
        multiBinding.Bindings.Add(new Binding(nameof(CultureChangeTrigger.Value))
        {
            Source = new CultureChangeTrigger(service),
            Mode = BindingMode.OneWay,
        });
        var target = new TextBlock { DataContext = new KeySource { TitleKey = "Info" } };

        BindingOperations.SetBinding(target, TextBlock.TextProperty, multiBinding);

        Assert.Equal(string.Empty, target.Text);
        Assert.Equal(0, service.GetStringCallCount);
    }

    // FallbackValue / TargetNullValue on the KeyBinding are the supported way to supply a
    // placeholder *key* - they feed the key source, so the result still goes through the lookup
    // (and picks up KeyPrefix). Documented here for the direct-element path.
    [StaFact]
    public void DirectPath_TargetNullValue_SuppliesPlaceholderKey()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Unknown" };
        var proxy = new DynamicToolkitLocalizationProxy(service, SomeAssembly, keyPrefix: "Common_");
        var target = new TextBlock { DataContext = new KeySource { TitleKey = null } };

        DynamicKeyBinder.Bind(target, proxy, new Binding(nameof(KeySource.TitleKey)) { TargetNullValue = "Placeholder" });

        Assert.Equal("Placeholder", proxy.Key);
        Assert.Equal("Common_Placeholder", service.LastRequestedKey);
        Assert.Equal("Unknown", proxy.Value);
    }

    [StaFact]
    public void DirectPath_FallbackValue_SuppliesPlaceholderKeyWhenPathUnresolved()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Unknown" };
        var proxy = new DynamicToolkitLocalizationProxy(service, SomeAssembly, keyPrefix: "Common_");
        var target = new TextBlock { DataContext = null };

        DynamicKeyBinder.Bind(target, proxy, new Binding(nameof(KeySource.TitleKey)) { FallbackValue = "Placeholder" });

        Assert.Equal("Placeholder", proxy.Key);
        Assert.Equal("Common_Placeholder", service.LastRequestedKey);
    }

    [StaFact]
    public void DirectPath_NoDataContext_ProxyStaysEmptyWithoutLookup()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var proxy = new DynamicToolkitLocalizationProxy(service, SomeAssembly);
        var target = new TextBlock { DataContext = null };

        DynamicKeyBinder.Bind(target, proxy, new Binding(nameof(KeySource.TitleKey)));

        Assert.Null(proxy.Key);
        Assert.Equal(string.Empty, proxy.Value);
        Assert.Equal(0, service.GetStringCallCount);
    }

    private sealed class KeySource : INotifyPropertyChanged
    {
        public string? TitleKey { get; set; }

#pragma warning disable CS0067
        public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
    }

    /// <summary>
    /// Collects only this library's own trace output from WPF's binding-diagnostics source,
    /// ignoring anything WPF itself writes to it.
    /// </summary>
    private sealed class TraceCapture : TraceListener
    {
        private readonly SourceLevels _originalLevel;

        public TraceCapture()
        {
            var source = PresentationTraceSources.DataBindingSource;
            _originalLevel = source.Switch.Level;
            source.Switch.Level = SourceLevels.All;
            source.Listeners.Add(this);
        }

        public List<string> Messages { get; } = [];

        public override void Write(string? message) => Capture(message);

        public override void WriteLine(string? message) => Capture(message);

        public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? message)
            => Capture(message);

        public override void TraceEvent(TraceEventCache? eventCache, string source, TraceEventType eventType, int id, string? format, params object?[]? args)
            => Capture(args is { Length: > 0 } ? string.Format(format ?? string.Empty, args) : format);

        private void Capture(string? message)
        {
            if (message?.Contains(TraceMarker, StringComparison.Ordinal) == true)
            {
                Messages.Add(message);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var source = PresentationTraceSources.DataBindingSource;
                source.Listeners.Remove(this);
                source.Switch.Level = _originalLevel;
            }

            base.Dispose(disposing);
        }
    }
}
