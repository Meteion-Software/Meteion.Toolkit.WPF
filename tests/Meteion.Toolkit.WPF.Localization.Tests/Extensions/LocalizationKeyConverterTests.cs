using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Extensions;

namespace Meteion.Toolkit.WPF.Localization.Tests.Extensions;

public class LocalizationKeyConverterTests
{
    [GeneratedLocalizationKeys]
    private static class FakeGeneratedKeys
    {
        public const string Greeting = "Greeting";
        public const string Farewell = "Farewell";

        // Only public static string consts count as keys - these should never show up.
        public static string NotConst = "NotAKey";
        private const string NotPublic = "AlsoNotAKey";
        public const int NotAString = 42;
    }

    private readonly LocalizationKeyConverter _converter = new();

    [Fact]
    public void GetStandardValuesSupported_IsTrue() =>
        Assert.True(_converter.GetStandardValuesSupported(context: null));

    [Fact]
    public void GetStandardValuesExclusive_IsFalse()
    {
        // Not exclusive - a Key can also come from a resx this converter can't see, or from
        // KeyBinding at runtime, so the dropdown is a convenience list, not a validator.
        Assert.False(_converter.GetStandardValuesExclusive(context: null));
    }

    [Fact]
    public void GetStandardValues_IncludesConstsFromMarkedClass_ButNotNonMatchingFields()
    {
        var values = _converter.GetStandardValues(context: null)!.Cast<string>().ToList();

        Assert.Contains("Greeting", values);
        Assert.Contains("Farewell", values);
        Assert.DoesNotContain("NotAKey", values);
        Assert.DoesNotContain("AlsoNotAKey", values);
    }
}
