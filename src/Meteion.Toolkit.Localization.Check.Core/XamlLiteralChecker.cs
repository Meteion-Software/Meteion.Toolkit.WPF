using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Meteion.Toolkit.Localization.Check;

/// <summary>
/// Finds plain (un-localized) string literals in the user-visible properties and element text
/// of a XAML file (LOC009), and <c>loc-ignore</c> suppressions that give no reason (LOC010).
/// </summary>
internal static class XamlLiteralChecker
{
    /// <summary>Properties checked by default: the WPF ones, plus the Telerik equivalents.</summary>
    private static readonly string[] DefaultProperties =
    [
        "Text", "Content", "Header", "ToolTip", "Title", "Watermark", "AutomationProperties.Name",
        "NullText", "EmptyText", "WatermarkContent", "Label", "Caption",
    ];

    /// <summary>
    /// Elements whose direct text content is displayed (<c>&lt;Button&gt;Save&lt;/Button&gt;</c>).
    /// A Telerik <c>Rad</c>-prefixed variant of any of these counts too.
    /// </summary>
    private static readonly HashSet<string> ContentElements = new(StringComparer.Ordinal)
    {
        "TextBlock", "Run", "Span", "Bold", "Italic", "Underline", "Hyperlink", "Label",
        "Button", "ToggleButton", "RepeatButton", "CheckBox", "RadioButton", "MenuItem",
        "TabItem", "ListBoxItem", "ComboBoxItem", "ListViewItem", "TreeViewItem", "ToolTip",
        "ContentControl",
    };

    private static readonly Regex IgnorePattern = new(
        @"^\s*loc-ignore\b\s*(?::\s*(?<reason>.*?))?\s*$",
        RegexOptions.Compiled | RegexOptions.Singleline);

    public static IEnumerable<LocalizationKeyUsageIssue> Check(string xamlPath, LocalizationCheckOptions options)
    {
        XDocument document;
        try
        {
            document = XDocument.Load(xamlPath, LoadOptions.SetLineInfo);
        }
        catch (Exception ex) when (ex is IOException or XmlException)
        {
            return [];
        }

        if (document.Root is null)
        {
            return [];
        }

        var properties = new HashSet<string>(DefaultProperties, StringComparer.Ordinal);
        foreach (var extra in options.AdditionalLiteralProperties)
        {
            properties.Add(extra);
        }

        var issues = new List<LocalizationKeyUsageIssue>();
        foreach (var element in document.Root.DescendantsAndSelf())
        {
            CheckElement(element, xamlPath, properties, issues);
        }

        return issues;
    }

    private static void CheckElement(XElement element, string xamlPath, HashSet<string> properties, List<LocalizationKeyUsageIssue> issues)
    {
        var ignore = FindIgnore(element);
        if (ignore is { HasReason: false })
        {
            issues.Add(new LocalizationKeyUsageIssue("loc-ignore", xamlPath, ignore.Line, LocalizationKeyUsageIssueKind.IgnoreWithoutReason));
        }

        var localName = element.Name.LocalName;
        var dot = localName.IndexOf('.');
        if (dot >= 0)
        {
            // A property element (<Button.Content>) belongs to its owner, so an ignore comment
            // on either the owner or the property element itself covers it.
            var ownerIgnored = element.Parent is { } owner && FindIgnore(owner) is not null;
            if (ignore is null && !ownerIgnored &&
                properties.Contains(localName[(dot + 1)..]) &&
                !element.HasElements &&
                GetLiteral(element.Value) is { } literal)
            {
                Report(element, xamlPath, $"{localName[(dot + 1)..]} \"{literal}\"", issues);
            }

            return;
        }

        if (ignore is not null)
        {
            return;
        }

        foreach (var attribute in element.Attributes())
        {
            if (!attribute.IsNamespaceDeclaration &&
                properties.Contains(attribute.Name.LocalName) &&
                GetLiteral(attribute.Value) is { } literal)
            {
                Report(attribute, xamlPath, $"{attribute.Name.LocalName}=\"{literal}\"", issues);
            }
        }

        if (IsContentElement(localName))
        {
            foreach (var text in element.Nodes().OfType<XText>())
            {
                if (GetLiteral(text.Value) is { } literal)
                {
                    Report(text, xamlPath, $"<{localName}> content \"{literal}\"", issues);
                }
            }
        }
    }

    private static void Report(IXmlLineInfo lineInfo, string xamlPath, string description, List<LocalizationKeyUsageIssue> issues) =>
        issues.Add(new LocalizationKeyUsageIssue(description, xamlPath, lineInfo.HasLineInfo() ? lineInfo.LineNumber : 0, LocalizationKeyUsageIssueKind.UnlocalizedLiteral));

    private static bool IsContentElement(string localName) =>
        ContentElements.Contains(localName) ||
        (localName.StartsWith("Rad", StringComparison.Ordinal) && ContentElements.Contains(localName[3..]));

    /// <summary>
    /// The trimmed value if it's a plain literal worth localizing - not a markup extension and
    /// containing at least one letter (so ":", "…" and "1" are left alone) - otherwise null.
    /// </summary>
    private static string? GetLiteral(string rawValue)
    {
        var value = rawValue.Trim();

        // "{}" is XAML's escape for a value that starts with a literal brace.
        if (value.StartsWith("{}", StringComparison.Ordinal))
        {
            value = value[2..].TrimStart();
        }
        else if (value.StartsWith('{'))
        {
            return null;
        }

        return value.Any(char.IsLetter) ? value : null;
    }

    /// <summary>
    /// A <c>&lt;!-- loc-ignore: reason --&gt;</c> comment immediately before the element, or null.
    /// </summary>
    private static IgnoreComment? FindIgnore(XElement element)
    {
        var previous = element.PreviousNode;
        while (previous is XText { Value: var text } && string.IsNullOrWhiteSpace(text))
        {
            previous = previous.PreviousNode;
        }

        if (previous is not XComment comment || IgnorePattern.Match(comment.Value) is not { Success: true } match)
        {
            return null;
        }

        var line = ((IXmlLineInfo)comment).HasLineInfo() ? ((IXmlLineInfo)comment).LineNumber : 0;
        return new IgnoreComment(line, match.Groups["reason"].Value.Length > 0);
    }

    private sealed record IgnoreComment(int Line, bool HasReason);
}
