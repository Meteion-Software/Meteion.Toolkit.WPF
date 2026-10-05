namespace Meteion.Toolkit.Localization.Check;

/// <summary>
/// Options controlling a <see cref="LocalizationKeyChecker.CheckDirectory"/> pass.
/// </summary>
public sealed class LocalizationCheckOptions
{
    /// <summary>
    /// When true (the default), also report keys that exist in a satellite resx but not in
    /// the neutral resx (<see cref="LocalizationKeyIssueKind.OrphanKey"/>) — usually a typo
    /// or a leftover key from a rename. Like every code, the CLI only treats these as errors when promoted with
    /// <c>--warnaserror</c> or <c>--error LOC002</c>.
    /// </summary>
    public bool CheckOrphanKeys { get; init; } = true;

    /// <summary>
    /// When true (the default), also scan .xaml files for literal <c>LocalizedValue</c> key
    /// usages that don't resolve to a key in a scanned neutral resx file.
    /// </summary>
    public bool CheckXamlUsages { get; init; } = true;

    /// <summary>
    /// When true, also scan .xaml files for plain string literals in user-visible properties
    /// (LOC009). Off by default.
    /// </summary>
    public bool CheckLiterals { get; init; }

    /// <summary>
    /// When true (the default), also check resx values as composite format strings: placeholders
    /// that differ between cultures (LOC011), invalid format syntax (LOC012), skipped placeholder
    /// indices (LOC013) and - together with <see cref="CheckXamlUsages"/> - XAML usages whose
    /// argument count doesn't match (LOC014). Turn it off for a project whose strings use literal
    /// braces without ever being formatted.
    /// </summary>
    public bool CheckFormatStrings { get; init; } = true;

    /// <summary>
    /// Extra property names (e.g. <c>Foo</c>, <c>Owner.Foo</c>) checked for literals in addition
    /// to the built-in list.
    /// </summary>
    public IReadOnlyCollection<string> AdditionalLiteralProperties { get; init; } = [];

    /// <summary>
    /// The project's assembly name - the first part of every qualified key it generates. When
    /// null, derived from the single <c>*.csproj</c> in the scanned directory (or the directory's
    /// own name), matching the SDK's default of <c>$(MSBuildProjectName)</c>.
    /// </summary>
    public string? AssemblyName { get; init; }

    /// <summary>
    /// The project's root namespace, used to compute each resx's resource base name and generated
    /// keys class exactly as Meteion.Toolkit.Localization.KeysGenerator does. When null, derived
    /// like the SDK's default: the project name with spaces replaced by underscores.
    /// </summary>
    public string? RootNamespace { get; init; }

    /// <summary>
    /// Directory names (matched case-insensitively, anywhere in the scanned path) whose
    /// contents are skipped entirely. Defaults to build output folders.
    /// </summary>
    public IReadOnlySet<string> ExcludedDirectoryNames { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "bin", "obj" };
}
