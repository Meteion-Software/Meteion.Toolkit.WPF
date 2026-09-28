namespace Meteion.Toolkit.Localization.Check;

/// <summary>
/// The combined result of a <see cref="LocalizationKeyChecker.CheckDirectory"/> pass.
/// </summary>
/// <param name="ResourceIssues">Missing/orphan key discrepancies between neutral and satellite resx files.</param>
/// <param name="UsageIssues">Problems with XAML <c>LocalizedValue</c> usages, including informational LOC007 notes.</param>
public sealed record LocalizationCheckResult(
    IReadOnlyList<LocalizationKeyIssue> ResourceIssues,
    IReadOnlyList<LocalizationKeyUsageIssue> UsageIssues)
{
    /// <summary>
    /// True when neither the resx sync check nor the XAML usage check found anything to report
    /// (informational LOC007 notes don't count).
    /// </summary>
    public bool IsClean => ResourceIssues.Count == 0 && UsageIssues.All(i => i.IsInformational);
}
