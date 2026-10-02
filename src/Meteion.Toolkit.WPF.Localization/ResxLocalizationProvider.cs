using Meteion.Toolkit.Localization.Abstractions;
using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Meteion.Toolkit.WPF.Localization;

internal sealed class ResxLocalizationProvider : ILocalizationProvider
{
    private const string ResourcesSuffix = ".resources";

    // A null base name is the "this assembly's only resx" fallback for unqualified keys.
    private readonly ConcurrentDictionary<(Assembly Assembly, string? BaseName), ResourceManager> _managers = new();

    public string? GetLocalizedString(LocalizationKey key, Assembly resourceAssembly, CultureInfo culture)
    {
        var manager = GetManager(resourceAssembly, key.BaseName, key);
        try
        {
            return manager.GetString(key.Key, culture);
        }
        catch (MissingManifestResourceException ex)
        {
            throw new LocalizationConfigurationException(
                $"Assembly '{resourceAssembly.GetName().Name}' has no embedded resx named '{key.BaseName}' " +
                $"(key '{key}'). Its embedded resx files are: {string.Join(", ", GetBaseNames(resourceAssembly))}. " +
                "If the generator computed the wrong name, set MeteionResourceBaseName metadata on the resx's AdditionalFiles item.",
                ex);
        }
    }

    public IEnumerable<string> GetAvailableKeys(Assembly resourceAssembly)
    {
        var assemblyName = resourceAssembly.GetName().Name!;

        foreach (var baseName in GetBaseNames(resourceAssembly))
        {
            var set = GetManager(resourceAssembly, baseName).GetResourceSet(CultureInfo.InvariantCulture, true, true);
            if (set is null)
            {
                continue;
            }

            foreach (DictionaryEntry entry in set)
            {
                if (entry.Value is string)
                {
                    yield return LocalizationKey.Qualified(assemblyName, baseName, (string)entry.Key).ToString();
                }
            }
        }
    }

    // key is only used to name the offending key if the assembly's only resx can't be determined.
    private ResourceManager GetManager(Assembly assembly, string? baseName, LocalizationKey? key = null)
        => _managers.GetOrAdd(
            (assembly, baseName),
            static (k, key) => new ResourceManager(k.BaseName ?? GetOnlyBaseName(k.Assembly, key), k.Assembly),
            key);

    private static string GetOnlyBaseName(Assembly assembly, LocalizationKey? key)
    {
        var names = GetBaseNames(assembly);
        var keyClause = key is null ? string.Empty : $" for key '{key}'";

        if (names.Length == 0)
        {
            throw new LocalizationConfigurationException(
                $"Assembly '{assembly.GetName().Name}' has no embedded .resources files{keyClause}. Add a .resx file.");
        }

        if (names.Length > 1)
        {
            throw new LocalizationConfigurationException(
                $"Assembly '{assembly.GetName().Name}' has multiple embedded .resources files " +
                $"({string.Join(", ", names)}), so the unqualified key '{key}' is ambiguous. Use a generated qualified key " +
                "(e.g. {x:Static strings:StringsKeys.MyKey}) or set LocalizedValue.Source.");
        }

        return names[0];
    }

    private static string[] GetBaseNames(Assembly assembly)
        => assembly.GetManifestResourceNames()
            .Where(n => n.EndsWith(ResourcesSuffix, StringComparison.Ordinal) && !n.EndsWith(".g.resources", StringComparison.Ordinal))
            .Select(n => n[..^ResourcesSuffix.Length])
            .ToArray();
}
