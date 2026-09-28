using System.Globalization;
using System.Reflection;

namespace Meteion.Toolkit.Localization.Abstractions;

public class LocalizationOptions
{
    /// <summary>
    /// Gets or sets the default assembly to use for resource lookups. If not set, the assembly of the calling code will be used.
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
