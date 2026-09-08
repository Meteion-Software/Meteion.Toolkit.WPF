using Meteion.Toolkit.Localization.Abstractions;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Meteion.Toolkit.WPF.Localization.Converters;

/// <summary>
/// Formats an <see cref="IFormattable"/> value (numbers, dates, currency, ...) using
/// <see cref="ILocalizationService.CurrentCulture"/>, instead of the culture WPF's binding
/// pipeline would otherwise supply — which, absent an explicit
/// <see cref="Binding.ConverterCulture"/>, falls back to the target element's
/// <see cref="FrameworkElement.Language"/> (defaulting to "en-US") rather than the app's
/// selected language. See <see cref="FrameworkLanguageSynchronizer"/> for the other half of
/// that fix.
/// </summary>
/// <remarks>
/// Implements both <see cref="IValueConverter"/> and <see cref="IMultiValueConverter"/>:
/// use it directly as a <c>Binding.Converter</c> for a one-off, non-live conversion (it still
/// reads whatever <see cref="ILocalizationService.CurrentCulture"/> is current each time the
/// bound value changes), or let <see cref="Extensions.CultureAwareFormatExtension"/> drive it
/// as a <see cref="MultiBinding.Converter"/> for a result that also re-formats immediately when
/// the culture itself changes.
/// </remarks>
[ValueConversion(typeof(IFormattable), typeof(string))]
public class CultureAwareFormatConverter : IValueConverter, IMultiValueConverter
{
    private readonly ILocalizationService? _service;

    /// <summary>
    /// Parameterless constructor for direct XAML usage, e.g.
    /// <c>&lt;lx:CultureAwareFormatConverter x:Key="..." FormatString="C"/&gt;</c> — resolves
    /// the service lazily via <see cref="LocalizationServiceLocator"/>, the same seam
    /// <see cref="ToolkitLocalizer"/> uses outside DI.
    /// </summary>
    public CultureAwareFormatConverter()
    {
    }

    internal CultureAwareFormatConverter(ILocalizationService service, string? formatString = null)
    {
        _service = service;
        FormatString = formatString;
    }

    /// <summary>
    /// The standard/custom .NET format string (e.g. "C", "N2", "d") applied to the value.
    /// Overridden per-binding by a non-null <c>ConverterParameter</c>, when one is supplied.
    /// </summary>
    public string? FormatString { get; set; }

    private ILocalizationService Service => _service ?? LocalizationServiceLocator.Resolve<ILocalizationService>();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Format(value, parameter as string ?? FormatString);

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        Format(values.Length > 0 ? values[0] : null, parameter as string ?? FormatString);

    private object Format(object? value, string? format) =>
        value is IFormattable formattable
            ? formattable.ToString(format, Service.CurrentCulture)
            : DependencyProperty.UnsetValue;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException($"{nameof(CultureAwareFormatConverter)} only supports one-way binding.");

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException($"{nameof(CultureAwareFormatConverter)} only supports one-way binding.");
}
