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
