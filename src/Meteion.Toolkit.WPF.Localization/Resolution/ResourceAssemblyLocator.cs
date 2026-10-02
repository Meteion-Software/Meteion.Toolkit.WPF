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

    /// <summary>
    /// Finds the assembly with the given simple name, caching the result.
    /// </summary>
    /// <param name="assemblyName">The assembly simple name, compared case-insensitively.</param>
    /// <returns>The matching assembly.</returns>
    /// <exception cref="LocalizationConfigurationException">The assembly is not loaded and cannot be loaded.</exception>
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

    /// <summary>
    /// Checks whether an assembly's simple name equals <paramref name="assemblyName"/>, ignoring case.
    /// </summary>
    /// <param name="assembly">The assembly to test.</param>
    /// <param name="assemblyName">The expected simple name.</param>
    /// <returns><see langword="true"/> when the names match.</returns>
    public static bool NameMatches(Assembly assembly, string assemblyName)
        => string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase);
}
