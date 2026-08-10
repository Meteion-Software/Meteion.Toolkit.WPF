using System.Globalization;
using System.Reflection;

namespace Meteion.Toolkit.Localization.Abstractions;

/// <summary>
/// Defines a contract for a localization provider singleton that keeps track of the current culture and provides localized strings based on a given key.
/// </summary>
public interface ILocalizationProvider
{
    string? GetLocalizedString(string key, Assembly resourceAssembly, CultureInfo culture);
    IEnumerable<string> GetAvailableKeys(Assembly resourceAssembly);
}
