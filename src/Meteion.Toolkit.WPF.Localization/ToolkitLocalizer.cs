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
    public static string Get(string key, Assembly? resourceAssembly = null)
    {
        var loc = LocalizationServiceLocator.Resolve<ILocalizationService>();
        return loc.GetString(key, resourceAssembly ?? Assembly.GetCallingAssembly());
    }

    public static CultureInfo CurrentCulture
    {
        get => LocalizationServiceLocator.Resolve<ILocalizationService>().CurrentCulture;
        set => LocalizationServiceLocator.Resolve<ILocalizationService>().CurrentCulture = value;
    }
}