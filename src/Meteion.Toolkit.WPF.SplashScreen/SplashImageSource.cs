using System.IO;
using System.Reflection;

namespace Meteion.Toolkit.WPF.SplashScreen;

/// <summary>
/// Resolves where the splash PNG comes from. Resolution (finding the resource or file) happens synchronously so
/// configuration mistakes throw from <see cref="SplashScreenBuilder.Show"/>; the returned opener is called later,
/// on the splash thread, to read the bytes.
/// </summary>
internal static class SplashImageSource
{
    /// <summary>Finds an embedded resource and returns a function that opens it.</summary>
    /// <param name="fileName">The resource name, or a unique trailing part of it.</param>
    /// <param name="assembly">The assembly to search, or <c>null</c> for the entry assembly.</param>
    /// <returns>A function that opens a new stream over the resource each time it is called.</returns>
    public static Func<Stream> ResolveEmbeddedResource(string fileName, Assembly? assembly)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        assembly ??= Assembly.GetEntryAssembly()
            ?? throw new InvalidOperationException("There is no entry assembly to search for the splash image; pass an Assembly explicitly.");

        var names = assembly.GetManifestResourceNames();
        var resourceName = Match(names, fileName, assembly);

        return () => assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The embedded resource '{resourceName}' could not be opened from assembly '{assembly.GetName().Name}'.");
    }

    /// <summary>Verifies that an image file exists and returns a function that opens it.</summary>
    /// <param name="fileName">An absolute path, or one relative to the app's directory.</param>
    /// <returns>A function that opens a new read-only stream over the file each time it is called.</returns>
    public static Func<Stream> ResolveFilesystem(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        // Relative paths resolve against the app's directory, never the current working directory.
        var path = Path.GetFullPath(fileName, AppContext.BaseDirectory);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"The splash image file was not found: '{path}'.", path);
        }

        return () => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    /// <summary>Picks the manifest resource name that corresponds to <paramref name="fileName"/>.</summary>
    /// <param name="resourceNames">All manifest resource names in the assembly.</param>
    /// <param name="fileName">The requested name; an exact match wins over a <c>.{fileName}</c> suffix match.</param>
    /// <param name="assembly">The assembly searched, used only in error messages.</param>
    /// <returns>The full manifest resource name.</returns>
    /// <exception cref="InvalidOperationException">There is no match, or the suffix match is ambiguous.</exception>
    internal static string Match(string[] resourceNames, string fileName, Assembly assembly)
    {
        if (Array.IndexOf(resourceNames, fileName) >= 0)
        {
            return fileName;
        }

        var suffix = "." + fileName;
        var candidates = resourceNames.Where(n => n.EndsWith(suffix, StringComparison.Ordinal)).ToArray();

        return candidates.Length switch
        {
            1 => candidates[0],
            0 => throw new InvalidOperationException(
                $"No embedded resource matching '{fileName}' was found in assembly '{assembly.GetName().Name}'. " +
                $"Is its Build Action set to EmbeddedResource? Available resources: {FormatList(resourceNames)}."),
            _ => throw new InvalidOperationException(
                $"'{fileName}' matches more than one embedded resource in assembly '{assembly.GetName().Name}'; use a longer name. " +
                $"Candidates: {FormatList(candidates)}."),
        };
    }

    private static string FormatList(string[] names) => names.Length == 0 ? "(none)" : string.Join(", ", names.Select(n => $"'{n}'"));
}
