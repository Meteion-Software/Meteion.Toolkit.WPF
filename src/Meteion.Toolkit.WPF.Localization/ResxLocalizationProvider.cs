using Meteion.Toolkit.Localization.Abstractions;
using Microsoft.Extensions.Options;
using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Meteion.Toolkit.WPF.Localization;

internal sealed class ResxLocalizationProvider(IOptions<LocalizationOptions> options) : ILocalizationProvider
{
    private readonly ConcurrentDictionary<Assembly, ResourceManager> _managers = new();
    private readonly LocalizationOptions _options = options.Value;

    public string? GetLocalizedString(string key, Assembly resourceAssembly, CultureInfo culture)
        => GetManager(resourceAssembly).GetString(key, culture);

    public IEnumerable<string> GetAvailableKeys(Assembly resourceAssembly)
    {
        var set = GetManager(resourceAssembly).GetResourceSet(CultureInfo.InvariantCulture, true, true);
        return set?.Cast<DictionaryEntry>().Select(e => (string)e.Key) ?? Enumerable.Empty<string>();
    }

    private ResourceManager GetManager(Assembly assembly)
        => _managers.GetOrAdd(assembly, asm =>
        {
            if (_options.ResourceBaseNameSelector is { } selector)
            {
                return new ResourceManager(selector(asm), asm);
            }

            var names = asm.GetManifestResourceNames()
                .Where(n => n.EndsWith(".resources", StringComparison.Ordinal) && !n.EndsWith(".g.resources", StringComparison.Ordinal))
                .ToArray();

            if (names.Length == 0)
            {
                throw new LocalizationConfigurationException(
                    $"Assembly '{asm.GetName().Name}' has no embedded .resources files. " +
                    "Add a .resx file, or configure LocalizationOptions.ResourceBaseNameSelector.");
            }

            if (names.Length > 1)
            {
                throw new LocalizationConfigurationException(
                    $"Assembly '{asm.GetName().Name}' has multiple embedded .resources files " +
                    $"({string.Join(", ", names)}) — configure LocalizationOptions.ResourceBaseNameSelector to disambiguate.");
            }

            return new ResourceManager(names[0][..^".resources".Length], asm);
        });
}
