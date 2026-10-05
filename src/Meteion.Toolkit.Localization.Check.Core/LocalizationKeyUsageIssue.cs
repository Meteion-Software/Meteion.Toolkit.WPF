namespace Meteion.Toolkit.Localization.Check;

/// <summary>
/// The kind of problem found with a XAML <c>LocalizedValue</c> usage.
/// </summary>
public enum LocalizationKeyUsageIssueKind
{
    /// <summary>
    /// LOC003: the key (a literal, a generated keys constant via <c>x:Static</c>, or
    /// <c>Source</c> + an unqualified key) doesn't exist in the resx it resolves to.
    /// </summary>
    UndefinedKey,

    /// <summary>
    /// LOC004: an unqualified key with no <c>Source</c>, in a project with more than one
    /// neutral resx - ambiguous at runtime.
    /// </summary>
    AmbiguousUnqualifiedKey,

    /// <summary>LOC005: a qualified key combined with <c>KeyPrefix</c>.</summary>
    QualifiedKeyWithPrefix,

    /// <summary>LOC006: <c>Source</c> names a resx that doesn't exist in this project.</summary>
    UnknownSource,

    /// <summary>
    /// LOC007 (informational): the key or <c>Source</c> names another assembly, which this
    /// project-scoped check can't see into.
    /// </summary>
    UnverifiableCrossAssemblyKey,

    /// <summary>
    /// LOC008: <c>Source</c> and <c>Assembly</c> are both set - <c>Source</c> already names its
    /// assembly, so this always throws at runtime.
    /// </summary>
    SourceWithAssembly,

    /// <summary>
    /// LOC009: a user-visible property or element text holds a plain string literal instead of
    /// a localized value. Only checked when <see cref="LocalizationCheckOptions.CheckLiterals"/> is set.
    /// </summary>
    UnlocalizedLiteral,

    /// <summary>
    /// LOC010: a <c>&lt;!-- loc-ignore --&gt;</c> comment gives no reason. The element it precedes
    /// is still skipped by LOC009.
    /// </summary>
    IgnoreWithoutReason,

    /// <summary>
    /// LOC014: the number of <c>Arg0</c>..<c>ArgN</c> a usage supplies doesn't match the number of
    /// placeholders in the resx value it resolves to. Only checked when the usage's arguments can
    /// be counted statically (an <c>Args</c> value or a property-element usage can't be).
    /// </summary>
    ArgumentCountMismatch,
}

/// <summary>
/// A problem with a XAML <c>LocalizedValue</c> usage. Since <c>ILocalizationService</c>
/// implementations commonly throw for an unknown key, these are the discrepancies most likely
/// to surface as a runtime crash rather than a silently-wrong translation.
/// </summary>
/// <param name="Key">The key as written (or as resolved from a generated constant), for display.</param>
/// <param name="XamlFilePath">Path to the XAML file the usage was found in.</param>
/// <param name="LineNumber">The 1-based line number the usage appears on, when available.</param>
/// <param name="Kind">What's wrong with the usage.</param>
/// <param name="Detail">What exactly is wrong, for LOC014; null otherwise.</param>
public sealed record LocalizationKeyUsageIssue(
    string Key,
    string XamlFilePath,
    int LineNumber,
    LocalizationKeyUsageIssueKind Kind = LocalizationKeyUsageIssueKind.UndefinedKey,
    string? Detail = null)
{
    /// <summary>The diagnostic code, e.g. "LOC003".</summary>
    public string Code => Kind switch
    {
        LocalizationKeyUsageIssueKind.UndefinedKey => "LOC003",
        LocalizationKeyUsageIssueKind.AmbiguousUnqualifiedKey => "LOC004",
        LocalizationKeyUsageIssueKind.QualifiedKeyWithPrefix => "LOC005",
        LocalizationKeyUsageIssueKind.UnknownSource => "LOC006",
        LocalizationKeyUsageIssueKind.UnverifiableCrossAssemblyKey => "LOC007",
        LocalizationKeyUsageIssueKind.SourceWithAssembly => "LOC008",
        LocalizationKeyUsageIssueKind.UnlocalizedLiteral => "LOC009",
        LocalizationKeyUsageIssueKind.IgnoreWithoutReason => "LOC010",
        LocalizationKeyUsageIssueKind.ArgumentCountMismatch => "LOC014",
        _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null),
    };

    /// <summary>True for informational issues (LOC007), which never fail a check.</summary>
    public bool IsInformational => Kind == LocalizationKeyUsageIssueKind.UnverifiableCrossAssemblyKey;
}
