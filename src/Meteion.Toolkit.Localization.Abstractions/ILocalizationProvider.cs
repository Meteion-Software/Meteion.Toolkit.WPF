using System.Globalization;
using System.Reflection;

namespace Meteion.Toolkit.Localization.Abstractions;

/// <summary>
/// Defines a contract for a localization provider singleton that keeps track of the current culture and provides localized strings based on a given key.
/// </summary>
public interface ILocalizationProvider
{
    /// <summary>
    /// Looks up <paramref name="key"/> in <paramref name="resourceAssembly"/> - in the resx named by
    /// <see cref="LocalizationKey.BaseName"/> for a qualified key, or the assembly's only resx for an
    /// unqualified one. The caller has already resolved the assembly (for a qualified key, the
    /// one matching <see cref="LocalizationKey.AssemblyName"/>).
    /// </summary>
    string? GetLocalizedString(LocalizationKey key, Assembly resourceAssembly, CultureInfo culture);

    /// <summary>
    /// Every string key in every resx embedded in <paramref name="resourceAssembly"/>, in qualified form.
    /// </summary>
    IEnumerable<string> GetAvailableKeys(Assembly resourceAssembly);
}
