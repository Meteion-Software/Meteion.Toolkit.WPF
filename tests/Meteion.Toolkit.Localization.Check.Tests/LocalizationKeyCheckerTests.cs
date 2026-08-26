namespace Meteion.Toolkit.Localization.Check.Tests;

public class LocalizationKeyCheckerTests
{
    private static string Resx(params (string Name, string? Type)[] entries)
    {
        var dataElements = entries.Select(e =>
            e.Type is null
                ? $"""<data name="{e.Name}" xml:space="preserve"><value>value</value></data>"""
                : $"""<data name="{e.Name}" type="{e.Type}"><value>AAA=</value></data>""");

        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <root>
                {string.Join(Environment.NewLine, dataElements)}
            </root>
            """;
    }

    [Fact]
    public void MissingKey_IsReported_WhenSatelliteLacksNeutralKey()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("Greeting", null), ("Farewell", null)));
        dir.WriteFile("Resources.ja-JP.resx", Resx(("Greeting", null)));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        var issue = Assert.Single(result.ResourceIssues);
        Assert.Equal(LocalizationKeyIssueKind.MissingKey, issue.Kind);
        Assert.Equal("Farewell", issue.Key);
        Assert.Equal("ja-JP", issue.CultureName);
    }

    [Fact]
    public void NoIssues_WhenKeysMatch()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("Greeting", null)));
        dir.WriteFile("Resources.ja-JP.resx", Resx(("Greeting", null)));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        Assert.True(result.IsClean);
    }

    [Fact]
    public void OrphanKey_IsReported_ByDefault_WhenSatelliteHasExtraKey()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("Greeting", null)));
        dir.WriteFile("Resources.ja-JP.resx", Resx(("Greeting", null), ("LeftoverTypo", null)));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        var issue = Assert.Single(result.ResourceIssues);
        Assert.Equal(LocalizationKeyIssueKind.OrphanKey, issue.Kind);
        Assert.Equal("LeftoverTypo", issue.Key);
    }

    [Fact]
    public void OrphanKey_NotReported_WhenDisabled()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("Greeting", null)));
        dir.WriteFile("Resources.ja-JP.resx", Resx(("Greeting", null), ("LeftoverTypo", null)));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path, new LocalizationCheckOptions { CheckOrphanKeys = false });

        Assert.Empty(result.ResourceIssues);
    }

    [Fact]
    public void NonStringResources_AreExcludedFromComparison()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("Greeting", null), ("SomeIcon", "System.Drawing.Bitmap, System.Drawing")));
        dir.WriteFile("Resources.ja-JP.resx", Resx(("Greeting", null)));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        // "SomeIcon" is a non-string resource and must not be reported as a missing key
        // just because the satellite (which only ever carries strings) doesn't have it.
        Assert.True(result.IsClean);
    }

    [Fact]
    public void MimetypeOnlyResources_AreAlsoExcludedFromComparison()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", $"""
            <?xml version="1.0" encoding="utf-8"?>
            <root>
                <data name="Greeting" xml:space="preserve"><value>value</value></data>
                <data name="SerializedBlob" mimetype="application/x-microsoft.net.object.binary.base64"><value>AAA=</value></data>
            </root>
            """);
        dir.WriteFile("Resources.ja-JP.resx", Resx(("Greeting", null)));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        // "SerializedBlob" has no "type" attribute, only "mimetype" - still a non-string
        // resource (a BinaryFormatter-serialized object) and must not be reported.
        Assert.True(result.IsClean);
    }

    [Fact]
    public void BinAndObjDirectories_AreExcludedByDefault()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("Greeting", null), ("Farewell", null)));
        dir.WriteFile("Resources.ja-JP.resx", Resx(("Greeting", null)));
        dir.WriteFile("bin/Debug/Resources.resx", Resx(("Greeting", null), ("Farewell", null)));
        dir.WriteFile("bin/Debug/Resources.ja-JP.resx", Resx(("Greeting", null), ("Farewell", null)));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        var issue = Assert.Single(result.ResourceIssues);
        Assert.Equal("Farewell", issue.Key);
        Assert.DoesNotContain("bin", issue.LocaleResourcePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MultipleResourceFamilies_InSameDirectory_AreCheckedIndependently()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("First.resx", Resx(("A", null)));
        dir.WriteFile("First.ja-JP.resx", Resx());
        dir.WriteFile("Second.resx", Resx(("B", null)));
        dir.WriteFile("Second.ja-JP.resx", Resx(("B", null)));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        var issue = Assert.Single(result.ResourceIssues);
        Assert.Equal("A", issue.Key);
        Assert.Contains("First", issue.NeutralResourcePath);
    }

    [Fact]
    public void FileNameWithNonCultureSuffix_IsTreatedAsItsOwnNeutralFamily()
    {
        // "Designer" doesn't parse as a culture, so this must not be misread as a satellite
        // of a "Resources" family.
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("Greeting", null)));
        dir.WriteFile("Resources.Designer.resx", Resx(("Unrelated", null)));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        Assert.True(result.IsClean);
    }

    [Fact]
    public void XamlUsage_UndefinedKey_IsReported()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("RealKey", null)));
        dir.WriteFile("View.xaml", """
            <Page xmlns:lx="http://wpf.meteion.ca/winfx/xaml/localization">
                <TextBlock Text="{lx:LocalizedValue RealKey}" />
                <TextBlock Text="{lx:LocalizedValue TotallyMadeUp}" />
            </Page>
            """);

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        var issue = Assert.Single(result.UsageIssues);
        Assert.Equal("TotallyMadeUp", issue.Key);
    }

    [Fact]
    public void XamlUsage_ObjectElementSyntax_IsAlsoChecked()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx());
        dir.WriteFile("View.xaml", """
            <Page xmlns:lx="http://wpf.meteion.ca/winfx/xaml/localization">
                <lx:LocalizedValue Key="UndefinedViaElement" />
            </Page>
            """);

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        var issue = Assert.Single(result.UsageIssues);
        Assert.Equal("UndefinedViaElement", issue.Key);
    }

    [Fact]
    public void XamlUsage_KeyBinding_IsSkipped_NotFalsePositive()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx());
        dir.WriteFile("View.xaml", """
            <Page xmlns:lx="http://wpf.meteion.ca/winfx/xaml/localization">
                <TextBlock Text="{lx:LocalizedValue KeyBinding={Binding SelectedKey}}" />
            </Page>
            """);

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        Assert.Empty(result.UsageIssues);
    }

    [Fact]
    public void XamlUsage_RespectsCustomNamespacePrefix()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx());
        dir.WriteFile("View.xaml", """
            <Page xmlns:loc="http://wpf.meteion.ca/winfx/xaml/localization">
                <TextBlock Text="{loc:LocalizedValue NotDefinedEither}" />
            </Page>
            """);

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        var issue = Assert.Single(result.UsageIssues);
        Assert.Equal("NotDefinedEither", issue.Key);
    }

    [Fact]
    public void XamlUsage_NotChecked_WhenDisabled()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx());
        dir.WriteFile("View.xaml", """
            <Page xmlns:lx="http://wpf.meteion.ca/winfx/xaml/localization">
                <TextBlock Text="{lx:LocalizedValue TotallyMadeUp}" />
            </Page>
            """);

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path, new LocalizationCheckOptions { CheckXamlUsages = false });

        Assert.Empty(result.UsageIssues);
    }
}
