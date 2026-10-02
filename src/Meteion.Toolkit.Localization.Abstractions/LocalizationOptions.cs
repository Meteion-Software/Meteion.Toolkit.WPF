using System.Globalization;
using System.Reflection;

namespace Meteion.Toolkit.Localization.Abstractions;

/// <summary>
/// Configuration for the localization service: default resource assembly, missing-key handling and default culture.
/// </summary>
public class LocalizationOptions
{
    /// <summary>
    /// Gets or sets the fallback assembly for resource lookups that name no assembly. It is used only
    /// when no assembly is set explicitly and none can be inferred from the XAML context; if it is
    /// also unset, an unqualified key without an assembly throws a <see cref="LocalizationConfigurationException"/>.
    /// </summary>
    public Assembly? DefaultAssembly { get; set; }

    /// <summary>
    /// Defines the behavior when a resource key is missing. The default behavior is to throw an exception.
    /// </summary>
    public MissingResourceBehavior MissingKeyBehavior { get; set; }

    /// <summary>
    /// The default culture to use. When set to null, the system's current culture will be used as the default.
    /// </summary>
    public CultureInfo? DefaultCulture { get; set; } = null;
}
