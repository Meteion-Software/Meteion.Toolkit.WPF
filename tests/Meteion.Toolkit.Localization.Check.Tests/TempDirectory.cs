namespace Meteion.Toolkit.Localization.Check.Tests;

/// <summary>
/// A scratch directory that is deleted when disposed. Tests write fixture .resx/.xaml files
/// into it rather than relying on embedded fixture files.
/// </summary>
internal sealed class TempDirectory : IDisposable
{
    public string Path { get; } = Directory.CreateTempSubdirectory("meteion-loc-check-tests-").FullName;

    public string WriteFile(string relativePath, string contents)
    {
        var fullPath = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, contents);
        return fullPath;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup - fine to leave scratch temp files behind if something's locked.
        }
    }
}
