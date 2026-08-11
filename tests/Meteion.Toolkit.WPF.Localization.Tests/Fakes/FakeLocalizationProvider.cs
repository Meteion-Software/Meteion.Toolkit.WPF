using Meteion.Toolkit.Localization.Abstractions;
using System.Globalization;
using System.Reflection;

namespace Meteion.Toolkit.WPF.Localization.Tests.Fakes;

/// <summary>
/// Fake ILocalizationProvider that returns a configured value (or null) for every key,
/// and records the arguments of the last call — lets LocalizationServiceTests assert
/// exactly what LocalizationService passed down without needing a real resx anywhere.
/// </summary>
public sealed class FakeLocalizationProvider : ILocalizationProvider
{
    public string? ValueToReturn { get; set; }

    public string? LastKey { get; private set; }
    public Assembly? LastAssembly { get; private set; }
    public CultureInfo? LastCulture { get; private set; }

    public string? GetLocalizedString(string key, Assembly resourceAssembly, CultureInfo culture)
    {
        LastKey = key;
        LastAssembly = resourceAssembly;
        LastCulture = culture;
        return ValueToReturn;
    }

    public IEnumerable<string> GetAvailableKeys(Assembly resourceAssembly) => Enumerable.Empty<string>();
}
