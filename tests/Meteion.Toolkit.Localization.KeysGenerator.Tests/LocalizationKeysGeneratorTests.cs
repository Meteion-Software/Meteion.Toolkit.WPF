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
        Assert.Contains("""public const string Greeting = "Greeting";""", generated);
        Assert.Contains("""public const string Farewell = "Farewell";""", generated);
        Assert.Contains("Hello there!", generated);
        Assert.Contains("Goodbye", generated);
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
        Assert.Contains("""public const string My_Key = "My.Key";""", generated);
        Assert.Contains("""public const string My_Key_2 = "My_Key";""", generated);
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
        Assert.Contains("""public const string Greeting = "Greeting";""", generated);
    }
}
