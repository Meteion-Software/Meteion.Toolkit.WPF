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
}

/// <summary>
/// A single missing- or orphan-key discrepancy between a neutral resx and one of its
/// culture-specific satellites.
/// </summary>
/// <param name="Kind">Whether the key is missing from the satellite or orphaned in it.</param>
/// <param name="Key">The resx key name involved.</param>
/// <param name="NeutralResourcePath">Path to the neutral (culture-less) resx file.</param>
/// <param name="LocaleResourcePath">Path to the satellite resx file the key is missing from or orphaned in.</param>
/// <param name="CultureName">The culture name of the satellite resx (e.g. "ja-JP").</param>
public sealed record LocalizationKeyIssue(
    LocalizationKeyIssueKind Kind,
    string Key,
    string NeutralResourcePath,
    string LocaleResourcePath,
    string CultureName);
