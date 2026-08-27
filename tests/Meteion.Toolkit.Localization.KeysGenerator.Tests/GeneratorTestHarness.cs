using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Meteion.Toolkit.Localization.KeysGenerator.Tests;

/// <summary>
/// Minimal hand-rolled harness for driving <see cref="LocalizationKeysGenerator"/> against an
/// in-memory compilation, without pulling in a full Roslyn testing package - just enough
/// scaffolding (a fake AdditionalText, a fake AnalyzerConfigOptionsProvider) to exercise the
/// generator's actual public entry points the same way the real MSBuild/IDE host would.
/// </summary>
internal static class GeneratorTestHarness
{
    /// <summary>
    /// Runs the generator over <paramref name="resxFiles"/> (path -> resx content) and returns
    /// every generated source file, keyed by hint name.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Run(
        IEnumerable<(string Path, string Contents)> resxFiles,
        string? rootNamespace = "TestApp",
        IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? perFileMetadata = null,
        bool referenceAbstractions = true)
    {
        var additionalTexts = resxFiles
            .Select(f => new TestAdditionalText(f.Path, f.Contents))
            .ToArray();

        var globalOptions = new TestAnalyzerConfigOptions(
            rootNamespace is null
                ? new Dictionary<string, string>()
                : new Dictionary<string, string> { ["build_property.RootNamespace"] = rootNamespace });

        var optionsProvider = new TestAnalyzerConfigOptionsProvider(globalOptions, additionalTexts, perFileMetadata);

        var abstractionsPath = typeof(Meteion.Toolkit.Localization.Abstractions.GeneratedLocalizationKeysAttribute).Assembly.Location;

        // The test host's own trusted-platform-assemblies list already includes
        // Abstractions.dll (it's a ProjectReference of this test project, needed for the
        // typeof() above) - exclude it here so referenceAbstractions:false actually means the
        // compilation-under-test can't see it, rather than picking it up unconditionally.
        var references = GetTrustedPlatformAssemblyPaths()
            .Where(p => !string.Equals(p, abstractionsPath, StringComparison.OrdinalIgnoreCase))
            .Select(p => MetadataReference.CreateFromFile(p))
            .ToList();

        if (referenceAbstractions)
        {
            references.Add(MetadataReference.CreateFromFile(abstractionsPath));
        }

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new LocalizationKeysGenerator();
        var driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: additionalTexts,
            optionsProvider: optionsProvider);

        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();

        return runResult.Results
            .SelectMany(r => r.GeneratedSources)
            .ToDictionary(s => s.HintName, s => s.SourceText.ToString());
    }

    private static IEnumerable<string> GetTrustedPlatformAssemblyPaths()
    {
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (trustedAssemblies is null)
        {
            yield break;
        }

        foreach (var path in trustedAssemblies.Split(Path.PathSeparator))
        {
            if (File.Exists(path))
            {
                yield return path;
            }
        }
    }

    private sealed class TestAdditionalText(string path, string contents) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(contents);
    }

    private sealed class TestAnalyzerConfigOptions(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, out string value)
        {
            if (values.TryGetValue(key, out var found))
            {
                value = found;
                return true;
            }

            value = null!;
            return false;
        }
    }

    private sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly Dictionary<string, AnalyzerConfigOptions> _perAdditionalTextOptions = [];

        public TestAnalyzerConfigOptionsProvider(
            AnalyzerConfigOptions globalOptions,
            IReadOnlyList<AdditionalText> additionalTexts,
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>? perFileMetadata)
        {
            GlobalOptions = globalOptions;

            foreach (var text in additionalTexts)
            {
                var relativeDir = System.IO.Path.GetDirectoryName(text.Path) ?? string.Empty;
                var values = new Dictionary<string, string>
                {
                    ["build_metadata.AdditionalFiles.RelativeDir"] = relativeDir.Length == 0 ? "" : relativeDir + System.IO.Path.DirectorySeparatorChar,
                };

                if (perFileMetadata?.TryGetValue(text.Path, out var overrides) == true)
                {
                    foreach (var kvp in overrides)
                    {
                        values[$"build_metadata.AdditionalFiles.{kvp.Key}"] = kvp.Value;
                    }
                }

                _perAdditionalTextOptions[text.Path] = new TestAnalyzerConfigOptions(values);
            }
        }

        public override AnalyzerConfigOptions GlobalOptions { get; }

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) =>
            _perAdditionalTextOptions.TryGetValue(textFile.Path, out var options) ? options : GlobalOptions;
    }
}
