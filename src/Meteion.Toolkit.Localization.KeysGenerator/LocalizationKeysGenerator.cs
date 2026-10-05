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
/// Each constant's value is the qualified key <c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;:&lt;Key&gt;</c>
/// (see <c>LocalizationKey</c>), so it resolves from the right assembly and resx no matter
/// where it's used. Each class also gets a <c>ResxSource</c> constant -
/// <c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;</c> - for <c>LocalizedValue.Source</c>.
/// </remarks>
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

    /// <summary>
    /// Warning reported when a resx entry is itself named <c>ResxSource</c>, which would clash with the
    /// generated <c>ResxSource</c> constant. The entry's constant is given a different identifier instead.
    /// </summary>
    internal static readonly DiagnosticDescriptor ResxSourceKeyCollision = new(
        id: "MTKGEN001",
        title: "Resx key collides with the generated ResxSource constant",
        messageFormat: "Resx key 'ResxSource' in '{0}' collides with the generated ResxSource constant; its key constant is generated as '{1}' instead",
        category: "Meteion.Toolkit.Localization",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Whether the marker attribute type is referenced decides if generated classes are annotated with it.
        var compilationInfo = context.CompilationProvider
            .Select(static (compilation, _) => (
                AssemblyName: compilation.AssemblyName ?? string.Empty,
                HasMarkerAttribute: compilation.GetTypeByMetadataName(GeneratedLocalizationKeysAttributeFullName) is not null));

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
            .Combine(compilationInfo)
            .Select(static (combined, ct) =>
            {
                var (((text, optionsProvider), defaultRootNamespace), (assemblyName, includeMarkerAttribute)) = combined;
                return TryCreateGeneratedFile(text, optionsProvider.GetOptions(text), defaultRootNamespace, assemblyName, includeMarkerAttribute, ct);
            })
            .Where(static file => file is not null);

        context.RegisterSourceOutput(generatedFiles, static (spc, file) =>
        {
            spc.AddSource(file!.HintName, file.Source);
            foreach (var diagnostic in file.Diagnostics)
            {
                spc.ReportDiagnostic(diagnostic);
            }
        });
    }

    /// <summary>
    /// Builds the generated keys class for one resx file, applying any per-file MSBuild metadata overrides
    /// for class name, namespace and resource base name.
    /// </summary>
    /// <param name="text">The resx file from <c>AdditionalFiles</c>.</param>
    /// <param name="fileOptions">Analyzer config options carrying the file's MSBuild item metadata.</param>
    /// <param name="defaultRootNamespace">The project's <c>RootNamespace</c>, used when no override is set.</param>
    /// <param name="assemblyName">Name of the assembly being compiled; the first part of each qualified key.</param>
    /// <param name="includeMarkerAttribute">Whether to annotate the class with the generated-keys marker attribute.</param>
    /// <param name="cancellationToken">Token used to cancel reading the resx.</param>
    /// <returns>The generated file, or <see langword="null"/> for satellite or unreadable resx files.</returns>
    private static GeneratedFile? TryCreateGeneratedFile(
        AdditionalText text,
        AnalyzerConfigOptions fileOptions,
        string? defaultRootNamespace,
        string assemblyName,
        bool includeMarkerAttribute,
        CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileNameWithoutExtension(text.Path);
        if (ResxNaming.SplitCultureSuffix(fileName).Culture is not null)
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
        fileOptions.TryGetValue("build_metadata.AdditionalFiles.MeteionResourceBaseName", out var baseNameOverride);

        // "RelativeDir" is well-known MSBuild item metadata - the item's directory, relative
        // to the project, with a trailing separator (e.g. "Resources\" or "" for the project
        // root) - computed automatically for every item, no extra setup needed.
        fileOptions.TryGetValue("build_metadata.AdditionalFiles.RelativeDir", out var relativeDir);

        var className = !string.IsNullOrWhiteSpace(classNameOverride)
            ? classNameOverride!
            : ResxNaming.DefaultClassName(fileName);

        var @namespace = !string.IsNullOrWhiteSpace(namespaceOverride)
            ? namespaceOverride!
            : ResxNaming.DefaultNamespace(defaultRootNamespace, relativeDir);

        var baseName = !string.IsNullOrWhiteSpace(baseNameOverride)
            ? baseNameOverride!
            : ResxNaming.DefaultBaseName(defaultRootNamespace, relativeDir, fileName);

        var diagnostics = new List<Diagnostic>();
        var source = GenerateSource(@namespace, className, text.Path, assemblyName, baseName, entries, includeMarkerAttribute, diagnostics);
        var hintName = SanitizeHintName($"{(@namespace is null ? "" : @namespace + ".")}{className}") + ".g.cs";

        return new GeneratedFile(hintName, SourceText.From(source, Encoding.UTF8), diagnostics);
    }

    /// <summary>
    /// Reads the string entries of a resx, ignoring non-string resources and duplicate names.
    /// </summary>
    /// <param name="text">The resx file to read.</param>
    /// <param name="cancellationToken">Token used to cancel parsing.</param>
    /// <returns>
    /// The entry names, neutral values and 1-based source lines, or <see langword="null"/> if the file is
    /// missing or not well-formed XML.
    /// </returns>
    private static List<(string Name, string Value, int Line)>? ReadStringEntries(AdditionalText text, CancellationToken cancellationToken)
    {
        var sourceText = text.GetText(cancellationToken);
        if (sourceText is null)
        {
            return null;
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(sourceText.ToString(), LoadOptions.SetLineInfo);
        }
        catch (System.Xml.XmlException)
        {
            return null;
        }

        if (document.Root is null)
        {
            return null;
        }

        var entries = new List<(string Name, string Value, int Line)>();
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
            entries.Add((name!, value, ((System.Xml.IXmlLineInfo)data).LineNumber));
        }

        return entries;
    }

    /// <summary>
    /// Renders the C# source of the keys class: a <c>ResxSource</c> constant plus one documented constant per entry.
    /// </summary>
    /// <param name="namespace">Namespace to wrap the class in, or <see langword="null"/> for the global namespace.</param>
    /// <param name="className">Name of the generated static class.</param>
    /// <param name="resxPath">Path of the source resx, recorded in the file header and marker attribute.</param>
    /// <param name="assemblyName">Assembly name used to qualify keys.</param>
    /// <param name="baseName">Resource base name used to qualify keys.</param>
    /// <param name="entries">The resx string entries to emit constants for.</param>
    /// <param name="includeMarkerAttribute">Whether to emit the generated-keys marker attribute.</param>
    /// <param name="diagnostics">Collects diagnostics found while generating, such as identifier collisions.</param>
    /// <returns>The generated source text.</returns>
    private static string GenerateSource(
        string? @namespace,
        string className,
        string resxPath,
        string assemblyName,
        string baseName,
        List<(string Name, string Value, int Line)> entries,
        bool includeMarkerAttribute,
        List<Diagnostic> diagnostics)
    {
        var resxSource = ResxNaming.Source(assemblyName, baseName);

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
        builder.AppendLine($"{indent}/// Resource keys generated from <c>{XmlEscape(Path.GetFileName(resxPath))}</c>. Each field's");
        builder.AppendLine($"{indent}/// value is the qualified key (for use with <c>LocalizedValueExtension.Key</c> et al.) -");
        builder.AppendLine($"{indent}/// see the field's own doc comment for the neutral-culture resx value.");
        builder.AppendLine($"{indent}/// </summary>");
        builder.AppendLine($"{indent}[global::System.CodeDom.Compiler.GeneratedCode(\"Meteion.Toolkit.Localization.KeysGenerator\", null)]");

        if (includeMarkerAttribute)
        {
            builder.AppendLine($"{indent}[global::{GeneratedLocalizationKeysAttributeFullName}({QuoteLiteral(resxPath)}, ResxSource = {QuoteLiteral(resxSource)})]");
        }

        builder.AppendLine($"{indent}public static partial class {className}");
        builder.AppendLine($"{indent}{{");

        builder.AppendLine($"{indent}    /// <summary>");
        builder.AppendLine($"{indent}    /// Identifies this resx (<c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;</c>) - for <c>LocalizedValueExtension.Source</c>.");
        builder.AppendLine($"{indent}    /// </summary>");
        builder.AppendLine($"{indent}    public const string {ResxNaming.ResxSourceFieldName} = {QuoteLiteral(resxSource)};");
        builder.AppendLine();

        var identifiers = ResxNaming.AssignIdentifiers(entries.Select(e => e.Name));

        // Every constant name is taken up front, so a generated Format helper can't collide with
        // a resx key that happens to be called e.g. "FormatGreeting".
        var usedIdentifiers = new HashSet<string>(identifiers, StringComparer.Ordinal) { ResxNaming.ResxSourceFieldName };

        for (var i = 0; i < entries.Count; i++)
        {
            var (name, value, line) = entries[i];
            var identifier = identifiers[i];
            if (name == ResxNaming.ResxSourceFieldName)
            {
                var position = new LinePosition(Math.Max(line - 1, 0), 0);
                diagnostics.Add(Diagnostic.Create(
                    ResxSourceKeyCollision,
                    Location.Create(resxPath, default, new LinePositionSpan(position, position)),
                    Path.GetFileName(resxPath),
                    identifier));
            }

            builder.AppendLine($"{indent}    /// <summary>");
            AppendValueDoc(builder, indent, value);
            builder.AppendLine($"{indent}    /// </summary>");
            builder.AppendLine($"{indent}    public const string {identifier} = {QuoteLiteral(ResxNaming.QualifiedKey(assemblyName, baseName, name))};");
            builder.AppendLine();

            // The helper calls ILocalizationService.GetFormattedString, so it's only emitted when the
            // project references the abstractions assembly (the same condition as the marker attribute).
            if (includeMarkerAttribute &&
                CompositeFormat.TryAnalyze(value, out var placeholders, out _) &&
                placeholders.Count > 0)
            {
                AppendFormatHelper(builder, indent, identifier, value, CompositeFormat.RequiredArgumentCount(placeholders), usedIdentifiers);
            }
        }

        builder.AppendLine($"{indent}}}");

        if (@namespace is not null)
        {
            builder.AppendLine("}");
        }

        return builder.ToString();
    }

    /// <summary>
    /// Writes a <c>Format&lt;Key&gt;</c> helper for a resx entry that is a composite format string, so a
    /// caller passes the right number of arguments by name instead of building an array by hand.
    /// </summary>
    /// <param name="builder">Builder receiving the generated source.</param>
    /// <param name="indent">Indentation of the class the helper belongs to.</param>
    /// <param name="identifier">The key's constant name; the helper formats that constant's key.</param>
    /// <param name="value">The neutral resx value, shown in the doc comment.</param>
    /// <param name="argumentCount">How many arguments the string needs.</param>
    /// <param name="usedIdentifiers">Every member name in the class; the helper's name is added to it.</param>
    private static void AppendFormatHelper(
        StringBuilder builder, string indent, string identifier, string value, int argumentCount, HashSet<string> usedIdentifiers)
    {
        var helperName = "Format" + identifier.TrimStart('@');
        var suffix = 2;
        while (!usedIdentifiers.Add(helperName))
        {
            helperName = "Format" + identifier.TrimStart('@') + "_" + suffix++;
        }

        var arguments = string.Join(", ", Enumerable.Range(0, argumentCount).Select(n => "arg" + n));
        var parameters = string.Join(", ", Enumerable.Range(0, argumentCount).Select(n => "object? arg" + n));

        builder.AppendLine($"{indent}    /// <summary>");
        builder.AppendLine($"{indent}    /// Resolves <see cref=\"{identifier}\"/> and formats it with {argumentCount} argument(s) using the");
        builder.AppendLine($"{indent}    /// service's current culture. Neutral-culture value:");
        AppendValueDoc(builder, indent, value);
        builder.AppendLine($"{indent}    /// </summary>");
        builder.AppendLine($"{indent}    /// <param name=\"service\">The localization service to resolve and format with.</param>");
        for (var n = 0; n < argumentCount; n++)
        {
            builder.AppendLine($"{indent}    /// <param name=\"arg{n}\">The value for <c>{{{n}}}</c>.</param>");
        }

        builder.AppendLine($"{indent}    /// <returns>The formatted text.</returns>");
        builder.AppendLine($"{indent}    public static string {helperName}(global::Meteion.Toolkit.Localization.Abstractions.ILocalizationService service, {parameters}) =>");
        builder.AppendLine($"{indent}        service.GetFormattedString({identifier}, new object?[] {{ {arguments} }});");
        builder.AppendLine();
    }

    /// <summary>
    /// Writes <paramref name="value"/> as a <c>&lt;c&gt;</c>-wrapped doc-comment body. A resx
    /// <c>&lt;value&gt;</c> can legitimately span multiple lines (e.g. wizard instructions) - a
    /// naive single <c>AppendLine</c> would embed those line breaks as bare, un-prefixed lines
    /// in the generated <c>.g.cs</c> file, which fall out of the doc comment and become invalid
    /// top-level statements, cascading into a wall of build errors from one long string. Every
    /// physical line gets its own <c>///</c> prefix instead.
    /// </summary>
    /// <param name="builder">Builder receiving the generated source.</param>
    /// <param name="indent">Indentation of the member the doc comment belongs to.</param>
    /// <param name="value">The resx value to render; XML-escaped before writing.</param>
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

    private static string SanitizeHintName(string name)
    {
        var builder = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            builder.Append(char.IsLetterOrDigit(c) || c is '_' or '.' or '-' ? c : '_');
        }

        return builder.ToString();
    }

    private static string QuoteLiteral(string value) =>
        Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(value, quote: true);

    private static string XmlEscape(string value) => new XText(value).ToString();

    /// <summary>
    /// A generated source file together with the diagnostics found while producing it.
    /// </summary>
    /// <param name="hintName">File name passed to the compiler for the generated source.</param>
    /// <param name="source">The generated source text.</param>
    /// <param name="diagnostics">Diagnostics to report alongside the source.</param>
    private sealed class GeneratedFile(string hintName, SourceText source, IReadOnlyList<Diagnostic> diagnostics)
    {
        public string HintName { get; } = hintName;
        public SourceText Source { get; } = source;
        public IReadOnlyList<Diagnostic> Diagnostics { get; } = diagnostics;
    }
}
