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
    /// <param name="key">The parsed key to look up.</param>
    /// <param name="resourceAssembly">The assembly containing the resx to search.</param>
    /// <param name="culture">The culture whose translation is wanted.</param>
    /// <returns>The localized string, or null if the key has no value.</returns>
    string? GetLocalizedString(LocalizationKey key, Assembly resourceAssembly, CultureInfo culture);

    /// <summary>
    /// Every string key in every resx embedded in <paramref name="resourceAssembly"/>, in qualified form.
    /// </summary>
    /// <param name="resourceAssembly">The assembly whose embedded resx files are enumerated.</param>
    /// <returns>The qualified keys found in the assembly.</returns>
    IEnumerable<string> GetAvailableKeys(Assembly resourceAssembly);
}
