using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Meteion.Toolkit.Localization.Check;

/// <summary>
/// Scans a directory tree for .resx localization resources and (optionally) .xaml files,
/// reporting keys missing from a satellite resx, keys orphaned in a satellite resx, and
/// XAML <c>LocalizedValue</c> usages that won't resolve at runtime.
/// </summary>
/// <remarks>
/// Every neutral resx is identified the same way Meteion.Toolkit.Localization.KeysGenerator
/// identifies it (see <c>ResxNaming</c>): <c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;</c>, plus
/// the namespace and name of its generated keys class - so a qualified key literal, a
/// <c>Source</c>, or an <c>x:Static</c> reference to a generated constant can all be traced back
/// to the resx they name. The generator's per-file overrides (<c>MeteionResourceBaseName</c>,
/// <c>MeteionKeysNamespace</c>, <c>MeteionKeysClassName</c>) live in the project file and aren't
/// modelled here.
/// </remarks>
public static class LocalizationKeyChecker
{
    /// <summary>
    /// The custom XAML namespace URI the toolkit's <c>LocalizedValueExtension</c> is
    /// typically imported under (see the "lx:" prefix used throughout the toolkit's docs
    /// and samples).
    /// </summary>
    private const string LocalizationXamlNamespace = "http://wpf.meteion.ca/winfx/xaml/localization";

    /// <summary>
    /// The CLR namespace <c>LocalizedValueExtension</c> lives in, for consumers who import
    /// it via a plain <c>clr-namespace:</c> mapping instead of the custom XAML namespace.
    /// </summary>
    private const string LocalizedValueExtensionClrNamespace = "Meteion.Toolkit.WPF.Localization.Extensions";

    /// <summary>Matches an <c>xmlns:prefix="uri"</c> declaration.</summary>
    private static readonly Regex XmlnsDeclarationPattern = new(
        "xmlns:(?<prefix>\\w+)\\s*=\\s*\"(?<uri>[^\"]*)\"",
        RegexOptions.Compiled);

    /// <summary>Matches a <c>name="value"</c> XML attribute.</summary>
    private static readonly Regex AttributePattern = new(
        "(?<name>[\\w.]+)\\s*=\\s*\"(?<value>[^\"]*)\"",
        RegexOptions.Compiled);

