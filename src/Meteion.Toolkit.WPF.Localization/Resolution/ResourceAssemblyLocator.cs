using Meteion.Toolkit.Localization.Abstractions;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;

namespace Meteion.Toolkit.WPF.Localization.Resolution;

/// <summary>
/// Maps the assembly name carried by a qualified <see cref="LocalizationKey"/> (or a
/// <c>Source</c>) to the actual <see cref="Assembly"/>: an already-loaded assembly with that
/// simple name first, then <see cref="Assembly.Load(AssemblyName)"/>.
/// </summary>
internal static class ResourceAssemblyLocator
{
    private static readonly ConcurrentDictionary<string, Assembly> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static Assembly Find(string assemblyName) => Cache.GetOrAdd(assemblyName, static name =>
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (NameMatches(assembly, name))
            {
                return assembly;
            }
        }

        try
        {
            return Assembly.Load(new AssemblyName(name));
        }
        catch (Exception ex) when (ex is FileNotFoundException or FileLoadException or BadImageFormatException)
        {
            throw new LocalizationConfigurationException(
                $"Could not load resource assembly '{name}' named by a qualified localization key.", ex);
        }
    });

    public static bool NameMatches(Assembly assembly, string assemblyName)
        => string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase);
}
