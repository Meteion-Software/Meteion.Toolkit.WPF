namespace Meteion.Toolkit.Localization.Check.Tests;

public class XamlLiteralCheckerTests
{
    private static IReadOnlyList<LocalizationKeyUsageIssue> Check(string xaml, LocalizationCheckOptions? options = null)
    {
        using var dir = new TempDirectory();
        dir.WriteFile("View.xaml", xaml);

        return LocalizationKeyChecker.CheckDirectory(dir.Path, options ?? new LocalizationCheckOptions { CheckLiterals = true }).UsageIssues;
    }

    [Fact]
    public void Literals_AreNotChecked_ByDefault()
    {
        var issues = Check("""<TextBlock Text="Hello" />""", new LocalizationCheckOptions());

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData("Text")]
    [InlineData("Content")]
    [InlineData("Header")]
    [InlineData("ToolTip")]
    [InlineData("Title")]
    [InlineData("Watermark")]
    [InlineData("AutomationProperties.Name")]
    [InlineData("NullText")]
    [InlineData("EmptyText")]
    [InlineData("WatermarkContent")]
    [InlineData("Label")]
    [InlineData("Caption")]
    public void PlainLiteral_InKnownProperty_IsReported(string property)
    {
        var issue = Assert.Single(Check($"""<Thing {property}="Hello" />"""));

        Assert.Equal(LocalizationKeyUsageIssueKind.UnlocalizedLiteral, issue.Kind);
        Assert.Equal("LOC009", issue.Code);
        Assert.Equal(1, issue.LineNumber);
    }

    [Theory]
    [InlineData("{lx:LocalizedValue Greeting}")]
    [InlineData("{Binding Name}")]
    [InlineData("{x:Static local:Keys.Greeting}")]
    [InlineData("{StaticResource Greeting}")]
    [InlineData(":")]
    [InlineData("…")]
    [InlineData("1")]
    [InlineData("")]
    public void MarkupExtensionsAndLetterlessStrings_AreNotReported(string value)
    {
        Assert.Empty(Check($"""<TextBlock Text="{value}" />"""));
    }

    [Fact]
    public void EscapedBrace_IsALiteral()
    {
        Assert.Single(Check("""<TextBlock Text="{}{Braces} here" />"""));
    }

    [Fact]
    public void UnknownProperty_IsNotReported()
    {
        Assert.Empty(Check("""<TextBlock xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" Tag="Hello" x:Name="Hello" />"""));
    }

    [Fact]
    public void ElementText_IsReported_ForContentElements()
    {
        var issues = Check("""
            <StackPanel>
                <TextBlock>Hello</TextBlock>
                <Button>Save</Button>
                <RadButton>Cancel</RadButton>
                <Border>Not a content element</Border>
            </StackPanel>
            """);

        Assert.Equal([2, 3, 4], issues.Select(i => i.LineNumber));
    }

    [Fact]
    public void PropertyElement_IsReported()
    {
        var issue = Assert.Single(Check("""
            <Button>
                <Button.Content>Hello</Button.Content>
            </Button>
            """));

        Assert.Equal(2, issue.LineNumber);
    }

    [Fact]
    public void PropertyElement_WithNestedElements_IsNotReported()
    {
        Assert.Empty(Check("""
            <Button>
                <Button.Content><Image Source="a.png" /></Button.Content>
            </Button>
            """));
    }

    [Fact]
    public void AdditionalProperties_AreChecked()
    {
        var options = new LocalizationCheckOptions { CheckLiterals = true, AdditionalLiteralProperties = ["Hint"] };

        Assert.Single(Check("""<Thing Hint="Hello" />""", options));
        Assert.Empty(Check("""<Thing Hint="Hello" />"""));
    }

    [Fact]
    public void IgnoreComment_WithReason_SuppressesElement()
    {
        var issues = Check("""
            <StackPanel>
                <!-- loc-ignore: product name -->
                <TextBlock Text="Meteion" />
                <TextBlock Text="Hello" />
            </StackPanel>
            """);

        var issue = Assert.Single(issues);
        Assert.Equal(LocalizationKeyUsageIssueKind.UnlocalizedLiteral, issue.Kind);
        Assert.Equal(4, issue.LineNumber);
    }

    [Fact]
    public void IgnoreComment_WithoutReason_SuppressesElement_ButWarnsAboutTheComment()
    {
        var issue = Assert.Single(Check("""
            <StackPanel>
                <!-- loc-ignore -->
                <TextBlock Text="Meteion" />
            </StackPanel>
            """));

        Assert.Equal(LocalizationKeyUsageIssueKind.IgnoreWithoutReason, issue.Kind);
        Assert.Equal("LOC010", issue.Code);
        Assert.Equal(2, issue.LineNumber);
    }

    [Fact]
    public void IgnoreComment_WithEmptyReason_CountsAsNoReason()
    {
        var issue = Assert.Single(Check("""
            <StackPanel>
                <!-- loc-ignore:   -->
                <TextBlock Text="Meteion" />
            </StackPanel>
            """));

        Assert.Equal(LocalizationKeyUsageIssueKind.IgnoreWithoutReason, issue.Kind);
    }

    [Fact]
    public void IgnoreComment_DoesNotCoverChildren()
    {
        var issue = Assert.Single(Check("""
            <StackPanel>
                <!-- loc-ignore: the panel only -->
                <Grid ToolTip="Skipped">
                    <TextBlock Text="Hello" />
                </Grid>
            </StackPanel>
            """));

        Assert.Equal(LocalizationKeyUsageIssueKind.UnlocalizedLiteral, issue.Kind);
        Assert.Equal(4, issue.LineNumber);
    }

    [Fact]
    public void IgnoreComment_OnOwner_CoversItsPropertyElements()
    {
        Assert.Empty(Check("""
            <StackPanel>
                <!-- loc-ignore: brand name -->
                <Button>
                    <Button.Content>Meteion</Button.Content>
                </Button>
            </StackPanel>
            """));
    }

    [Fact]
    public void IgnoreComment_NotImmediatelyBefore_DoesNotApply()
    {
        var issue = Assert.Single(Check("""
            <StackPanel>
                <!-- loc-ignore: stale -->
                <Border />
                <TextBlock Text="Hello" />
            </StackPanel>
            """));

        Assert.Equal(4, issue.LineNumber);
    }

    [Fact]
    public void MalformedXaml_IsSkipped()
    {
        Assert.Empty(Check("<Page><TextBlock Text=\"Hello\""));
    }
}