    private static readonly Regex CommentPattern = new("<!--.*?-->", RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>Matches a <c>clr-namespace:Ns;assembly=Asm</c> XAML namespace URI (the assembly part is optional).</summary>
    private static readonly Regex ClrNamespacePattern = new(
        @"^clr-namespace:(?<ns>[^;]*)(?:;assembly=(?<asm>.*))?$",
        RegexOptions.Compiled);

    /// <summary><c>{x:Static prefix:Type.Member}</c>, optionally written as <c>Member=prefix:Type.Member</c>.</summary>
    private static readonly Regex StaticExtensionPattern = new(
        @"^\{\s*(?:\w+:)?Static\s+(?:Member\s*=\s*)?(?<prefix>\w+):(?<type>\w+)\.(?<member>\w+)\s*\}$",
        RegexOptions.Compiled);

    /// <summary>
    /// Scans <paramref name="rootDirectory"/> and reports resx sync issues and (unless
    /// disabled) problems with XAML <c>LocalizedValue</c> usages.
    /// </summary>
    /// <param name="rootDirectory">The project directory to scan recursively. Must not be null or whitespace.</param>
    /// <param name="options">Controls which checks run and what is skipped. Defaults are used when null.</param>
    /// <returns>
    /// The issues found. The result is empty if <paramref name="rootDirectory"/> does not exist.
    /// </returns>
    /// <exception cref="ArgumentException"><paramref name="rootDirectory"/> is null, empty or whitespace.</exception>
    public static LocalizationCheckResult CheckDirectory(string rootDirectory, LocalizationCheckOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        options ??= new LocalizationCheckOptions();

        if (!Directory.Exists(rootDirectory))
        {
            return new LocalizationCheckResult([], []);
        }

        var project = ResolveProject(rootDirectory, options);
        var resourceGroups = DiscoverResourceGroups(rootDirectory, options, project);
        var resourceIssues = CheckResourceGroups(resourceGroups, options);
        if (options.CheckFormatStrings)
        {
            resourceIssues.AddRange(CheckFormatStrings(resourceGroups));
        }

        var usageIssues = options.CheckXamlUsages
            ? CheckXamlUsages(rootDirectory, options, project, resourceGroups)
            : [];

        if (options.CheckLiterals)
        {
            usageIssues.AddRange(CheckLiterals(rootDirectory, options));
        }

        return new LocalizationCheckResult(resourceIssues, usageIssues);
    }

    // Runs the LOC009/LOC010 literal check over every non-excluded .xaml file.
    private static IEnumerable<LocalizationKeyUsageIssue> CheckLiterals(string rootDirectory, LocalizationCheckOptions options) =>
        EnumerateFiles(rootDirectory, "*.xaml", options).SelectMany(path => XamlLiteralChecker.Check(path, options));

    // Compares each satellite resx against its neutral resx: LOC001 for missing keys, LOC002 for orphans.
    private static List<LocalizationKeyIssue> CheckResourceGroups(IReadOnlyList<ResourceGroup> groups, LocalizationCheckOptions options)
    {
        var issues = new List<LocalizationKeyIssue>();

        foreach (var group in groups)
        {
            if (group.NeutralPath is null)
            {
                // No neutral (culture-less) resx in this family - nothing to compare against.
                continue;
            }

            foreach (var (culture, satellitePath) in group.Satellites)
            {
                var satelliteKeys = ReadStringKeys(satellitePath);

                foreach (var key in group.Keys)
                {
                    if (!satelliteKeys.Contains(key))
                    {
                        issues.Add(new LocalizationKeyIssue(LocalizationKeyIssueKind.MissingKey, key, group.NeutralPath, satellitePath, culture));
                    }
                }

                if (options.CheckOrphanKeys)
                {
                    foreach (var key in satelliteKeys)
                    {
                        if (!group.KeySet.Contains(key))
                        {
                            issues.Add(new LocalizationKeyIssue(LocalizationKeyIssueKind.OrphanKey, key, group.NeutralPath, satellitePath, culture));
                        }
                    }
                }
            }
        }

        return issues;
    }

    /// <summary>
    /// Reads every value as a composite format string: LOC012 for invalid syntax, LOC013 for a
    /// skipped placeholder index, and LOC011 where a satellite's placeholders differ from the
    /// neutral value's. A satellite gap is left to LOC011 when the sets differ, since that already
    /// says what is wrong.
    /// </summary>
    private static List<LocalizationKeyIssue> CheckFormatStrings(IReadOnlyList<ResourceGroup> groups)
    {
        var issues = new List<LocalizationKeyIssue>();

        foreach (var group in groups)
        {
            if (group.NeutralPath is null)
            {
                continue;
            }

            var neutral = new Dictionary<string, List<int>>(StringComparer.Ordinal);
            foreach (var (key, value) in group.Values)
            {
                if (CompositeFormat.TryAnalyze(value, out var indices, out var error))
                {
                    neutral[key] = indices;
                    AddGapIssue(issues, key, indices, group.NeutralPath, group.NeutralPath, culture: string.Empty);
                }
                else
                {
                    issues.Add(new LocalizationKeyIssue(
                        LocalizationKeyIssueKind.InvalidFormat, key, group.NeutralPath, group.NeutralPath, string.Empty, error));
                }
            }

            foreach (var (culture, satellitePath) in group.Satellites)
            {
                foreach (var (key, value) in ReadStringEntries(satellitePath))
                {
                    if (!CompositeFormat.TryAnalyze(value, out var indices, out var error))
                    {
                        issues.Add(new LocalizationKeyIssue(
                            LocalizationKeyIssueKind.InvalidFormat, key, group.NeutralPath, satellitePath, culture, error));
                        continue;
                    }

                    if (neutral.TryGetValue(key, out var neutralIndices) && !neutralIndices.SequenceEqual(indices))
                    {
                        issues.Add(new LocalizationKeyIssue(
                            LocalizationKeyIssueKind.PlaceholderMismatch, key, group.NeutralPath, satellitePath, culture,
                            $"the neutral value uses {CompositeFormat.Describe(neutralIndices)}, but the '{culture}' value uses {CompositeFormat.Describe(indices)}"));
                        continue;
                    }

                    AddGapIssue(issues, key, indices, group.NeutralPath, satellitePath, culture);
                }
            }
        }

        return issues;
    }

    private static void AddGapIssue(
        List<LocalizationKeyIssue> issues, string key, List<int> indices, string neutralPath, string resourcePath, string culture)
    {
        var gaps = CompositeFormat.FindGaps(indices);
        if (gaps.Count > 0)
        {
            issues.Add(new LocalizationKeyIssue(
                LocalizationKeyIssueKind.PlaceholderGap, key, neutralPath, resourcePath, culture,
                $"it uses {CompositeFormat.Describe(indices)} but never {CompositeFormat.Describe(gaps)}"));
        }
    }

    /// <summary>
    /// LOC014: compares the number of <c>Arg0</c>..<c>ArgN</c> a usage supplies with the number
    /// of placeholders in the value it resolves to. Skipped when the usage's arguments can't be
    /// counted statically, or the value isn't a valid format string (LOC012 reports that).
    /// </summary>
    private static IEnumerable<LocalizationKeyUsageIssue> CheckArguments(
        ResourceGroup group, string keyName, XamlUsage usage, UsageContext context)
    {
        if (!context.CheckFormats ||
            usage.ArgumentCount is not { } supplied ||
            !group.Values.TryGetValue(keyName, out var value) ||
            !CompositeFormat.TryAnalyze(value, out var indices, out _))
        {
            yield break;
        }

        var required = CompositeFormat.RequiredArgumentCount(indices);
        if (supplied != required)
        {
            yield return new LocalizationKeyUsageIssue(
                keyName, context.XamlPath, usage.LineNumber, LocalizationKeyUsageIssueKind.ArgumentCountMismatch,
                $"the value uses {CompositeFormat.Describe(indices)} ({required} argument(s)), but this usage supplies {supplied}");
        }
    }

    private static List<LocalizationKeyUsageIssue> CheckXamlUsages(
        string rootDirectory,
        LocalizationCheckOptions options,
        ProjectInfo project,
        IReadOnlyList<ResourceGroup> resourceGroups)
    {
        var neutralGroups = resourceGroups.Where(g => g.NeutralPath is not null).ToList();
        var issues = new List<LocalizationKeyUsageIssue>();

        foreach (var xamlPath in EnumerateFiles(rootDirectory, "*.xaml", options))
        {
            string text;
            try
            {
                text = File.ReadAllText(xamlPath);
            }
            catch (IOException)
            {
                continue;
            }

            text = BlankOutComments(text);
            var context = new UsageContext(xamlPath, project, neutralGroups, ReadXmlnsMappings(text), options.CheckFormatStrings);
            foreach (var usage in ExtractLocalizedValueUsages(text))
            {
                issues.AddRange(CheckUsage(usage, context));
            }
        }

        return issues;
    }

    /// <summary>
    /// Applies the same rules <c>LocalizedValueExtension</c> applies at runtime to one usage, as
    /// far as they can be known statically. A <c>KeyBinding</c>'s values only exist at runtime,
    /// so for those only <c>Source</c> is checked.
    /// </summary>
    private static IEnumerable<LocalizationKeyUsageIssue> CheckUsage(XamlUsage usage, UsageContext context)
    {
        LocalizationKeyUsageIssue Issue(string key, LocalizationKeyUsageIssueKind kind) =>
            new(key, context.XamlPath, usage.LineNumber, kind);

        if (usage.Source is not null && usage.HasAssembly)
        {
            // Rejected by LocalizedValueExtension before any lookup happens, whatever the key.
            yield return Issue(usage.Source.Trim(), LocalizationKeyUsageIssueKind.SourceWithAssembly);
            yield break;
        }

        ResourceGroup? sourceGroup = null;
        var source = ResolveValue(usage.Source, context);
        switch (source.Kind)
        {
            case ResolvedKind.CrossAssembly:
                yield return Issue(source.Display, LocalizationKeyUsageIssueKind.UnverifiableCrossAssemblyKey);
                yield break;

            case ResolvedKind.UndefinedMember:
                yield return Issue(source.Display, LocalizationKeyUsageIssueKind.UnknownSource);
                yield break;

            case ResolvedKind.Value:
                if (!ResxNaming.TryParseSource(source.Value, out var sourceAssembly, out var sourceBaseName))
                {
                    yield return Issue(source.Display, LocalizationKeyUsageIssueKind.UnknownSource);
                    yield break;
                }

                if (!context.Project.IsSameAssembly(sourceAssembly))
                {
                    yield return Issue(source.Display, LocalizationKeyUsageIssueKind.UnverifiableCrossAssemblyKey);
                    yield break;
                }

                sourceGroup = context.FindByBaseName(sourceBaseName);
                if (sourceGroup is null)
                {
                    yield return Issue(source.Display, LocalizationKeyUsageIssueKind.UnknownSource);
                    yield break;
                }

                break;
        }

        if (usage.HasKeyBinding)
        {
            yield break;
        }

        var key = ResolveValue(usage.Key, context);
        switch (key.Kind)
        {
            case ResolvedKind.CrossAssembly:
                yield return Issue(key.Display, LocalizationKeyUsageIssueKind.UnverifiableCrossAssemblyKey);
                yield break;

            case ResolvedKind.UndefinedMember:
                yield return Issue(key.Display, LocalizationKeyUsageIssueKind.UndefinedKey);
                yield break;

            case ResolvedKind.Value:
                break;

            default:
                // No key, or one that can't be resolved statically (e.g. an x:Static to a
                // hand-written class) - nothing to check.
                yield break;
        }

        if (ResxNaming.TryParseQualifiedKey(key.Value!, out var keyAssembly, out var keyBaseName, out var keyName))
        {
            if (!string.IsNullOrEmpty(usage.KeyPrefix))
            {
                yield return Issue(key.Display, LocalizationKeyUsageIssueKind.QualifiedKeyWithPrefix);
                yield break;
            }

            if (!context.Project.IsSameAssembly(keyAssembly))
            {
                yield return Issue(key.Display, LocalizationKeyUsageIssueKind.UnverifiableCrossAssemblyKey);
                yield break;
            }

            var keyGroup = context.FindByBaseName(keyBaseName);
            if (keyGroup?.KeySet.Contains(keyName) != true)
            {
                yield return Issue(key.Display, LocalizationKeyUsageIssueKind.UndefinedKey);
            }
            else
            {
                foreach (var issue in CheckArguments(keyGroup, keyName, usage, context))
                {
                    yield return issue;
                }
            }

            yield break;
        }

        var fullKey = usage.KeyPrefix + key.Value;

        if (usage.Source is not null)
        {
            // An unresolvable Source (e.g. bound, or a hand-written constant) can't be checked.
            if (sourceGroup is not null && !sourceGroup.KeySet.Contains(fullKey))
            {
                yield return Issue(fullKey, LocalizationKeyUsageIssueKind.UndefinedKey);
            }
            else if (sourceGroup is not null)
            {
                foreach (var issue in CheckArguments(sourceGroup, fullKey, usage, context))
                {
                    yield return issue;
                }
            }

            yield break;
        }

        if (usage.HasAssembly)
        {
            // Resolves against a different assembly's only resx - not visible from here.
            yield break;
        }

        switch (context.NeutralGroups.Count)
        {
            case > 1:
                yield return Issue(fullKey, LocalizationKeyUsageIssueKind.AmbiguousUnqualifiedKey);
                break;

            case 1 when context.NeutralGroups[0].KeySet.Contains(fullKey):
                foreach (var issue in CheckArguments(context.NeutralGroups[0], fullKey, usage, context))
                {
                    yield return issue;
                }

                break;

            default:
                yield return Issue(fullKey, LocalizationKeyUsageIssueKind.UndefinedKey);
                break;
        }
    }

    /// <summary>
    /// Resolves a raw <c>Key</c>/<c>Source</c> argument: a literal as-is, or an
    /// <c>x:Static</c> reference to a generated keys class's constant as the value that
    /// constant holds.
    /// </summary>
    private static Resolved ResolveValue(string? raw, UsageContext context)
    {
        if (raw is null)
        {
            return new Resolved(ResolvedKind.None, null, string.Empty);
        }

        var trimmed = raw.Trim();
        if (!(trimmed.Length >= 2 && trimmed[0] == '{' && trimmed[^1] == '}'))
        {
            var literal = Unquote(trimmed);
            return new Resolved(ResolvedKind.Value, literal, literal);
        }

        var match = StaticExtensionPattern.Match(trimmed);
        if (!match.Success ||
            !context.Xmlns.TryGetValue(match.Groups["prefix"].Value, out var uri) ||
            ClrNamespacePattern.Match(uri) is not { Success: true } clr)
        {
            // Some other markup extension (a binding, a resource, ...).
            return new Resolved(ResolvedKind.Unresolvable, null, trimmed);
        }

        var typeName = match.Groups["type"].Value;
        var member = match.Groups["member"].Value;
        var display = $"{typeName}.{member}";

        if (clr.Groups["asm"].Success && !context.Project.IsSameAssembly(clr.Groups["asm"].Value.Trim()))
        {
            return new Resolved(ResolvedKind.CrossAssembly, null, display);
        }

        var group = context.FindByClass(clr.Groups["ns"].Value.Trim(), typeName);
        if (group is null)
        {
            // Not a class generated from any resx here (e.g. hand-written constants).
            return new Resolved(ResolvedKind.Unresolvable, null, display);
        }

        if (member == ResxNaming.ResxSourceFieldName)
        {
            return new Resolved(ResolvedKind.Value, group.Source, display);
        }

        return group.KeysByIdentifier.TryGetValue(member, out var keyName)
            ? new Resolved(ResolvedKind.Value, ResxNaming.QualifiedKey(context.Project.AssemblyName, group.BaseName, keyName), display)
            : new Resolved(ResolvedKind.UndefinedMember, null, display);
    }

    private static IEnumerable<XamlUsage> ExtractLocalizedValueUsages(string text)
    {
        // Only scan files that actually import the toolkit's localization extension - avoids
        // false positives from an unrelated type that happens to also be named LocalizedValue.
        if (!text.Contains(LocalizationXamlNamespace, StringComparison.Ordinal) &&
            !text.Contains(LocalizedValueExtensionClrNamespace, StringComparison.Ordinal))
        {
            yield break;
        }

        var prefix = FindMarkupExtensionPrefix(text);
        if (prefix is null)
        {
            yield break;
        }

        // Only locates the opening "{prefix:LocalizedValue" marker - the matching closing
        // brace is then found with depth-aware scanning below, since a usage's args can
        // themselves contain braces (e.g. `KeyBinding={Binding SelectedKey}`), which a
        // simple "everything up to the next }" regex would truncate on.
        var curlyMarkerPattern = new Regex($@"\{{\s*{Regex.Escape(prefix)}:LocalizedValue\b");
        var elementPattern = new Regex($@"<{Regex.Escape(prefix)}:LocalizedValue\b([^>]*)/?>");

        foreach (Match marker in curlyMarkerPattern.Matches(text))
        {
            var closeIndex = FindMatchingBrace(text, marker.Index);
            if (closeIndex < 0)
            {
                continue;
            }

            var argsStart = marker.Index + marker.Length;
            yield return ParseMarkupExtensionArgs(text[argsStart..closeIndex], GetLineNumber(text, marker.Index));
        }

        foreach (Match match in elementPattern.Matches(text))
        {
            var attributes = AttributePattern.Matches(match.Groups[1].Value)
                .ToDictionary(m => m.Groups["name"].Value, m => m.Groups["value"].Value, StringComparer.Ordinal);
            yield return CreateUsage(
                attributes, positional: null, GetLineNumber(text, match.Index), argumentsKnowable: match.Value.EndsWith("/>", StringComparison.Ordinal));
        }
    }

    private static XamlUsage ParseMarkupExtensionArgs(string rawArgs, int lineNumber)
    {
        var named = new Dictionary<string, string>(StringComparer.Ordinal);
        string? positional = null;

        var tokens = SplitTopLevel(rawArgs);
        for (var i = 0; i < tokens.Count; i++)
        {
            var eq = IndexOfTopLevel(tokens[i], '=');
            if (eq >= 0)
            {
                named[tokens[i][..eq].Trim()] = tokens[i][(eq + 1)..].Trim();
            }
            else if (i == 0)
            {
                // A bare leading token is the constructor's positional `key` arg.
                positional = tokens[i];
            }
        }

        return CreateUsage(named, positional, lineNumber);
    }

    /// <param name="argumentsKnowable">
    /// False for a <c>&lt;lx:LocalizedValue&gt;...&lt;/lx:LocalizedValue&gt;</c> element with children, which may
    /// carry an <c>Args</c> property element this scan doesn't read.
    /// </param>
    private static XamlUsage CreateUsage(
        IReadOnlyDictionary<string, string> named, string? positional, int lineNumber, bool argumentsKnowable = true)
    {
        string? Get(string name) => named.TryGetValue(name, out var value) ? value : null;

        // Arg0..Arg9 are counted as highest index + 1; an Args value (e.g. a StaticResource) is opaque.
        int? argumentCount = null;
        if (argumentsKnowable && !named.ContainsKey("Args"))
        {
            argumentCount = 0;
            foreach (var name in named.Keys)
            {
                if (name.StartsWith("Arg", StringComparison.Ordinal) &&
                    int.TryParse(name.AsSpan(3), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var index))
                {
                    argumentCount = Math.Max(argumentCount.Value, index + 1);
                }
            }
        }

        var keyPrefix = Get("KeyPrefix");
        return new XamlUsage(
            lineNumber,
            Key: Get("Key") ?? positional,
            KeyPrefix: keyPrefix is null ? null : Unquote(keyPrefix),
            Source: Get("Source"),
            HasAssembly: named.ContainsKey("Assembly"),
            HasKeyBinding: named.ContainsKey("KeyBinding"),
            ArgumentCount: argumentCount);
    }

    /// <summary>
    /// Replaces every <c>&lt;!-- ... --&gt;</c> comment with spaces (keeping its line breaks), so
    /// usage examples written in comments aren't reported while line numbers stay accurate.
    /// </summary>
    private static string BlankOutComments(string xamlText) =>
        CommentPattern.Replace(xamlText, m => new string(m.Value.Select(c => c is '\r' or '\n' ? c : ' ').ToArray()));

    private static string? FindMarkupExtensionPrefix(string xamlText)
    {
        foreach (Match match in XmlnsDeclarationPattern.Matches(xamlText))
        {
            var uri = match.Groups["uri"].Value;
            if (uri.Equals(LocalizationXamlNamespace, StringComparison.Ordinal) ||
                uri.Contains(LocalizedValueExtensionClrNamespace, StringComparison.Ordinal))
            {
                return match.Groups["prefix"].Value;
            }
        }

        return null;
    }

    private static Dictionary<string, string> ReadXmlnsMappings(string xamlText)
    {
        var mappings = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match match in XmlnsDeclarationPattern.Matches(xamlText))
        {
            mappings.TryAdd(match.Groups["prefix"].Value, match.Groups["uri"].Value);
        }

        return mappings;
    }

