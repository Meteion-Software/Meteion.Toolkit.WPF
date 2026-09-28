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
    public void XamlUsage_KeyViaXStatic_IsSkipped_NotFalsePositive()
    {
        // A Key sourced from a nested markup extension (e.g. a
        // Meteion.Toolkit.Localization.KeysGenerator-generated const via x:Static) can't be
        // statically resolved here any more than a KeyBinding can - same non-literal-Key rule.
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("RealKey", null)));
        dir.WriteFile("View.xaml", """
            <Page
                xmlns:keys="clr-namespace:MyApp.Resources"
                xmlns:lx="http://wpf.meteion.ca/winfx/xaml/localization"
                xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <TextBlock Text="{lx:LocalizedValue Key={x:Static keys:ResourcesKeys.RealKey}}" />
                <lx:LocalizedValue Key="{x:Static keys:ResourcesKeys.RealKey}" />
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

    private static readonly LocalizationCheckOptions AppOptions = new() { AssemblyName = "App", RootNamespace = "App" };

    private const string MultiResxXamlHeader = """
        <Page
            xmlns:lx="http://wpf.meteion.ca/winfx/xaml/localization"
            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
            xmlns:home="clr-namespace:App.Views.Home"
            xmlns:shared="clr-namespace:App.Shared"
            xmlns:other="clr-namespace:Other.Strings;assembly=Other">

        """;

    private static TempDirectory MultiResxProject(string xamlBody)
    {
        var dir = new TempDirectory();
        dir.WriteFile("Views/Home/Strings.resx", Resx(("Welcome", null), ("Notification_Info", null)));
        dir.WriteFile("Shared/Common.resx", Resx(("Ok", null)));
        dir.WriteFile("View.xaml", MultiResxXamlHeader + xamlBody + "\n</Page>");
        return dir;
    }

    [Fact]
    public void XamlUsage_GeneratedKeysViaXStatic_ResolveAgainstTheirResx()
    {
        using var dir = MultiResxProject("""
            <TextBlock Text="{lx:LocalizedValue {x:Static home:StringsKeys.Welcome}}" />
            <TextBlock Text="{lx:LocalizedValue Key={x:Static shared:CommonKeys.Ok}}" />
            <lx:LocalizedValue Key="{x:Static home:StringsKeys.Welcome}" />
            """);

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path, AppOptions);

        Assert.True(result.IsClean, string.Join(", ", result.UsageIssues));
    }

    [Fact]
    public void XamlUsage_XStaticToMissingGeneratedMember_IsUndefinedKey()
    {
        using var dir = MultiResxProject("""
            <TextBlock Text="{lx:LocalizedValue {x:Static home:StringsKeys.Nope}}" />
            """);

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path, AppOptions);

        var issue = Assert.Single(result.UsageIssues);
        Assert.Equal(LocalizationKeyUsageIssueKind.UndefinedKey, issue.Kind);
        Assert.Equal("StringsKeys.Nope", issue.Key);
        Assert.Equal("LOC003", issue.Code);
    }

    [Fact]
    public void XamlUsage_QualifiedLiteral_IsCheckedAgainstNamedResx()
    {
        using var dir = MultiResxProject("""
            <TextBlock Text="{lx:LocalizedValue App/App.Views.Home.Strings:Welcome}" />
            <TextBlock Text="{lx:LocalizedValue App/App.Shared.Common:Welcome}" />
            """);

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path, AppOptions);

        var issue = Assert.Single(result.UsageIssues);
        Assert.Equal(LocalizationKeyUsageIssueKind.UndefinedKey, issue.Kind);
        Assert.Equal("App/App.Shared.Common:Welcome", issue.Key);
    }

    [Fact]
    public void XamlUsage_UnqualifiedKeyWithSeveralResx_IsAmbiguous()
    {
        using var dir = MultiResxProject("""
            <TextBlock Text="{lx:LocalizedValue Welcome}" />
            """);

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path, AppOptions);

        var issue = Assert.Single(result.UsageIssues);
        Assert.Equal(LocalizationKeyUsageIssueKind.AmbiguousUnqualifiedKey, issue.Kind);
        Assert.Equal("LOC004", issue.Code);
    }

    [Fact]
    public void XamlUsage_QualifiedKeyWithKeyPrefix_IsReported()
    {
        using var dir = MultiResxProject("""
            <TextBlock Text="{lx:LocalizedValue {x:Static home:StringsKeys.Welcome}, KeyPrefix=Notification_}" />
            """);

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path, AppOptions);

        var issue = Assert.Single(result.UsageIssues);
        Assert.Equal(LocalizationKeyUsageIssueKind.QualifiedKeyWithPrefix, issue.Kind);
        Assert.Equal("LOC005", issue.Code);
    }

    [Fact]
    public void XamlUsage_SourceWithPrefixedKey_IsCheckedAgainstSourceResx()
    {
        using var dir = MultiResxProject("""
            <TextBlock Text="{lx:LocalizedValue Info, KeyPrefix=Notification_, Source={x:Static home:StringsKeys.ResxSource}}" />
            <TextBlock Text="{lx:LocalizedValue Warning, KeyPrefix=Notification_, Source=App/App.Views.Home.Strings}" />
            """);

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path, AppOptions);

        var issue = Assert.Single(result.UsageIssues);
        Assert.Equal(LocalizationKeyUsageIssueKind.UndefinedKey, issue.Kind);
        Assert.Equal("Notification_Warning", issue.Key);
    }

    [Theory]
    [InlineData("App/App.Nowhere")]
    [InlineData("NotASource")]
    public void XamlUsage_UnknownSource_IsReported_EvenWithKeyBinding(string source)
    {
        using var dir = MultiResxProject(
            "<TextBlock Text=\"{lx:LocalizedValue KeyBinding={Binding Severity}, Source=" + source + "}\" />");

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path, AppOptions);

        var issue = Assert.Single(result.UsageIssues);
        Assert.Equal(LocalizationKeyUsageIssueKind.UnknownSource, issue.Kind);
        Assert.Equal("LOC006", issue.Code);
    }

    [Fact]
    public void XamlUsage_KeyBindingWithoutSource_IsNotAmbiguous()
    {
        // Bound values may themselves be qualified keys (e.g. generated constants exposed by a
        // view model), so a KeyBinding alone never implies an ambiguous lookup.
        using var dir = MultiResxProject("""
            <TextBlock Text="{lx:LocalizedValue KeyBinding={Binding TitleKey}}" />
            """);

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path, AppOptions);

        Assert.True(result.IsClean);
    }

    [Theory]
    [InlineData("{lx:LocalizedValue Welcome, Source=App/App.Views.Home.Strings, Assembly={x:Static home:Marker.Assembly}}")]
    [InlineData("{lx:LocalizedValue KeyBinding={Binding Key}, Source={x:Static home:StringsKeys.ResxSource}, Assembly={x:Null}}")]
    [InlineData("{lx:LocalizedValue Anything, Source=Other/Other.Strings, Assembly={x:Null}}")]
    public void XamlUsage_SourceWithAssembly_IsReported(string usage)
    {
        using var dir = MultiResxProject($"<TextBlock Text=\"{usage}\" />");

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path, AppOptions);

        var issue = Assert.Single(result.UsageIssues);
        Assert.Equal(LocalizationKeyUsageIssueKind.SourceWithAssembly, issue.Kind);
        Assert.Equal("LOC008", issue.Code);
        Assert.False(issue.IsInformational);
    }

    [Fact]
    public void XamlUsage_SourceWithAssembly_ElementSyntax_IsReported()
    {
        using var dir = MultiResxProject("""
            <lx:LocalizedValue Key="Welcome" Source="App/App.Views.Home.Strings" Assembly="{x:Null}" />
            """);

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path, AppOptions);

        Assert.Equal(LocalizationKeyUsageIssueKind.SourceWithAssembly, Assert.Single(result.UsageIssues).Kind);
    }

    [Fact]
    public void XamlUsage_CrossAssemblyKeys_AreInformationalOnly()
    {
        using var dir = MultiResxProject("""
            <TextBlock Text="{lx:LocalizedValue Other/Other.Strings:Anything}" />
            <TextBlock Text="{lx:LocalizedValue {x:Static other:StringsKeys.Anything}}" />
            <TextBlock Text="{lx:LocalizedValue Anything, Source=Other/Other.Strings}" />
            """);

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path, AppOptions);

        Assert.Equal(3, result.UsageIssues.Count);
        Assert.All(result.UsageIssues, issue =>
        {
            Assert.Equal(LocalizationKeyUsageIssueKind.UnverifiableCrossAssemblyKey, issue.Kind);
            Assert.Equal("LOC007", issue.Code);
            Assert.True(issue.IsInformational);
        });
        Assert.True(result.IsClean);
    }

    [Fact]
    public void ProjectNames_DefaultToTheCsprojName()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("My App.csproj", "<Project />");
        dir.WriteFile("Strings.resx", Resx(("Welcome", null)));
        dir.WriteFile("View.xaml", """
            <Page xmlns:lx="http://wpf.meteion.ca/winfx/xaml/localization">
                <TextBlock Text="{lx:LocalizedValue My App/My_App.Strings:Welcome}" />
            </Page>
            """);

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        Assert.True(result.IsClean, string.Join(", ", result.UsageIssues));
    }

    [Fact]
    public void XamlUsage_InsideComment_IsIgnored_AndLineNumbersStayAccurate()
    {
        using var dir = new TempDirectory();
        dir.WriteFile("Resources.resx", Resx(("RealKey", null)));
        dir.WriteFile("View.xaml", """
            <Page xmlns:lx="http://wpf.meteion.ca/winfx/xaml/localization">
                <!--
                    e.g. {lx:LocalizedValue CommentedOut}
                -->
                <TextBlock Text="{lx:LocalizedValue TotallyMadeUp}" />
            </Page>
            """);

        var result = LocalizationKeyChecker.CheckDirectory(dir.Path);

        var issue = Assert.Single(result.UsageIssues);
        Assert.Equal("TotallyMadeUp", issue.Key);
        Assert.Equal(5, issue.LineNumber);
    }
}
