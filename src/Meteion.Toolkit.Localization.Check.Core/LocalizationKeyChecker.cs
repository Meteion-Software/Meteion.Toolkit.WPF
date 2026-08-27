using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Meteion.Toolkit.Localization.Check;

/// <summary>
/// Scans a directory tree for .resx localization resources and (optionally) .xaml files,
/// reporting keys missing from a satellite resx, keys orphaned in a satellite resx, and
/// XAML usages of a key that exists in no scanned neutral resx.
/// </summary>
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

    private static readonly Regex XmlnsDeclarationPattern = new(
        "xmlns:(?<prefix>\\w+)\\s*=\\s*\"(?<uri>[^\"]*)\"",
        RegexOptions.Compiled);

    private static readonly Regex KeyBindingTokenPattern = new(
        @"\bKeyBinding\s*=",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex KeyAttributePattern = new(
        "\\bKey\\s*=\\s*\"(?<value>[^\"]*)\"",
        RegexOptions.Compiled);

    /// <summary>
    /// Scans <paramref name="rootDirectory"/> and reports resx sync issues and (unless
    /// disabled) XAML usages of undefined keys.
    /// </summary>
    public static LocalizationCheckResult CheckDirectory(string rootDirectory, LocalizationCheckOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        options ??= new LocalizationCheckOptions();

        if (!Directory.Exists(rootDirectory))
        {
            return new LocalizationCheckResult([], []);
        }

        var resourceGroups = DiscoverResourceGroups(rootDirectory, options);
        var resourceIssues = CheckResourceGroups(resourceGroups, options);
        var usageIssues = options.CheckXamlUsages
            ? CheckXamlUsages(rootDirectory, options, resourceGroups)
            : [];

        return new LocalizationCheckResult(resourceIssues, usageIssues);
    }

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

            var neutralKeys = ReadStringKeys(group.NeutralPath);

            foreach (var (culture, satellitePath) in group.Satellites)
            {
                var satelliteKeys = ReadStringKeys(satellitePath);

                foreach (var key in neutralKeys)
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
                        if (!neutralKeys.Contains(key))
                        {
                            issues.Add(new LocalizationKeyIssue(LocalizationKeyIssueKind.OrphanKey, key, group.NeutralPath, satellitePath, culture));
                        }
                    }
                }
            }
        }

        return issues;
    }

    private static List<LocalizationKeyUsageIssue> CheckXamlUsages(
        string rootDirectory,
        LocalizationCheckOptions options,
        IReadOnlyList<ResourceGroup> resourceGroups)
    {
        var knownKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in resourceGroups)
        {
            if (group.NeutralPath is not null)
            {
                knownKeys.UnionWith(ReadStringKeys(group.NeutralPath));
            }
        }

        var issues = new List<LocalizationKeyUsageIssue>();

        foreach (var xamlPath in EnumerateFiles(rootDirectory, "*.xaml", options))
        {
            foreach (var (key, lineNumber) in ExtractLocalizedValueUsages(xamlPath))
            {
                if (!knownKeys.Contains(key))
                {
                    issues.Add(new LocalizationKeyUsageIssue(key, xamlPath, lineNumber));
                }
            }
        }

        return issues;
    }

    private static IEnumerable<(string Key, int LineNumber)> ExtractLocalizedValueUsages(string xamlPath)
    {
        string text;
        try
        {
            text = File.ReadAllText(xamlPath);
        }
        catch (IOException)
        {
            yield break;
        }

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
            var key = ExtractKeyFromMarkupExtensionArgs(text[argsStart..closeIndex]);
            if (key is not null)
            {
                yield return (key, GetLineNumber(text, marker.Index));
            }
        }

        foreach (Match match in elementPattern.Matches(text))
        {
            var key = ExtractKeyAttribute(match.Groups[1].Value);
            if (key is not null)
            {
                yield return (key, GetLineNumber(text, match.Index));
            }
        }
    }

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

    /// <summary>
    /// Extracts a literal <c>Key</c> from the args of a <c>{prefix:LocalizedValue ...}</c>
    /// markup extension usage. Returns null for a dynamic <c>KeyBinding</c>-based usage, or a
    /// <c>Key</c> itself supplied via a nested markup extension (e.g. <c>Key={x:Static
    /// local:Strings.Foo}</c>) - neither can be statically resolved to a literal key here.
    /// </summary>
    private static string? ExtractKeyFromMarkupExtensionArgs(string rawArgs)
    {
        var tokens = SplitTopLevel(rawArgs);
        if (tokens.Count == 0)
        {
            return null;
        }

        if (tokens.Any(t => KeyBindingTokenPattern.IsMatch(t)))
        {
            return null;
        }

        foreach (var token in tokens)
        {
            var eq = token.IndexOf('=');
            if (eq >= 0 && token[..eq].Trim().Equals("Key", StringComparison.Ordinal))
            {
                return UnquoteKeyValue(token[(eq + 1)..]);
            }
        }

        // No named "Key=" - a bare leading token is the constructor's positional `key` arg.
        var first = tokens[0];
        return first.Contains('=') ? null : UnquoteKeyValue(first);
    }

    private static string? ExtractKeyAttribute(string attributesText)
    {
        if (KeyBindingTokenPattern.IsMatch(attributesText))
        {
            return null;
        }

        var match = KeyAttributePattern.Match(attributesText);
        return match.Success ? UnquoteKeyValue(match.Groups["value"].Value) : null;
    }

    /// <summary>
    /// Unquotes a raw <c>Key</c> value, or returns null if it's a nested markup extension
    /// (e.g. <c>{x:Static local:Strings.Foo}</c>) rather than a literal - like <c>KeyBinding</c>,
    /// a key supplied that way can't be statically resolved to a resx key name here, and
    /// isn't the "used a key that doesn't exist" case this check is looking for.
    /// </summary>
    private static string? UnquoteKeyValue(string rawValue)
    {
        var trimmed = rawValue.Trim();
        return trimmed.Length >= 2 && trimmed[0] == '{' && trimmed[^1] == '}'
            ? null
            : Unquote(rawValue);
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
        return value.Length >= 2 && value[0] == '"' && value[^1] == '"'
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

    private static HashSet<string> ReadStringKeys(string resxPath)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);

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
            if (!string.IsNullOrEmpty(name))
            {
                keys.Add(name);
            }
        }

        return keys;
    }

    private static List<ResourceGroup> DiscoverResourceGroups(string root, LocalizationCheckOptions options)
    {
        var builders = new Dictionary<(string Directory, string BaseName), (string? Neutral, List<(string Culture, string Path)> Satellites)>();

        foreach (var path in EnumerateFiles(root, "*.resx", options))
        {
            var directory = Path.GetDirectoryName(path) ?? string.Empty;
            var fileName = Path.GetFileNameWithoutExtension(path);
            var (baseName, culture) = SplitCultureSuffix(fileName);
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

        return builders.Values
            .Select(e => new ResourceGroup(e.Neutral, e.Satellites))
            .ToList();
    }

    private static (string BaseName, string? Culture) SplitCultureSuffix(string fileNameWithoutExtension)
    {
        var lastDot = fileNameWithoutExtension.LastIndexOf('.');
        if (lastDot < 0)
        {
            return (fileNameWithoutExtension, null);
        }

        var candidate = fileNameWithoutExtension[(lastDot + 1)..];

        try
        {
            var culture = CultureInfo.GetCultureInfo(candidate);
            if (!string.IsNullOrEmpty(culture.Name))
            {
                return (fileNameWithoutExtension[..lastDot], culture.Name);
            }
        }
        catch (CultureNotFoundException)
        {
            // Not a culture suffix (e.g. "Resources.Designer") - the whole name is the base.
        }

        return (fileNameWithoutExtension, null);
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

    private sealed record ResourceGroup(string? NeutralPath, List<(string Culture, string Path)> Satellites);
}
