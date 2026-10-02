using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Meteion.Toolkit.Localization.Abstractions;

/// <summary>
/// Provides localized strings for the current culture and notifies listeners when the culture changes.
/// </summary>
public interface ILocalizationService : INotifyPropertyChanged
{
    /// <summary>
    /// Resolves a qualified key (see <see cref="LocalizationKey"/>) from the assembly and resx it
    /// names. An unqualified key falls back to <see cref="LocalizationOptions.DefaultAssembly"/>'s
    /// only resx.
    /// </summary>
    /// <param name="key">The qualified or unqualified key to resolve.</param>
    /// <returns>The localized string for <see cref="CurrentCulture"/>.</returns>
    string GetString(string key);

    /// <summary>
    /// Resolves <paramref name="key"/> against <paramref name="resourceAssembly"/>'s only resx. A
    /// qualified key is also accepted, but must name <paramref name="resourceAssembly"/>.
    /// </summary>
    /// <param name="key">The qualified or unqualified key to resolve.</param>
    /// <param name="resourceAssembly">The assembly whose resx supplies the value.</param>
    /// <returns>The localized string for <see cref="CurrentCulture"/>.</returns>
    string GetString(string key, Assembly resourceAssembly);

    /// <summary>
    /// Resolves an unqualified <paramref name="key"/> from the resx named by
    /// <paramref name="source"/> (<c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;</c>, e.g. a
    /// generated keys class's <c>ResxSource</c> constant). A qualified key is an error.
    /// </summary>
    /// <param name="key">The unqualified key to resolve.</param>
    /// <param name="source">The <c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;</c> identity of the resx to read.</param>
    /// <returns>The localized string for <see cref="CurrentCulture"/>.</returns>
    string GetString(string key, string source);

    /// <summary>
    /// The culture used for lookups. Setting a different culture raises <see cref="CultureChanged"/>;
    /// setting the current culture again does nothing.
    /// </summary>
    CultureInfo CurrentCulture { get; set; }

    /// <summary>
    /// Raised after <see cref="CurrentCulture"/> changes.
    /// </summary>
    event EventHandler<CultureChangedEventArgs> CultureChanged;
}

/// <summary>
/// Event data for <see cref="ILocalizationService.CultureChanged"/>.
/// </summary>
/// <param name="culture">The culture that is now current.</param>
public class CultureChangedEventArgs(CultureInfo culture) : EventArgs
{
    /// <summary>The culture that is now current.</summary>
    public CultureInfo Culture { get; } = culture;
}