    /// <summary>
    /// Finds the index of the <c>}</c> that closes the <c>{</c> at <paramref name="openBraceIndex"/>,
    /// accounting for nested markup extensions (e.g. <c>{Binding ...}</c>) in between.
    /// Returns -1 if the braces are unbalanced.
    /// </summary>
    private static int FindMatchingBrace(string text, int openBraceIndex)
    {
        var depth = 0;
        for (var i = openBraceIndex; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }

                    break;
            }
        }

        return -1;
    }

    private static int IndexOfTopLevel(string text, char value)
    {
        var depth = 0;
        for (var i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    break;
                default:
                    if (text[i] == value && depth == 0)
                    {
                        return i;
                    }

                    break;
            }
        }

        return -1;
    }

    private static List<string> SplitTopLevel(string text)
    {
        var tokens = new List<string>();
        var depth = 0;
        var start = 0;

        for (var i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '{':
                    depth++;
                    break;
                case '}':
                    depth--;
                    break;
                case ',' when depth == 0:
                    tokens.Add(text[start..i]);
                    start = i + 1;
                    break;
            }
        }

        tokens.Add(text[start..]);

        return tokens
            .Select(t => t.Trim())
            .Where(t => t.Length > 0)
            .ToList();
    }

    private static string Unquote(string value)
    {
        value = value.Trim();
        return value.Length >= 2 && value[0] is '"' or '\'' && value[^1] == value[0]
            ? value[1..^1]
            : value;
    }

    private static int GetLineNumber(string text, int charIndex)
    {
        var line = 1;
        var length = Math.Min(charIndex, text.Length);
        for (var i = 0; i < length; i++)
        {
            if (text[i] == '\n')
            {
                line++;
            }
        }

        return line;
    }

    /// <summary>
    /// The string keys of a resx, in document order, first occurrence only - the same entries
    /// (and order, which the generated constant names depend on) the generator sees.
    /// </summary>
    private static List<string> ReadStringKeyList(string resxPath) =>
        ReadStringEntries(resxPath).Select(e => e.Key).ToList();

    /// <summary>
    /// The string entries of a resx - key and value - in document order, first occurrence only.
    /// </summary>
    private static List<(string Key, string Value)> ReadStringEntries(string resxPath)
    {
        var keys = new List<(string Key, string Value)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        XDocument document;
        try
        {
            document = XDocument.Load(resxPath);
        }
        catch (Exception ex) when (ex is IOException or System.Xml.XmlException)
        {
            return keys;
        }

        if (document.Root is null)
        {
            return keys;
        }

        foreach (var data in document.Root.Elements("data"))
        {
            // Skip non-string resources (images, icons, byte arrays, serialized objects, ...) -
            // they're never meant to be translated and would otherwise be reported as false
            // positives. Per the resx schema, a non-string entry carries either a "type"
            // (e.g. a byte-array resource) or just a "mimetype" (a BinaryFormatter-serialized
            // object, which embeds its own type info instead).
            if (data.Attribute("type") is not null || data.Attribute("mimetype") is not null)
            {
                continue;
            }

            var name = data.Attribute("name")?.Value;
            if (!string.IsNullOrEmpty(name) && seen.Add(name))
            {
                keys.Add((name, data.Element("value")?.Value ?? string.Empty));
            }
        }

        return keys;
    }

    private static HashSet<string> ReadStringKeys(string resxPath) => new(ReadStringKeyList(resxPath), StringComparer.Ordinal);

    // Groups .resx files into families (a neutral file plus its culture satellites) by directory and base name.
    private static List<ResourceGroup> DiscoverResourceGroups(string root, LocalizationCheckOptions options, ProjectInfo project)
    {
        var builders = new Dictionary<(string Directory, string BaseName), (string? Neutral, List<(string Culture, string Path)> Satellites)>();

        foreach (var path in EnumerateFiles(root, "*.resx", options))
        {
            var directory = Path.GetDirectoryName(path) ?? string.Empty;
            var fileName = Path.GetFileNameWithoutExtension(path);
            var (baseName, culture) = ResxNaming.SplitCultureSuffix(fileName);
            var key = (directory, baseName);

            if (!builders.TryGetValue(key, out var entry))
            {
                entry = (null, []);
            }

            if (culture is null)
            {
                entry.Neutral = path;
            }
            else
            {
                entry.Satellites.Add((culture, path));
            }

            builders[key] = entry;
        }

        return builders
            .Select(kvp =>
            {
                var relativeDir = Path.GetRelativePath(root, kvp.Key.Directory);
                if (relativeDir == ".")
                {
                    relativeDir = string.Empty;
                }

                var entries = kvp.Value.Neutral is null ? [] : ReadStringEntries(kvp.Value.Neutral);
                return new ResourceGroup(
                    kvp.Value.Neutral,
                    kvp.Value.Satellites,
                    ResxNaming.DefaultBaseName(project.RootNamespace, relativeDir, kvp.Key.BaseName),
                    ResxNaming.DefaultNamespace(project.RootNamespace, relativeDir),
                    ResxNaming.DefaultClassName(kvp.Key.BaseName),
                    project.AssemblyName,
                    entries);
            })
            .ToList();
    }

    private static ProjectInfo ResolveProject(string root, LocalizationCheckOptions options)
    {
        var projectFiles = Directory.GetFiles(root, "*.csproj");
        var projectName = projectFiles.Length == 1
            ? Path.GetFileNameWithoutExtension(projectFiles[0])
            : new DirectoryInfo(root).Name;

        return new ProjectInfo(
            options.AssemblyName ?? projectName,
            options.RootNamespace ?? projectName.Replace(' ', '_'));
    }

    private static IEnumerable<string> EnumerateFiles(string root, string searchPattern, LocalizationCheckOptions options)
    {
        foreach (var path in Directory.EnumerateFiles(root, searchPattern, SearchOption.AllDirectories))
        {
            if (!IsExcluded(path, options))
            {
                yield return path;
            }
        }
    }

    private static bool IsExcluded(string path, LocalizationCheckOptions options)
    {
        foreach (var segment in path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (options.ExcludedDirectoryNames.Contains(segment))
            {
                return true;
            }
        }

        return false;
    }

    private sealed record ProjectInfo(string AssemblyName, string RootNamespace)
    {
        public bool IsSameAssembly(string assemblyName) => string.Equals(assemblyName, AssemblyName, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ResourceGroup
    {
        public ResourceGroup(
            string? neutralPath,
            List<(string Culture, string Path)> satellites,
            string baseName,
            string? @namespace,
            string className,
            string assemblyName,
            List<(string Key, string Value)> entries)
        {
            var keys = entries.Select(e => e.Key).ToList();

            NeutralPath = neutralPath;
            Satellites = satellites;
            BaseName = baseName;
            Namespace = @namespace;
            ClassName = className;
            Source = ResxNaming.Source(assemblyName, baseName);
            Values = entries.ToDictionary(e => e.Key, e => e.Value, StringComparer.Ordinal);
            Keys = keys;
            KeySet = new HashSet<string>(keys, StringComparer.Ordinal);

            var identifiers = ResxNaming.AssignIdentifiers(keys);
            KeysByIdentifier = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < keys.Count; i++)
            {
                KeysByIdentifier[identifiers[i]] = keys[i];
            }
        }

        public string? NeutralPath { get; }
        public List<(string Culture, string Path)> Satellites { get; }
        public string BaseName { get; }
        public string? Namespace { get; }
        public string ClassName { get; }
        public string Source { get; }
        public List<string> Keys { get; }
        public HashSet<string> KeySet { get; }

        /// <summary>Neutral resx key → value.</summary>
        public Dictionary<string, string> Values { get; }

        /// <summary>Generated constant name → resx key name.</summary>
        public Dictionary<string, string> KeysByIdentifier { get; }
    }

    private sealed record UsageContext(
        string XamlPath,
        ProjectInfo Project,
        IReadOnlyList<ResourceGroup> NeutralGroups,
        IReadOnlyDictionary<string, string> Xmlns,
        bool CheckFormats)
    {
        public ResourceGroup? FindByBaseName(string baseName) =>
            NeutralGroups.FirstOrDefault(g => string.Equals(g.BaseName, baseName, StringComparison.Ordinal));

        public ResourceGroup? FindByClass(string @namespace, string className) =>
            NeutralGroups.FirstOrDefault(g => g.Namespace == @namespace && g.ClassName == className);
    }

    /// <summary>
    /// One <c>LocalizedValue</c> usage's statically visible arguments. <see cref="Key"/> and
    /// <see cref="Source"/> are raw - a literal, or a nested markup extension.
    /// </summary>
    private sealed record XamlUsage(
        int LineNumber,
        string? Key,
        string? KeyPrefix,
        string? Source,
        bool HasAssembly,
        bool HasKeyBinding,
        int? ArgumentCount);

    private enum ResolvedKind
    {
        /// <summary>The argument wasn't given.</summary>
        None,

        /// <summary>A literal, or an <c>x:Static</c> to a generated constant - see <see cref="Resolved.Value"/>.</summary>
        Value,

        /// <summary>An <c>x:Static</c> into another assembly.</summary>
        CrossAssembly,

        /// <summary>An <c>x:Static</c> to a generated keys class, naming a member it doesn't have.</summary>
        UndefinedMember,

        /// <summary>Anything else - not statically knowable.</summary>
        Unresolvable,
    }

    private readonly record struct Resolved(ResolvedKind Kind, string? Value, string Display);
}
