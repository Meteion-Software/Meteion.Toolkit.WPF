using Microsoft.CodeAnalysis.CSharp;

namespace Meteion.Toolkit.Localization.KeysGenerator.Tests;

public class LocalizationKeysGeneratorTests
{
    private static string Resx(params (string Name, string Value, string? Type)[] entries)
    {
        var data = string.Join("\n", entries.Select(e =>
            e.Type is null
                ? $"""<data name="{e.Name}"><value>{e.Value}</value></data>"""
                : $"""<data name="{e.Name}" type="{e.Type}"><value>{e.Value}</value></data>"""));

        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <root>
            {data}
            </root>
            """;
    }

    [Fact]
    public void GeneratesConstForEachStringEntry_WithValueAsDocComment()
    {
        var resx = Resx(("Greeting", "Hello there!", null), ("Farewell", "Goodbye", null));

        var results = GeneratorTestHarness.Run([("Resources/Resources.resx", resx)]);

        var generated = Assert.Single(results).Value;
        Assert.Contains("""public const string Greeting = "TestAssembly/TestApp.Resources.Resources:Greeting";""", generated);
        Assert.Contains("""public const string Farewell = "TestAssembly/TestApp.Resources.Resources:Farewell";""", generated);
        Assert.Contains("Hello there!", generated);
        Assert.Contains("Goodbye", generated);
    }

    [Fact]
    public void GeneratesSyntacticallyValidSource_ForMultiLineValue()
    {
        // A resx <value> can legitimately span several lines (e.g. wizard instructions).
        // Regression test for a bug where those embedded newlines landed in the generated
        // .g.cs file without a "///" prefix on the continuation lines, falling out of the doc
        // comment and producing a wall of build errors from a single long string.
        var multiLineValue = "Instructions go here\nFirst paragraph.\nSecond paragraph, line one.\nSecond paragraph, line two.";
        var resx = Resx(("Instructions", multiLineValue, null));

        var results = GeneratorTestHarness.Run([("Resources/Resources.resx", resx)]);
        var generated = Assert.Single(results).Value;

        Assert.Contains("""public const string Instructions = "TestAssembly/TestApp.Resources.Resources:Instructions";""", generated);

        var tree = CSharpSyntaxTree.ParseText(generated);
        var errors = tree.GetDiagnostics()
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToList();
        Assert.True(errors.Count == 0, $"Generated source failed to parse:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}{Environment.NewLine}{Environment.NewLine}{generated}");
    }

    [Fact]
    public void SkipsCultureSuffixedResx()
    {
        var neutral = Resx(("Greeting", "Hello!", null));
        var satellite = Resx(("Greeting", "Bonjour!", null));

        var results = GeneratorTestHarness.Run(
        [
            ("Resources/Resources.resx", neutral),
            ("Resources/Resources.fr.resx", satellite),
        ]);

        // Only the neutral resx produces a keys class - the satellite is a translation of the
        // same key set, not a new source of truth.
        Assert.Single(results);
    }

    [Fact]
    public void SkipsNonStringResourceEntries()
    {
        var resx = Resx(
            ("Greeting", "Hello!", null),
            ("SomeIcon", "base64==", "System.Drawing.Bitmap, System.Drawing"));

        var results = GeneratorTestHarness.Run([("Resources.resx", resx)]);

        var generated = Assert.Single(results).Value;
        Assert.Contains("Greeting", generated);
        Assert.DoesNotContain("SomeIcon", generated);
    }

    [Fact]
    public void DisambiguatesKeysThatSanitizeToTheSameIdentifier()
    {
        var resx = Resx(("My.Key", "First", null), ("My_Key", "Second", null));

        var results = GeneratorTestHarness.Run([("Resources.resx", resx)]);

        var generated = Assert.Single(results).Value;
        Assert.Contains("""public const string My_Key = "TestAssembly/TestApp.Resources:My.Key";""", generated);
        Assert.Contains("""public const string My_Key_2 = "TestAssembly/TestApp.Resources:My_Key";""", generated);
    }

    [Fact]
    public void DefaultNamespaceCombinesRootNamespaceAndRelativeDirectory()
    {
        var resx = Resx(("Greeting", "Hello!", null));

        var results = GeneratorTestHarness.Run([("Resources/Resources.resx", resx)], rootNamespace: "MyApp");

        var generated = Assert.Single(results).Value;
        Assert.Contains("namespace MyApp.Resources", generated);
        Assert.Contains("public static partial class ResourcesKeys", generated);
    }

    [Fact]
    public void PerFileMetadataOverridesNamespaceAndClassName()
    {
        var resx = Resx(("Greeting", "Hello!", null));
        var metadata = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["Resources/Resources.resx"] = new Dictionary<string, string>
            {
                ["MeteionKeysNamespace"] = "Custom.Namespace",
                ["MeteionKeysClassName"] = "MyStrings",
            },
        };

        var results = GeneratorTestHarness.Run(
            [("Resources/Resources.resx", resx)], rootNamespace: "MyApp", perFileMetadata: metadata);

        var generated = Assert.Single(results).Value;
        Assert.Contains("namespace Custom.Namespace", generated);
        Assert.Contains("public static partial class MyStrings", generated);
    }

    [Fact]
    public void AppliesMarkerAttribute_WhenAbstractionsIsReferenced()
    {
        var resx = Resx(("Greeting", "Hello!", null));

        var results = GeneratorTestHarness.Run([("Resources.resx", resx)], referenceAbstractions: true);

        var generated = Assert.Single(results).Value;
        Assert.Contains("GeneratedLocalizationKeysAttribute", generated);
    }

    [Fact]
    public void OmitsMarkerAttribute_WhenAbstractionsIsNotReferenced()
    {
        var resx = Resx(("Greeting", "Hello!", null));

        var results = GeneratorTestHarness.Run([("Resources.resx", resx)], referenceAbstractions: false);

        var generated = Assert.Single(results).Value;
        Assert.DoesNotContain("GeneratedLocalizationKeysAttribute", generated);
        // The class itself is still generated even without the marker attribute available.
        Assert.Contains("""public const string Greeting = "TestAssembly/TestApp.Resources:Greeting";""", generated);
    }

    [Fact]
    public void EmitsResxSourceConstantAndMarkerProperty()
    {
        var resx = Resx(("Greeting", "Hello!", null));

        var results = GeneratorTestHarness.Run([("Properties/Strings.resx", resx)], rootNamespace: "MyApp");

        var generated = Assert.Single(results).Value;
        Assert.Contains("""public const string ResxSource = "TestAssembly/MyApp.Properties.Strings";""", generated);
        Assert.Contains("""ResxSource = "TestAssembly/MyApp.Properties.Strings")]""", generated);
        Assert.Contains("""public const string Greeting = "TestAssembly/MyApp.Properties.Strings:Greeting";""", generated);
    }

    [Fact]
    public void BaseNameSanitizesDirectorySegmentsButNotFileName()
    {
        var resx = Resx(("Greeting", "Hello!", null));

        var results = GeneratorTestHarness.Run([("My Folder/2024/Strings-Main.resx", resx)], rootNamespace: "MyApp");

        var generated = Assert.Single(results).Value;
        Assert.Contains("""public const string ResxSource = "TestAssembly/MyApp.My_Folder._2024.Strings-Main";""", generated);
        Assert.Contains("public static partial class Strings_MainKeys", generated);
    }

    [Fact]
    public void ResourceBaseNameMetadataOverridesComputedBaseName()
    {
        var resx = Resx(("Greeting", "Hello!", null));
        var metadata = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["Resources/Resources.resx"] = new Dictionary<string, string>
            {
                ["MeteionResourceBaseName"] = "Custom.Logical.Name",
            },
        };

        var results = GeneratorTestHarness.Run([("Resources/Resources.resx", resx)], perFileMetadata: metadata);

        var generated = Assert.Single(results).Value;
        Assert.Contains("""public const string Greeting = "TestAssembly/Custom.Logical.Name:Greeting";""", generated);
        // Only the base name is overridden - namespace/class still follow the file's location.
        Assert.Contains("namespace TestApp.Resources", generated);
    }

    [Fact]
    public void KeyNamedResxSource_IsRenamedAndReportsMTKGEN001()
    {
        var resx = Resx(("ResxSource", "Source", null), ("Greeting", "Hello!", null));

        var (results, diagnostics) = GeneratorTestHarness.RunWithDiagnostics([("Resources.resx", resx)]);

        var generated = Assert.Single(results).Value;
        Assert.Contains("""public const string ResxSource = "TestAssembly/TestApp.Resources";""", generated);
        Assert.Contains("""public const string ResxSource_2 = "TestAssembly/TestApp.Resources:ResxSource";""", generated);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("MTKGEN001", diagnostic.Id);
        Assert.Equal(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("ResxSource_2", diagnostic.GetMessage());
    }

    // Compiles the generated source against the abstractions assembly, so a helper that calls
    // GetFormattedString is checked for real (overload resolution included), not just parsed.
    private static void AssertCompiles(string generated)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(p => Microsoft.CodeAnalysis.MetadataReference.CreateFromFile(p));
        var compilation = CSharpCompilation.Create(
            "GeneratedCheck",
            [CSharpSyntaxTree.ParseText(generated)],
            references,
            new CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: Microsoft.CodeAnalysis.NullableContextOptions.Enable));

        var errors = compilation.GetDiagnostics()
            .Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
            .ToList();
        Assert.True(errors.Count == 0, $"Generated source failed to compile:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}{Environment.NewLine}{generated}");
    }

    [Fact]
    public void FormattedEntry_GeneratesTypedFormatHelper()
    {
        var resx = Resx(("Greeting", "Hello {0}, you have {1:N0} items", null), ("Plain", "No placeholders", null));

        var generated = Assert.Single(GeneratorTestHarness.Run([("Resources.resx", resx)])).Value;

        Assert.Contains("public static string FormatGreeting(global::Meteion.Toolkit.Localization.Abstractions.ILocalizationService service, object? arg0, object? arg1)", generated);
        Assert.Contains("service.GetFormattedString(Greeting, new object?[] { arg0, arg1 });", generated);
        Assert.DoesNotContain("FormatPlain", generated);
        AssertCompiles(generated);
    }

    [Fact]
    public void FormattedEntry_ArgumentCountIsHighestIndexPlusOne()
    {
        var resx = Resx(("Skips", "{0} and {2}", null), ("Repeats", "{0} {0} {0}", null));

        var generated = Assert.Single(GeneratorTestHarness.Run([("Resources.resx", resx)])).Value;

        Assert.Contains("FormatSkips(global::Meteion.Toolkit.Localization.Abstractions.ILocalizationService service, object? arg0, object? arg1, object? arg2)", generated);
        Assert.Contains("FormatRepeats(global::Meteion.Toolkit.Localization.Abstractions.ILocalizationService service, object? arg0)", generated);
        AssertCompiles(generated);
    }

    [Fact]
    public void FormattedEntry_EscapedBracesAndInvalidFormat_GenerateNoHelper()
    {
        var resx = Resx(("Escaped", "Use {{0}} literally", null), ("Broken", "Oops {0", null));

        var generated = Assert.Single(GeneratorTestHarness.Run([("Resources.resx", resx)])).Value;

        Assert.DoesNotContain("FormatEscaped", generated);
        Assert.DoesNotContain("FormatBroken", generated);
        AssertCompiles(generated);
    }

    [Fact]
    public void FormattedEntry_HelperNameCollidingWithAKey_IsDisambiguated()
    {
        var resx = Resx(("Greeting", "Hi {0}", null), ("FormatGreeting", "Other", null));

        var generated = Assert.Single(GeneratorTestHarness.Run([("Resources.resx", resx)])).Value;

        Assert.Contains("public const string FormatGreeting =", generated);
        Assert.Contains("public static string FormatGreeting_2(", generated);
        AssertCompiles(generated);
    }

    [Fact]
    public void FormattedEntry_WithoutAbstractionsReference_GeneratesNoHelper()
    {
        var resx = Resx(("Greeting", "Hi {0}", null));

        var generated = Assert.Single(GeneratorTestHarness.Run([("Resources.resx", resx)], referenceAbstractions: false)).Value;

        Assert.DoesNotContain("FormatGreeting", generated);
    }}
