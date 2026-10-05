namespace Meteion.Toolkit.Localization.Check;

/// <summary>
/// The kind of discrepancy found between a neutral (default) resx file and one of its
/// culture-specific satellites.
/// </summary>
public enum LocalizationKeyIssueKind
{
    /// <summary>
    /// A key is defined in the neutral resx but is missing from a satellite resx.
    /// </summary>
    MissingKey,

    /// <summary>
    /// A key exists in a satellite resx but is not defined in the neutral resx — likely a
    /// typo or a leftover key from a rename.
    /// </summary>
    OrphanKey,

    /// <summary>
    /// LOC011: a satellite resx value uses a different set of format placeholders
    /// (<c>{0}</c>, <c>{1:N0}</c>, ...) than the neutral value for the same key, so the same
    /// arguments can't format both.
    /// </summary>
    PlaceholderMismatch,

    /// <summary>
    /// LOC012: a value is not a valid composite format string (unbalanced braces, a placeholder
    /// with no index, ...). Only a real problem if the string is ever formatted; see
    /// <see cref="LocalizationCheckOptions.CheckFormatStrings"/>.
    /// </summary>
    InvalidFormat,

    /// <summary>
    /// LOC013: a value skips a placeholder index (e.g. <c>{0}</c> and <c>{2}</c> but no <c>{1}</c>) -
    /// almost always a typo, since the skipped argument still has to be supplied.
    /// </summary>
    PlaceholderGap,
}

/// <summary>
/// A single missing- or orphan-key discrepancy between a neutral resx and one of its
/// culture-specific satellites.
/// </summary>
/// <param name="Kind">Whether the key is missing from the satellite or orphaned in it.</param>
/// <param name="Key">The resx key name involved.</param>
/// <param name="NeutralResourcePath">Path to the neutral (culture-less) resx file.</param>
/// <param name="LocaleResourcePath">Path to the satellite resx file the key is missing from or orphaned in.</param>
/// <param name="CultureName">The culture name of the satellite resx (e.g. "ja-JP"), or empty when the issue is in the neutral resx.</param>
/// <param name="Detail">What exactly is wrong, for the format issues (LOC011-LOC013); null for missing and orphan keys.</param>
public sealed record LocalizationKeyIssue(
    LocalizationKeyIssueKind Kind,
    string Key,
    string NeutralResourcePath,
    string LocaleResourcePath,
    string CultureName,
    string? Detail = null)
{
    /// <summary>The diagnostic code, e.g. "LOC001" for a missing key.</summary>
    public string Code => Kind switch
    {
        LocalizationKeyIssueKind.MissingKey => "LOC001",
        LocalizationKeyIssueKind.OrphanKey => "LOC002",
        LocalizationKeyIssueKind.PlaceholderMismatch => "LOC011",
        LocalizationKeyIssueKind.InvalidFormat => "LOC012",
        LocalizationKeyIssueKind.PlaceholderGap => "LOC013",
        _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, null),
    };
}
