using System.IO;
using System.Reflection;

namespace Meteion.Toolkit.WPF.SplashScreen.Tests;

public class SplashScreenBuilderTests
{
    private static readonly Assembly TestAssembly = typeof(SplashScreenBuilderTests).Assembly;

    [Fact]
    public void Show_WithoutImage_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new SplashScreenBuilder().Show());
    }

    [Fact]
    public void Show_Twice_ThrowsOnTheSecondCall()
    {
        var builder = new SplashScreenBuilder().UseImageFromEmbeddedResource("Unique.png", TestAssembly);
        using var first = builder.Show();

        Assert.Throws<InvalidOperationException>(() => builder.Show());
    }

    [Fact]
    public void Embedded_NoMatch_ListsAvailableResourceNames()
    {
        var builder = new SplashScreenBuilder().UseImageFromEmbeddedResource("Nope.png", TestAssembly);

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Show());
        Assert.Contains("Unique.png", ex.Message);
        Assert.Contains("EmbeddedResource", ex.Message);
    }

    [Fact]
    public void Embedded_AmbiguousSuffix_ListsCandidates()
    {
        var builder = new SplashScreenBuilder().UseImageFromEmbeddedResource("Dup.png", TestAssembly);

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Show());
        Assert.Contains("Fixtures.A.Dup.png", ex.Message);
        Assert.Contains("Fixtures.B.Dup.png", ex.Message);
    }

    [Fact]
    public void Embedded_ExactManifestName_WinsOverSuffixMatching()
    {
        var exact = TestAssembly.GetManifestResourceNames().Single(n => n.EndsWith("Fixtures.A.Dup.png", StringComparison.Ordinal));

        var resolved = SplashImageSource.Match(TestAssembly.GetManifestResourceNames(), exact, TestAssembly);

        Assert.Equal(exact, resolved);
    }

    [Fact]
    public void Filesystem_MissingFile_ThrowsWithTheResolvedPath()
    {
        var builder = new SplashScreenBuilder().UseImageFromFilesystem("definitely-missing.png");

        var ex = Assert.Throws<FileNotFoundException>(() => builder.Show());
        Assert.Contains(Path.Combine(AppContext.BaseDirectory, "definitely-missing.png"), ex.Message);
    }

    [Fact]
    public void LastUseImageCallWins()
    {
        var builder = new SplashScreenBuilder()
            .UseImageFromFilesystem("definitely-missing.png")
            .UseImageFromEmbeddedResource("Unique.png", TestAssembly);

        using var splash = builder.Show(); // would throw if the filesystem call had won
    }

    [Fact]
    public void Configure_AccumulatesAcrossCallsInOrder()
    {
        var builder = new SplashScreenBuilder().UseImageFromEmbeddedResource("Unique.png", TestAssembly);
        var order = new List<int>();
        builder.Configure(o => order.Add(1)).Configure(o => order.Add(2));

        using var splash = builder.Show();

        Assert.Equal([1, 2], order);
    }

    [Fact]
    public void Show_InvalidOption_Throws()
    {
        var builder = new SplashScreenBuilder()
            .UseImageFromEmbeddedResource("Unique.png", TestAssembly)
            .Configure(o => o.FontSize = 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => builder.Show());
    }
}
