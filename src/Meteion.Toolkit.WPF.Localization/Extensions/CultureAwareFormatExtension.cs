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

    /// <summary>
    /// Several bindings to format together with a composite <see cref="FormatString"/> such as
    /// <c>"{0:C} of {1:C}"</c> - each child binding of this <see cref="MultiBinding"/> is one
    /// placeholder, in order. Set with property-element syntax. Can't be combined with
    /// <see cref="Value"/>.
    /// </summary>
    public MultiBinding? Values { get; set; }

    /// <summary>Creates the extension with <see cref="Value"/> to be set by the caller.</summary>
    public CultureAwareFormatExtension()
    {
    }

    /// <summary>Creates the extension for the given value binding.</summary>
    /// <param name="value">The binding supplying the value to format.</param>
    public CultureAwareFormatExtension(BindingBase value) => Value = value;

    /// <summary>Creates the extension for the given value binding and format string.</summary>
    /// <param name="value">The binding supplying the value to format.</param>
    /// <param name="formatString">The standard/custom .NET format string to apply.</param>
    public CultureAwareFormatExtension(BindingBase value, string formatString)
    {
        Value = value;
        FormatString = formatString;
    }

    /// <summary>
    /// Builds a multi-binding that formats <see cref="Value"/> and re-runs on culture changes.
    /// </summary>
    /// <param name="serviceProvider">The XAML service provider for the current usage.</param>
    /// <returns>The binding expression for the target, or a placeholder at design time.</returns>
    /// <exception cref="LocalizationConfigurationException"><see cref="Value"/> was not set.</exception>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            return $"[{FormatString}]";
        }

        if (Values != null)
        {
            return ProvideCompositeValue(serviceProvider);
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

    /// <summary>
    /// The <see cref="Values"/> path: formats several bindings with one composite format string.
    /// </summary>
    /// <param name="serviceProvider">The XAML service provider for the current usage.</param>
    /// <returns>The binding expression for the target.</returns>
    /// <exception cref="LocalizationConfigurationException">
    /// <see cref="Value"/> is also set, or <see cref="FormatString"/> isn't.
    /// </exception>
    private object ProvideCompositeValue(IServiceProvider serviceProvider)
    {
        if (Value != null)
        {
            throw new LocalizationConfigurationException(
                $"{nameof(CultureAwareFormatExtension)}.{nameof(Value)} and .{nameof(Values)} can't both be set - " +
                $"use {nameof(Values)} with a composite {nameof(FormatString)}, or {nameof(Value)} on its own.");
        }

        if (string.IsNullOrEmpty(FormatString))
        {
            throw new LocalizationConfigurationException(
                $"{nameof(CultureAwareFormatExtension)}.{nameof(FormatString)} must be set to a composite " +
                $"format string (e.g. \"{{0:C}} of {{1:C}}\") when {nameof(Values)} is used.");
        }

        var loc = LocalizationServiceLocator.Resolve<ILocalizationService>();

        var multiBinding = new MultiBinding
        {
            Converter = new CultureAwareCompositeFormatConverter(loc, FormatString, LocalizationRequest.ResolveMissingKeyBehavior()),
            Mode = BindingMode.OneWay,
        };
        multiBinding.Bindings.Add(new Binding(nameof(CultureChangeTrigger.Value))
        {
            Source = new CultureChangeTrigger(loc),
            Mode = BindingMode.OneWay,
        });

        foreach (var value in Values!.Bindings)
        {
            multiBinding.Bindings.Add(value);
        }

        return multiBinding.ProvideValue(serviceProvider);
    }
}
