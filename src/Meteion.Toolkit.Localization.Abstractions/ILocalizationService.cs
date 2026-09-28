using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Meteion.Toolkit.Localization.Abstractions;

public interface ILocalizationService : INotifyPropertyChanged
{
    /// <summary>
    /// Resolves a qualified key (see <see cref="LocalizationKey"/>) from the assembly and resx it
    /// names. An unqualified key falls back to <see cref="LocalizationOptions.DefaultAssembly"/>'s
    /// only resx.
    /// </summary>
    string GetString(string key);

    /// <summary>
    /// Resolves <paramref name="key"/> against <paramref name="resourceAssembly"/>'s only resx. A
    /// qualified key is also accepted, but must name <paramref name="resourceAssembly"/>.
    /// </summary>
    string GetString(string key, Assembly resourceAssembly);

    /// <summary>
    /// Resolves an unqualified <paramref name="key"/> from the resx named by
    /// <paramref name="source"/> (<c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;</c>, e.g. a
    /// generated keys class's <c>ResxSource</c> constant). A qualified key is an error.
    /// </summary>
    string GetString(string key, string source);

    CultureInfo CurrentCulture { get; set; }
    event EventHandler<CultureChangedEventArgs> CultureChanged;
}

public class CultureChangedEventArgs(CultureInfo culture) : EventArgs
{
    public CultureInfo Culture { get; } = culture;
}
