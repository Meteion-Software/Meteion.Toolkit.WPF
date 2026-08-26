namespace Meteion.Toolkit.Localization.Check;

/// <summary>
/// A literal key used in a XAML <c>LocalizedValue</c> usage that does not exist in any
/// neutral resx file scanned under the same root. Since <c>ILocalizationService</c>
/// implementations commonly throw for an unknown key, this is the discrepancy most likely
/// to surface as a runtime crash rather than a silently-wrong translation.
/// </summary>
/// <param name="Key">The literal key referenced from XAML.</param>
/// <param name="XamlFilePath">Path to the XAML file the usage was found in.</param>
/// <param name="LineNumber">The 1-based line number the usage appears on, when available.</param>
public sealed record LocalizationKeyUsageIssue(
    string Key,
    string XamlFilePath,
    int LineNumber);
