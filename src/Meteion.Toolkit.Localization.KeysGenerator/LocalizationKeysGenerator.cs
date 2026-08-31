using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Meteion.Toolkit.Localization.KeysGenerator;

/// <summary>
/// Emits a <c>public const string</c> class - name plus the resx's neutral-culture value as
/// an XML-doc summary - for every string entry in each neutral (culture-less) .resx found in
/// the project's <c>AdditionalFiles</c>, so both code-behind and (via <c>x:Static</c> or the
/// <c>Key="..."</c> IntelliSense dropdown) XAML get autocompletion over
/// <c>LocalizedValueExtension</c> resource keys.
/// </summary>
/// <remarks>
/// Deliberately keyed off a project's <c>AdditionalFiles</c> (not <c>EmbeddedResource</c>) -
/// analyzers/generators can only see the former. The shipped
/// <c>build\Meteion.Toolkit.Localization.KeysGenerator.props</c> adds every <c>*.resx</c> as
/// an <c>AdditionalFiles</c> item automatically so consumers don't have to.
///
/// Satellite (culture-suffixed) resx files - e.g. <c>Resources.ja-JP.resx</c> - are skipped:
/// only the neutral resx is a family's source of truth for which keys exist, matching
/// <c>LocalizationKeyChecker</c>'s convention in Meteion.Toolkit.Localization.Check.Core.
/// </remarks>
[Generator(LanguageNames.CSharp)]
public sealed class LocalizationKeysGenerator : IIncrementalGenerator
{
    private const string GeneratedLocalizationKeysAttributeFullName =
        "Meteion.Toolkit.Localization.Abstractions.GeneratedLocalizationKeysAttribute";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var hasMarkerAttribute = context.CompilationProvider
            .Select(static (compilation, _) =>
                compilation.GetTypeByMetadataName(GeneratedLocalizationKeysAttributeFullName) is not null);

