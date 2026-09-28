using Meteion.Toolkit.Localization.Abstractions;
using System.Globalization;
using System.Reflection;

namespace Meteion.Toolkit.WPF.Localization;


/// <summary>
/// Static convenience facade for retrieving localized strings from non-DI contexts
/// (value converters, static helpers) where constructor-injecting ILocalizationService
/// isn't possible. Routes through the same LocalizationServiceLocator seam
/// ToolkitLocalizationExtension uses. Call this directly — wrapping it in your own
/// helper method breaks the calling-assembly inference below.
///
/// Prefer constructor-injecting ILocalizationService wherever DI is available;
/// this facade is the documented exception, not the default.
/// </summary>
public static class ToolkitLocalizer
{
    /// <summary>
    /// Resolves <paramref name="key"/>. A qualified key (e.g. a generated keys constant) names
    /// its own assembly and resx; an unqualified key resolves against
    /// <paramref name="resourceAssembly"/>, or the calling assembly, when not given.
    /// </summary>
    public static string Get(string key, Assembly? resourceAssembly = null)
    {
        var loc = LocalizationServiceLocator.Resolve<ILocalizationService>();
        if (resourceAssembly is null && LocalizationKey.Parse(key).IsQualified)
        {
            return loc.GetString(key);
        }

        return loc.GetString(key, resourceAssembly ?? Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Resolves an unqualified <paramref name="key"/> from the resx named by <paramref name="source"/>
    /// (e.g. a generated keys class's <c>ResxSource</c> constant).
    /// </summary>
    public static string Get(string key, string source)
        => LocalizationServiceLocator.Resolve<ILocalizationService>().GetString(key, source);

    public static CultureInfo CurrentCulture
    {
        get => LocalizationServiceLocator.Resolve<ILocalizationService>().CurrentCulture;
        set => LocalizationServiceLocator.Resolve<ILocalizationService>().CurrentCulture = value;
    }
}