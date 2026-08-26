namespace Meteion.Toolkit.Localization.Check;

/// <summary>
/// Options controlling a <see cref="LocalizationKeyChecker.CheckDirectory"/> pass.
/// </summary>
public sealed class LocalizationCheckOptions
{
    /// <summary>
    /// When true (the default), also report keys that exist in a satellite resx but not in
    /// the neutral resx (<see cref="LocalizationKeyIssueKind.OrphanKey"/>) — usually a typo
    /// or a leftover key from a rename. These never affect a strict/non-zero exit code.
    /// </summary>
    public bool CheckOrphanKeys { get; init; } = true;

    /// <summary>
    /// When true (the default), also scan .xaml files for literal <c>LocalizedValue</c> key
    /// usages that don't exist in any scanned neutral resx file.
    /// </summary>
    public bool CheckXamlUsages { get; init; } = true;

    /// <summary>
    /// Directory names (matched case-insensitively, anywhere in the scanned path) whose
    /// contents are skipped entirely. Defaults to build output folders.
    /// </summary>
    public IReadOnlySet<string> ExcludedDirectoryNames { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj" };
}