        var resxFiles = context.AdditionalTextsProvider
            .Where(static text => text.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase));

        var rootNamespace = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) =>
                provider.GlobalOptions.TryGetValue("build_property.RootNamespace", out var value) && !string.IsNullOrWhiteSpace(value)
                    ? value
                    : null);

        var perFileOptions = resxFiles.Combine(context.AnalyzerConfigOptionsProvider);

        var generatedFiles = perFileOptions
            .Combine(rootNamespace)
            .Combine(hasMarkerAttribute)
            .Select(static (combined, ct) =>
            {
                var (((text, optionsProvider), defaultRootNamespace), includeMarkerAttribute) = combined;
                return TryCreateGeneratedFile(text, optionsProvider.GetOptions(text), defaultRootNamespace, includeMarkerAttribute, ct);
            })
            .Where(static file => file is not null);

        context.RegisterSourceOutput(generatedFiles, static (spc, file) =>
            spc.AddSource(file!.HintName, file.Source));
    }

    private static GeneratedFile? TryCreateGeneratedFile(
        AdditionalText text,
        AnalyzerConfigOptions fileOptions,
        string? defaultRootNamespace,
        bool includeMarkerAttribute,
        CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileNameWithoutExtension(text.Path);
        if (HasCultureSuffix(fileName))
        {
            // A satellite translation, e.g. "Resources.ja-JP" - only the neutral resx defines
            // the key set.
            return null;
        }

        var entries = ReadStringEntries(text, cancellationToken);
        if (entries is null)
        {
            // Missing, unreadable, or not a well-formed resx - nothing to generate. (A
            // malformed resx is also a build error from the normal EmbeddedResource compile
            // step, so this doesn't silently hide anything.)
            return null;
        }

        fileOptions.TryGetValue("build_metadata.AdditionalFiles.MeteionKeysClassName", out var classNameOverride);
        fileOptions.TryGetValue("build_metadata.AdditionalFiles.MeteionKeysNamespace", out var namespaceOverride);

        var className = !string.IsNullOrWhiteSpace(classNameOverride)
            ? classNameOverride!
            : SanitizeIdentifier(fileName) + "Keys";

        var @namespace = !string.IsNullOrWhiteSpace(namespaceOverride)
            ? namespaceOverride!
            : BuildDefaultNamespace(defaultRootNamespace, fileOptions);

        var source = GenerateSource(@namespace, className, text.Path, entries, includeMarkerAttribute);
        var hintName = SanitizeHintName($"{(@namespace is null ? "" : @namespace + ".")}{className}") + ".g.cs";

        return new GeneratedFile(hintName, SourceText.From(source, Encoding.UTF8));
    }

    private static string? BuildDefaultNamespace(string? rootNamespace, AnalyzerConfigOptions fileOptions)
    {
        // "RelativeDir" is well-known MSBuild item metadata - the item's directory, relative
        // to the project, with a trailing separator (e.g. "Resources\" or "" for the project
        // root) - computed automatically for every item, no extra setup needed.
        fileOptions.TryGetValue("build_metadata.AdditionalFiles.RelativeDir", out var relativeDir);

        IEnumerable<string> segments = string.IsNullOrEmpty(relativeDir)
            ? Array.Empty<string>()
            : relativeDir!.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).Select(SanitizeIdentifier);

        var parts = string.IsNullOrWhiteSpace(rootNamespace)
            ? segments
            : new[] { rootNamespace! }.Concat(segments);

        var @namespace = string.Join(".", parts);
        return string.IsNullOrEmpty(@namespace) ? null : @namespace;
    }

    private static List<(string Name, string Value)>? ReadStringEntries(AdditionalText text, CancellationToken cancellationToken)
    {
        var sourceText = text.GetText(cancellationToken);
        if (sourceText is null)
        {
            return null;
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(sourceText.ToString());
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }

        if (document.Root is null)
        {
            return null;
        }

        var entries = new List<(string Name, string Value)>();
        var seenNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var data in document.Root.Elements("data"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Skip non-string resources (images, icons, byte arrays, ...) - same rule as
            // LocalizationKeyChecker: a non-string entry carries a "type" or "mimetype".
            if (data.Attribute("type") is not null || data.Attribute("mimetype") is not null)
            {
                continue;
            }

            var name = data.Attribute("name")?.Value;
            if (string.IsNullOrEmpty(name) || !seenNames.Add(name!))
            {
                continue;
            }

            var value = data.Element("value")?.Value ?? string.Empty;
            entries.Add((name!, value));
        }

        return entries;
    }

    private static string GenerateSource(
        string? @namespace,
        string className,
        string resxPath,
        List<(string Name, string Value)> entries,
        bool includeMarkerAttribute)
    {
        var builder = new StringBuilder();
        builder.AppendLine("// <auto-generated>");
        builder.AppendLine("// Generated by Meteion.Toolkit.Localization.KeysGenerator from:");
        builder.AppendLine($"//   {resxPath}");
        builder.AppendLine("// Changes to this file will be lost when it's regenerated.");
        builder.AppendLine("// </auto-generated>");
        builder.AppendLine("#nullable enable");
        builder.AppendLine();

        var indent = "";
        if (@namespace is not null)
        {
            builder.AppendLine($"namespace {@namespace}");
            builder.AppendLine("{");
            indent = "    ";
        }

        builder.AppendLine($"{indent}/// <summary>");
        builder.AppendLine($"{indent}/// Resource key names generated from <c>{XmlEscape(Path.GetFileName(resxPath))}</c>. Each field's");
        builder.AppendLine($"{indent}/// value is the key itself (for use with <c>LocalizedValueExtension.Key</c> et al.) -");
        builder.AppendLine($"{indent}/// see the field's own doc comment for the neutral-culture resx value.");
        builder.AppendLine($"{indent}/// </summary>");
        builder.AppendLine($"{indent}[global::System.CodeDom.Compiler.GeneratedCode(\"Meteion.Toolkit.Localization.KeysGenerator\", null)]");

        if (includeMarkerAttribute)
        {
            builder.AppendLine($"{indent}[global::{GeneratedLocalizationKeysAttributeFullName}({QuoteLiteral(resxPath)})]");
        }

        builder.AppendLine($"{indent}public static partial class {className}");
        builder.AppendLine($"{indent}{{");

        var usedIdentifiers = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (name, value) in entries)
        {
            var identifier = MakeUniqueIdentifier(name, usedIdentifiers);

            builder.AppendLine($"{indent}    /// <summary>");
            AppendValueDoc(builder, indent, value);
            builder.AppendLine($"{indent}    /// </summary>");
            builder.AppendLine($"{indent}    public const string {identifier} = {QuoteLiteral(name)};");
            builder.AppendLine();
        }

        builder.AppendLine($"{indent}}}");

        if (@namespace is not null)
        {
            builder.AppendLine("}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Writes <paramref name="value"/> as a <c>&lt;c&gt;</c>-wrapped doc-comment body. A resx
    /// <c>&lt;value&gt;</c> can legitimately span multiple lines (e.g. wizard instructions) - a
    /// naive single <c>AppendLine</c> would embed those line breaks as bare, un-prefixed lines
    /// in the generated <c>.g.cs</c> file, which fall out of the doc comment and become invalid
    /// top-level statements, cascading into a wall of build errors from one long string. Every
    /// physical line gets its own <c>///</c> prefix instead.
    /// </summary>
    private static void AppendValueDoc(StringBuilder builder, string indent, string value)
    {
        var lines = value.Replace("\r\n", "\n").Split('\n');
        if (lines.Length == 1)
        {
            builder.AppendLine($"{indent}    /// <c>\"{XmlEscape(lines[0])}\"</c>");
            return;
        }

        builder.AppendLine($"{indent}    /// <c>");
        foreach (var line in lines)
        {
            builder.AppendLine($"{indent}    /// {XmlEscape(line)}");
        }

        builder.AppendLine($"{indent}    /// </c>");
    }

    private static string MakeUniqueIdentifier(string name, HashSet<string> usedIdentifiers)
    {
        var identifier = SanitizeIdentifier(name);

        if (usedIdentifiers.Add(identifier))
        {
            return identifier;
        }

        // Two resx key names that sanitize to the same identifier (e.g. "My.Key" and
        // "My_Key") - keep both, deterministically, rather than dropping one silently.
        var suffix = 2;
        string candidate;
        do
        {
            candidate = identifier + "_" + suffix.ToString(CultureInfo.InvariantCulture);
            suffix++;
        } while (!usedIdentifiers.Add(candidate));

        return candidate;
    }

    /// <summary>
    /// Converts an arbitrary resx key name (or path segment) into a valid C# identifier -
    /// invalid characters become <c>_</c>, a leading digit gets a <c>_</c> prefix, and a
    /// reserved keyword gets an <c>@</c> prefix.
    /// </summary>
    private static string SanitizeIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return "_";
        }

        var builder = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            builder.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
        }

        if (char.IsDigit(builder[0]))
        {
            builder.Insert(0, '_');
        }

        var identifier = builder.ToString();
        return CSharpKeywords.Contains(identifier) ? "@" + identifier : identifier;
    }

    private static string SanitizeHintName(string name)
    {
        var builder = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            builder.Append(char.IsLetterOrDigit(c) || c is '_' or '.' or '-' ? c : '_');
        }

        return builder.ToString();
    }

    private static bool HasCultureSuffix(string fileNameWithoutExtension)
    {
        var lastDot = fileNameWithoutExtension.LastIndexOf('.');
        if (lastDot < 0)
        {
            return false;
        }

        var candidate = fileNameWithoutExtension.Substring(lastDot + 1);

        try
        {
            var culture = CultureInfo.GetCultureInfo(candidate);
            return !string.IsNullOrEmpty(culture.Name);
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }

    private static string QuoteLiteral(string value) =>
        Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(value, quote: true);

    private static string XmlEscape(string value) => new XText(value).ToString();

    private static readonly HashSet<string> CSharpKeywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
        "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
        "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while",
    ];

    private sealed class GeneratedFile(string hintName, SourceText source)
    {
        public string HintName { get; } = hintName;
        public SourceText Source { get; } = source;
    }
}
