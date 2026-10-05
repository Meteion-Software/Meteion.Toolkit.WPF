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
    /// <see cref="GetString(string)"/>, then applies <paramref name="args"/> to the result as a
    /// composite format string (e.g. <c>"Hello {0}, you have {1:N0} items"</c>) using
    /// <see cref="CurrentCulture"/>. A bad format string or too few arguments is handled per
    /// <see cref="LocalizationOptions.MissingKeyBehavior"/>: rethrown as a
    /// <see cref="LocalizationConfigurationException"/>, or returned as the unformatted text or an
    /// empty string.
    /// </summary>
    /// <remarks>
    /// This has its own name rather than being a <c>GetString</c> overload so a string argument
    /// can never be mistaken for the <c>source</c> parameter of <see cref="GetString(string, string)"/>.
    /// The same reason keeps the <c>Assembly</c> and <c>source</c> variants below from taking
    /// <see langword="params"/>: they take the arguments as an explicit array.
    /// </remarks>
    /// <param name="key">The qualified or unqualified key to resolve.</param>
    /// <param name="args">The values for the format string's placeholders.</param>
    /// <returns>The formatted string for <see cref="CurrentCulture"/>.</returns>
    string GetFormattedString(string key, params object?[] args);

    /// <summary>
    /// <see cref="GetString(string, Assembly)"/>, then applies <paramref name="args"/> as
    /// described on <see cref="GetFormattedString(string, object?[])"/>.
    /// </summary>
    /// <param name="key">The qualified or unqualified key to resolve.</param>
    /// <param name="resourceAssembly">The assembly whose resx supplies the value.</param>
    /// <param name="args">The values for the format string's placeholders.</param>
    /// <returns>The formatted string for <see cref="CurrentCulture"/>.</returns>
    string GetFormattedString(string key, Assembly resourceAssembly, object?[] args);

    /// <summary>
    /// <see cref="GetString(string, string)"/>, then applies <paramref name="args"/> as
    /// described on <see cref="GetFormattedString(string, object?[])"/>.
    /// </summary>
    /// <param name="key">The unqualified key to resolve.</param>
    /// <param name="source">The <c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;</c> identity of the resx to read.</param>
    /// <param name="args">The values for the format string's placeholders.</param>
    /// <returns>The formatted string for <see cref="CurrentCulture"/>.</returns>
    string GetFormattedString(string key, string source, object?[] args);

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
