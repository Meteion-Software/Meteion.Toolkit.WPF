using Meteion.Toolkit.WPF.Localization.Extensions;
using Meteion.Toolkit.WPF.Localization.Tests.Fakes;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

namespace Meteion.Toolkit.WPF.Localization.Tests.Extensions;

/// <summary>
/// Regression coverage for the DependencyProperty-vs-plain-CLR-property split in
/// LocalizedValueTargetBinder — this is the exact area that produced the
/// "'System.Windows.Data.Binding' is not a valid value for property 'Text'" crash
/// when Run.Text (not a DependencyProperty) was bound directly.
/// </summary>
public class LocalizedValueTargetBinderTests
{
    private static readonly System.Reflection.Assembly SomeAssembly = typeof(LocalizedValueTargetBinderTests).Assembly;

    // Constructing a real FrameworkElement (TextBlock) requires an STA thread.
    [StaFact]
    public void Bind_DependencyPropertyTarget_PushesValueAndUpdatesLiveOnCultureChange()
    {
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var proxy = new ToolkitLocalizationProxy(service, "Greeting", SomeAssembly);
        var binding = new Binding(nameof(ToolkitLocalizationProxy.Value)) { Source = proxy };

        var textBlock = new TextBlock();
        var initialReturn = LocalizedValueTargetBinder.Bind(textBlock, TextBlock.TextProperty, binding);

        Assert.Equal("Hello", initialReturn);
        Assert.Equal("Hello", textBlock.Text);

        service.ValueToReturn = "Bonjour";
        service.RaiseCultureChanged(new CultureInfo("fr-CA"));

        Assert.Equal("Bonjour", textBlock.Text);
    }

    [Fact]
    public void Bind_PlainClrPropertyTarget_PushesValueViaReflectionAndUpdatesLiveOnCultureChange()
    {
        // Run.Text is a plain CLR property, not a DependencyProperty — WPF binding can
        // never target it directly. This is exactly the case the binder exists for.
        var service = new FakeLocalizationService { ValueToReturn = "Hello" };
        var proxy = new ToolkitLocalizationProxy(service, "Greeting", SomeAssembly);
        var binding = new Binding(nameof(ToolkitLocalizationProxy.Value)) { Source = proxy };

        var run = new Run();
        var textProperty = typeof(Run).GetProperty(nameof(Run.Text))!;
        var initialReturn = LocalizedValueTargetBinder.Bind(run, textProperty, binding);

        Assert.Equal("Hello", initialReturn);
        Assert.Equal("Hello", run.Text);

        service.ValueToReturn = "Bonjour";
        service.RaiseCultureChanged(new CultureInfo("fr-CA"));

        Assert.Equal("Bonjour", run.Text);
    }

    [Fact]
    public void Bind_SourceValueIsNull_PushesEmptyStringRatherThanNull()
    {
        var run = new Run();
        var textProperty = typeof(Run).GetProperty(nameof(Run.Text))!;
        var binding = new Binding(nameof(NullableValueSource.Value)) { Source = new NullableValueSource() };

        LocalizedValueTargetBinder.Bind(run, textProperty, binding);

        Assert.Equal(string.Empty, run.Text);
    }

    private sealed class NullableValueSource : System.ComponentModel.INotifyPropertyChanged
    {
        public string? Value => null;

        // Required by INotifyPropertyChanged for Binding to accept this as a source;
        // never raised since this source's value never changes.
#pragma warning disable CS0067
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067
    }
}
