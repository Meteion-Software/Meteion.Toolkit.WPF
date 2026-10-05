namespace Meteion.Toolkit.Localization.Check.Tests;

/// <summary>LOC011-LOC014: resx values and XAML usages checked as composite format strings.</summary>
public class FormatStringCheckerTests
{
    private const string XamlHeader =
        """xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:lx="http://wpf.meteion.ca/winfx/xaml/localization" """;

    private static string Resx(params (string Name, string Value)[] entries)
    {
        var dataElements = entries.Select(e =>
            $"""<data name="{e.Name}" xml:space="preserve"><value>{System.Security.SecurityElement.Escape(e.Value)}</value></data>""");

        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <root>
                {string.Join(Environment.NewLine, dataElements)}
            </root>
            """;
    }

    private static string Xaml(string body) => $"""
        <Page {XamlHeader}>
            {body}
        </Page>
        """;

    [Fact]
    public void PlaceholderMismatch_IsReported_WhenSatelliteUsesDifferentPlaceholders()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("Greeting", "Hello {0}, you have {1:N0} items")));
        dir.WriteFile("Resources.fr.resx", Resx(("Greeting", "Bonjour {0}")));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        var issue = Assert.Single(result.ResourceIssues);
        Assert.Equal(LocalizationKeyIssueKind.PlaceholderMismatch, issue.Kind);
        Assert.Equal("LOC011", issue.Code);
        Assert.Equal("fr", issue.CultureName);
        Assert.Contains("{0}, {1}", issue.Detail);
    }

    [Fact]
    public void PlaceholderOrderAndFormatSpecifiers_DoNotCountAsAMismatch()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("Greeting", "{0} has {1:N0} items")));
        dir.WriteFile("Resources.ja-JP.resx", Resx(("Greeting", "{1}個の項目が{0}にあります")));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        Assert.True(result.IsClean, string.Join(", ", result.ResourceIssues));
    }

    [Fact]
    public void InvalidFormat_IsReported_ForNeutralAndSatelliteValues()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("Broken", "Hello {0")));
        dir.WriteFile("Resources.fr.resx", Resx(("Broken", "Bonjour }")));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        Assert.Collection(
            result.ResourceIssues.OrderBy(i => i.CultureName),
            neutral =>
            {
                Assert.Equal(LocalizationKeyIssueKind.InvalidFormat, neutral.Kind);
                Assert.Equal("LOC012", neutral.Code);
                Assert.Equal(string.Empty, neutral.CultureName);
            },
            satellite =>
            {
                Assert.Equal(LocalizationKeyIssueKind.InvalidFormat, satellite.Kind);
                Assert.Equal("fr", satellite.CultureName);
            });
    }

    [Fact]
    public void EscapedBraces_AreNotPlaceholders()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("Literal", "Use {{braces}} literally, and {0}")));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        Assert.True(result.IsClean, string.Join(", ", result.ResourceIssues));
    }

    [Fact]
    public void PlaceholderGap_IsReported_WhenAnIndexIsSkipped()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("Skips", "{0} and {2}")));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        var issue = Assert.Single(result.ResourceIssues);
        Assert.Equal(LocalizationKeyIssueKind.PlaceholderGap, issue.Kind);
        Assert.Equal("LOC013", issue.Code);
        Assert.Contains("{1}", issue.Detail);
    }

    [Fact]
    public void FormatChecks_AreSkipped_WhenDisabled()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("Broken", "Hello {0"), ("Skips", "{0} and {2}")));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path, new LocalizationCheckOptions { CheckFormatStrings = false });

        Assert.Empty(result.ResourceIssues);
    }

    [Fact]
    public void ArgumentCountMismatch_IsReported_WhenUsageSuppliesTooFewArguments()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("Greeting", "Hello {0}, you have {1} items")));
        dir.WriteFile("Page.xaml", Xaml("""<TextBlock Text="{lx:LocalizedValue Key=Greeting, Arg0={Binding Name}}" />"""));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        var issue = Assert.Single(result.UsageIssues);
        Assert.Equal(LocalizationKeyUsageIssueKind.ArgumentCountMismatch, issue.Kind);
        Assert.Equal("LOC014", issue.Code);
        Assert.Equal("Greeting", issue.Key);
        Assert.Contains("supplies 1", issue.Detail);
    }

    [Fact]
    public void ArgumentCountMismatch_IsReported_WhenAFormattedValueGetsNoArguments()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("Greeting", "Hello {0}")));
        dir.WriteFile("Page.xaml", Xaml("""<TextBlock Text="{lx:LocalizedValue Key=Greeting}" />"""));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        Assert.Equal(LocalizationKeyUsageIssueKind.ArgumentCountMismatch, Assert.Single(result.UsageIssues).Kind);
    }

    [Fact]
    public void ArgumentCount_Matching_ProducesNoIssue()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("Greeting", "Hello {0}, you have {1} items"), ("Plain", "Hi")));
        dir.WriteFile("Page.xaml", Xaml("""
            <StackPanel>
                <TextBlock Text="{lx:LocalizedValue Key=Greeting, Arg0={Binding Name}, Arg1=5}" />
                <TextBlock Text="{lx:LocalizedValue Key=Plain}" />
                <TextBlock Text="{lx:LocalizedValue Greeting, Arg0={Binding Name}, Arg1={Binding Count}}" />
            </StackPanel>
            """));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        Assert.Empty(result.UsageIssues);
    }

    [Fact]
    public void ArgumentCount_IsNotChecked_WhenArgsOrPropertyElementsHideTheCount()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("Greeting", "Hello {0}, you have {1} items")));
        dir.WriteFile("Page.xaml", Xaml("""
            <StackPanel>
                <TextBlock Text="{lx:LocalizedValue Key=Greeting, Args={StaticResource SomeArgs}}" />
                <TextBlock>
                    <TextBlock.Text>
                        <lx:LocalizedValue Key="Greeting">
                            <lx:LocalizedValue.Args>
                                <MultiBinding><Binding Path="A" /><Binding Path="B" /></MultiBinding>
                            </lx:LocalizedValue.Args>
                        </lx:LocalizedValue>
                    </TextBlock.Text>
                </TextBlock>
            </StackPanel>
            """));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        Assert.Empty(result.UsageIssues);
    }

    [Fact]
    public void ArgumentCount_IsNotChecked_WhenFormatChecksAreDisabled()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("Greeting", "Hello {0}")));
        dir.WriteFile("Page.xaml", Xaml("""<TextBlock Text="{lx:LocalizedValue Key=Greeting}" />"""));

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path, new LocalizationCheckOptions { CheckFormatStrings = false });

        Assert.Empty(result.UsageIssues);
    }
}
