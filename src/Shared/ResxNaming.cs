using System.Globalization;
using System.Text;

namespace Meteion.Toolkit.Localization;

/// <summary>
/// Naming rules shared, as a linked source file, by Meteion.Toolkit.Localization.KeysGenerator
/// (which emits them) and Meteion.Toolkit.Localization.Check.Core (which has to find the same
/// identities again from XAML) - one copy, so the two can't drift apart. Must stay
/// netstandard2.0-compatible for the generator: no ranges/indices on strings.
/// </summary>
internal static class ResxNaming
{
    /// <summary>Name of the generated constant holding a keys class's resx identity.</summary>
    public const string ResxSourceFieldName = "ResxSource";

    /// <summary>Suffix appended to the resx file name to form the default generated class name.</summary>
    public const string ClassNameSuffix = "Keys";

    /// <summary>
    /// Splits a resx file name (without extension) into its family base name and culture
    /// suffix, e.g. "Resources.ja-JP" → ("Resources", "ja-JP"). The culture is null for a
    /// neutral resx.
    /// </summary>
    public static (string BaseName, string? Culture) SplitCultureSuffix(string fileNameWithoutExtension)
    {
        var lastDot = fileNameWithoutExtension.LastIndexOf('.');
        if (lastDot < 0)
        {
            return (fileNameWithoutExtension, null);
        }

        var candidate = fileNameWithoutExtension.Substring(lastDot + 1);

        try
        {
            var culture = CultureInfo.GetCultureInfo(candidate);
            if (!string.IsNullOrEmpty(culture.Name))
            {
                return (fileNameWithoutExtension.Substring(0, lastDot), culture.Name);
            }
        }
        catch (CultureNotFoundException)
        {
            // Not a culture suffix (e.g. "Resources.Designer") - the whole name is the base.
        }

        return (fileNameWithoutExtension, null);
    }

    /// <summary>
    /// The manifest resource base name MSBuild gives an embedded resx by default (what
    /// <c>ResourceManager</c> takes): root namespace, then the project-relative directory with
    /// each segment made a valid identifier, then the file name as-is - e.g.
    /// <c>Outrun.Client</c> + <c>Properties\</c> + <c>Strings</c> → <c>Outrun.Client.Properties.Strings</c>.
    /// Doesn't model <c>LogicalName</c> or the <c>DependentUpon</c> convention - those need the
    /// <c>MeteionResourceBaseName</c> override.
    /// </summary>
    public static string DefaultBaseName(string? rootNamespace, string? relativeDir, string fileNameWithoutCulture)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(rootNamespace))
        {
            parts.Add(rootNamespace!);
        }

        parts.AddRange(SplitDirectory(relativeDir).Select(MakeValidEverettIdentifier));
        parts.Add(fileNameWithoutCulture);
        return string.Join(".", parts);
    }

    /// <summary>
    /// The generated keys class's default namespace: root namespace plus the project-relative
    /// directory's segments, each sanitized to an identifier. Null when both are empty.
    /// </summary>
    public static string? DefaultNamespace(string? rootNamespace, string? relativeDir)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(rootNamespace))
        {
            parts.Add(rootNamespace!);
        }

        parts.AddRange(SplitDirectory(relativeDir).Select(SanitizeIdentifier));
        return parts.Count == 0 ? null : string.Join(".", parts);
    }

    /// <summary>The generated keys class's default name, e.g. "Strings" → "StringsKeys".</summary>
    public static string DefaultClassName(string fileNameWithoutCulture) => SanitizeIdentifier(fileNameWithoutCulture) + ClassNameSuffix;

    /// <summary><c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;</c> - see <c>LocalizationKey</c>.</summary>
    public static string Source(string assemblyName, string baseName) => assemblyName + "/" + baseName;

    /// <summary><c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;:&lt;Key&gt;</c> - see <c>LocalizationKey</c>.</summary>
    public static string QualifiedKey(string assemblyName, string baseName, string key) => Source(assemblyName, baseName) + ":" + key;

    /// <summary>
    /// Parses <c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;:&lt;Key&gt;</c>. Splits at the first
    /// <c>/</c> and then the first <c>:</c> after it; the key part may contain either. Anything
    /// else is an unqualified key.
    /// </summary>
    public static bool TryParseQualifiedKey(string key, out string assemblyName, out string baseName, out string keyName)
    {
        assemblyName = baseName = keyName = string.Empty;

        var slash = key.IndexOf('/');
        if (slash <= 0 || key.IndexOf(':', 0, slash) >= 0)
        {
            return false;
        }

        var colon = key.IndexOf(':', slash + 1);
        if (colon <= slash + 1 || colon == key.Length - 1 || key.IndexOf('/', slash + 1, colon - slash - 1) >= 0)
        {
            return false;
        }

        assemblyName = key.Substring(0, slash);
        baseName = key.Substring(slash + 1, colon - slash - 1);
        keyName = key.Substring(colon + 1);
        return true;
    }

    /// <summary>Parses <c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;</c>.</summary>
    public static bool TryParseSource(string? source, out string assemblyName, out string baseName)
    {
        assemblyName = baseName = string.Empty;
        if (string.IsNullOrEmpty(source))
        {
            return false;
        }

        var slash = source!.IndexOf('/');
        if (slash <= 0 || slash == source.Length - 1 || source.IndexOf('/', slash + 1) >= 0 || source.IndexOf(':') >= 0)
        {
            return false;
        }

        assemblyName = source.Substring(0, slash);
        baseName = source.Substring(slash + 1);
        return true;
    }

    /// <summary>
    /// The generated constant name for each resx key, in resx order: sanitized, and made unique
    /// with a <c>_2</c>, <c>_3</c>, ... suffix where two keys sanitize alike (e.g. "My.Key" and
    /// "My_Key") or a key collides with <see cref="ResxSourceFieldName"/>.
    /// </summary>
    public static List<string> AssignIdentifiers(IEnumerable<string> keyNames)
    {
        var used = new HashSet<string>(StringComparer.Ordinal) { ResxSourceFieldName };
        var identifiers = new List<string>();

        foreach (var name in keyNames)
        {
            var identifier = SanitizeIdentifier(name);
            if (!used.Add(identifier))
            {
                var suffix = 2;
                string candidate;
                do
                {
                    candidate = identifier + "_" + suffix.ToString(CultureInfo.InvariantCulture);
                    suffix++;
                } while (!used.Add(candidate));

                identifier = candidate;
            }

            identifiers.Add(identifier);
        }

        return identifiers;
    }

    /// <summary>
    /// Converts an arbitrary resx key name (or path segment) into a valid C# identifier -
    /// invalid characters become <c>_</c>, a leading digit gets a <c>_</c> prefix, and a
    /// reserved keyword gets an <c>@</c> prefix.
    /// </summary>
    public static string SanitizeIdentifier(string name)
    {
        var identifier = MakeValidEverettIdentifier(name);
        return CSharpKeywords.Contains(identifier) ? "@" + identifier : identifier;
    }

    /// <summary>
    /// MSBuild's identifier rule for directory segments of a manifest resource name (and the
    /// basis of <see cref="SanitizeIdentifier"/>): every character that isn't a letter, digit
    /// or <c>_</c> becomes <c>_</c>, and a leading digit gets a <c>_</c> prefix.
    /// </summary>
    private static string MakeValidEverettIdentifier(string name)
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

        return builder.ToString();
    }

    private static IEnumerable<string> SplitDirectory(string? relativeDir)
        => string.IsNullOrEmpty(relativeDir)
            ? Enumerable.Empty<string>()
            : relativeDir!.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);

    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
        "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
        "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while",
    };
}
