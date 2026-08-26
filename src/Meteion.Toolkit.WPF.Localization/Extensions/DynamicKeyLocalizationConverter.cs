using Meteion.Toolkit.Localization.Abstractions;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Localization.Extensions;

/// <summary>
/// <see cref="MultiBinding.Converter"/> that resolves a <see cref="LocalizedValueExtension.KeyBinding"/>
/// value into localized text. Combined with a <see cref="CultureChangeTrigger"/> as the second
/// input, so the result re-resolves whenever either the bound key or the active culture changes.
/// </summary>
internal sealed class DynamicKeyLocalizationConverter(ILocalizationService service, Assembly assembly, string? keyPrefix = null) : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        var key = values.Length > 0 ? values[0] as string : null;
        return key == null ? string.Empty : service.GetString(keyPrefix + key, assembly);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException($"{nameof(DynamicKeyLocalizationConverter)} only supports one-way binding.");
}
