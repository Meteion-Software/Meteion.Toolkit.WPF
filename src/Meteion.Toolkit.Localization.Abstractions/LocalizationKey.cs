namespace Meteion.Toolkit.Localization.Abstractions;

/// <summary>
/// A parsed localization key. A qualified key names its own assembly and resx -
/// <c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;:&lt;Key&gt;</c>, e.g.
/// <c>Outrun.Client/Outrun.Client.Properties.Strings:HomePage_Welcome</c> - which is the form
/// <c>Meteion.Toolkit.Localization.KeysGenerator</c> emits. An unqualified key is a bare resx
/// key name, resolved against a caller-supplied assembly's only resx.
/// </summary>
/// <remarks>
/// <c>/</c> can't appear in an assembly name and neither <c>/</c> nor <c>:</c> can appear in a
/// resource base name (a dotted namespace + file name), so splitting at the first <c>/</c> and
/// then the first <c>:</c> after it is unambiguous - the key part itself may contain either.
/// </remarks>
public readonly record struct LocalizationKey
{
    public const char AssemblySeparator = '/';
    public const char KeySeparator = ':';

    private LocalizationKey(string? assemblyName, string? baseName, string key)
    {
        AssemblyName = assemblyName;
        BaseName = baseName;
        Key = key;
    }

    /// <summary>The assembly's simple name, or null for an unqualified key.</summary>
    public string? AssemblyName { get; }

    /// <summary>The resx's manifest resource base name (what <c>ResourceManager</c> takes), or null for an unqualified key.</summary>
    public string? BaseName { get; }

    /// <summary>The raw resx key name.</summary>
    public string Key { get; }

    public bool IsQualified => AssemblyName is not null;

    /// <summary>
    /// The <c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;</c> part of a qualified key - the same
    /// format as a generated <c>ResxSource</c> constant - or null for an unqualified key.
    /// </summary>
    public string? Source => IsQualified ? AssemblyName + AssemblySeparator + BaseName : null;

    public static LocalizationKey Qualified(string assemblyName, string baseName, string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(assemblyName);
        ArgumentException.ThrowIfNullOrEmpty(baseName);
        ArgumentNullException.ThrowIfNull(key);
        return new LocalizationKey(assemblyName, baseName, key);
    }

    public static LocalizationKey Unqualified(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return new LocalizationKey(null, null, key);
    }

    /// <summary>
    /// Parses <paramref name="key"/> as a qualified key when it has the
    /// <c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;:&lt;Key&gt;</c> shape, otherwise as an
    /// unqualified key. Never throws for a non-null input.
    /// </summary>
    public static LocalizationKey Parse(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return ResxNaming.TryParseQualifiedKey(key, out var assemblyName, out var baseName, out var keyName)
            ? new LocalizationKey(assemblyName, baseName, keyName)
            : Unqualified(key);
    }

    /// <summary>
    /// Parses a <c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;</c> resx identity (a generated
    /// <c>ResxSource</c> constant, or <c>LocalizedValueExtension.Source</c>).
    /// </summary>
    public static bool TryParseSource(string? source, out string assemblyName, out string baseName)
        => ResxNaming.TryParseSource(source, out assemblyName, out baseName);

    /// <summary>
    /// Qualifies an unqualified <paramref name="key"/> with a <c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;</c>
    /// <paramref name="source"/>.
    /// </summary>
    /// <exception cref="LocalizationConfigurationException"><paramref name="source"/> isn't a valid source, or <paramref name="key"/> is already qualified.</exception>
    public static LocalizationKey FromSource(string source, string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        if (!TryParseSource(source, out var assemblyName, out var baseName))
        {
            throw new LocalizationConfigurationException(
                $"'{source}' is not a valid localization source - expected '<AssemblyName>{AssemblySeparator}<ResourceBaseName>', " +
                "e.g. a generated keys class's ResxSource constant.");
        }

        if (Parse(key).IsQualified)
        {
            throw new LocalizationConfigurationException(
                $"Key '{key}' is already qualified, so it can't be combined with source '{source}'.");
        }

        return new LocalizationKey(assemblyName, baseName, key);
    }

    public override string ToString() => IsQualified ? Source + KeySeparator + Key : Key;
}
