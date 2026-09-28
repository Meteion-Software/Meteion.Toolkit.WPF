using Meteion.Toolkit.Localization.Abstractions;

namespace Meteion.Toolkit.WPF.Localization.Tests.Abstractions;

public class LocalizationKeyTests
{
    [Theory]
    [InlineData("Asm/Asm.Strings:Key", "Asm", "Asm.Strings", "Key")]
    [InlineData("Asm.Client/Asm.Client.Properties.Strings:Home_Welcome", "Asm.Client", "Asm.Client.Properties.Strings", "Home_Welcome")]
    [InlineData("Asm/Asm.Strings:Time: {0}", "Asm", "Asm.Strings", "Time: {0}")]
    [InlineData("Asm/Asm.Strings:Yes/No", "Asm", "Asm.Strings", "Yes/No")]
    public void Parse_QualifiedKey_SplitsIntoParts(string raw, string assembly, string baseName, string key)
    {
        var parsed = LocalizationKey.Parse(raw);

        Assert.True(parsed.IsQualified);
        Assert.Equal(assembly, parsed.AssemblyName);
        Assert.Equal(baseName, parsed.BaseName);
        Assert.Equal(key, parsed.Key);
        Assert.Equal($"{assembly}/{baseName}", parsed.Source);
        Assert.Equal(raw, parsed.ToString());
    }

    [Theory]
    [InlineData("Greeting")]
    [InlineData("Time: {0}")]
    [InlineData("Yes/No")]
    [InlineData("/Base:Key")]
    [InlineData("Asm/:Key")]
    [InlineData("Asm/Base:")]
    [InlineData("A:sm/Base:Key")]
    [InlineData("Asm/Ba/se:Key")]
    public void Parse_NotQualifiedShape_IsUnqualified(string raw)
    {
        var parsed = LocalizationKey.Parse(raw);

        Assert.False(parsed.IsQualified);
        Assert.Null(parsed.AssemblyName);
        Assert.Null(parsed.BaseName);
        Assert.Null(parsed.Source);
        Assert.Equal(raw, parsed.Key);
        Assert.Equal(raw, parsed.ToString());
    }

    [Theory]
    [InlineData("Asm/Asm.Strings", true)]
    [InlineData("Asm", false)]
    [InlineData("Asm/", false)]
    [InlineData("/Asm.Strings", false)]
    [InlineData("Asm/Asm.Strings:Key", false)]
    [InlineData("Asm/A/B", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void TryParseSource_ValidatesShape(string? source, bool expected)
    {
        Assert.Equal(expected, LocalizationKey.TryParseSource(source, out _, out _));
    }

    [Fact]
    public void FromSource_CombinesSourceAndKey()
    {
        var key = LocalizationKey.FromSource("Asm/Asm.Strings", "Notification_Info");

        Assert.Equal("Asm/Asm.Strings:Notification_Info", key.ToString());
    }

    [Fact]
    public void FromSource_QualifiedKey_Throws()
    {
        Assert.Throws<LocalizationConfigurationException>(
            () => LocalizationKey.FromSource("Asm/Asm.Strings", "Other/Other.Strings:Key"));
    }

    [Fact]
    public void FromSource_InvalidSource_Throws()
    {
        Assert.Throws<LocalizationConfigurationException>(() => LocalizationKey.FromSource("Asm", "Key"));
    }
}
