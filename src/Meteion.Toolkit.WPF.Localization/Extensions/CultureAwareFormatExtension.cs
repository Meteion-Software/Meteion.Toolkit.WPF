using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Converters;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Meteion.Toolkit.WPF.Localization.Extensions;

/// <summary>
/// Formats a bound number/date/currency value using
/// <see cref="ILocalizationService.CurrentCulture"/>, live: unlike a plain
/// <c>{Binding ..., StringFormat=...}</c> (which only reformats when the bound value itself
/// changes — see <see cref="CultureAwareFormatConverter"/>'s remarks), the result re-formats
/// immediately whenever <see cref="ILocalizationService.CurrentCulture"/> changes too, the same
/// way <see cref="LocalizedValueExtension"/> keeps a resolved resx string live.
/// </summary>
/// <example>
/// <c>&lt;TextBlock Text="{lx:CultureAwareFormat Value={Binding Total}, FormatString=C}" /&gt;</c>
/// </example>
[MarkupExtensionReturnType(typeof(object))]
public class CultureAwareFormatExtension : MarkupExtension
{
    /// <summary>The binding supplying the <see cref="IFormattable"/> value to format.</summary>
    public BindingBase? Value { get; set; }

    /// <summary>The standard/custom .NET format string (e.g. "C", "N2", "d") to apply.</summary>
    public string? FormatString { get; set; }

    public CultureAwareFormatExtension()
    {
    }

    public CultureAwareFormatExtension(BindingBase value) => Value = value;

    public CultureAwareFormatExtension(BindingBase value, string formatString)
    {
        Value = value;
        FormatString = formatString;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            return $"[{FormatString}]";
        }

        if (Value == null)
        {
            throw new LocalizationConfigurationException(
                $"{nameof(CultureAwareFormatExtension)}.{nameof(Value)} must be set to the binding " +
                "supplying the value to format.");
        }

        var loc = LocalizationServiceLocator.Resolve<ILocalizationService>();

        var multiBinding = new MultiBinding
        {
            Converter = new CultureAwareFormatConverter(loc, FormatString),
            Mode = BindingMode.OneWay,
        };
        multiBinding.Bindings.Add(Value);
        multiBinding.Bindings.Add(new Binding(nameof(CultureChangeTrigger.Value))
        {
            Source = new CultureChangeTrigger(loc),
            Mode = BindingMode.OneWay,
        });

        return multiBinding.ProvideValue(serviceProvider);
    }
}
