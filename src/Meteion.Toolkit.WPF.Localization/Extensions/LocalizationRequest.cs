using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Resolution;
using System.Reflection;

namespace Meteion.Toolkit.WPF.Localization.Extensions;

/// <summary>
/// One <see cref="LocalizedValueExtension"/> usage's lookup settings - <c>Source</c>,
/// <c>Assembly</c>, <c>KeyPrefix</c> - applied to each raw key it resolves (the literal
/// <c>Key</c>, or every value a <c>KeyBinding</c> produces), so the static and dynamic paths
/// share one set of rules:
/// <list type="number">
/// <item><description>A qualified key combined with a <c>KeyPrefix</c> is an error.</description></item>
/// <item><description>A qualified key must agree with <c>Source</c> / an explicit <c>Assembly</c> when either is set.</description></item>
/// <item><description>An unqualified key + <c>Source</c> becomes <c>Source:KeyPrefix+key</c>.</description></item>
/// <item><description>An unqualified key with no <c>Source</c> resolves against the context assembly's only resx.</description></item>
/// </list>
/// (<c>Source</c> + an explicit <c>Assembly</c> together is rejected before this is built - see
/// <see cref="LocalizedValueExtension.ProvideValue"/>.)
/// </summary>
internal sealed class LocalizationRequest
{
    private readonly Assembly? _contextAssembly;
    private readonly Exception? _contextAssemblyError;

    /// <param name="service">The service lookups go through.</param>
    /// <param name="contextAssembly">
    /// The assembly an unqualified key without a <paramref name="source"/> resolves against -
    /// null when it couldn't be resolved, in which case <paramref name="contextAssemblyError"/>
    /// is rethrown only if such a key actually needs it.
    /// </param>
    /// <param name="explicitAssembly">The usage's explicit <c>Assembly</c>, if any - checked against qualified keys.</param>
    /// <param name="missingKeyBehavior">
    /// How <see cref="ResolveForBinding"/> handles a bound value that breaks the rules above -
    /// the app's <see cref="LocalizationOptions.MissingKeyBehavior"/>.
    /// </param>
    public LocalizationRequest(
        ILocalizationService service,
        Assembly? contextAssembly,
        string? source = null,
        string? keyPrefix = null,
        Assembly? explicitAssembly = null,
        Exception? contextAssemblyError = null,
        MissingResourceBehavior missingKeyBehavior = MissingResourceBehavior.ThrowException)
    {
        MissingKeyBehavior = missingKeyBehavior;
        Service = service;
        _contextAssembly = contextAssembly;
        _contextAssemblyError = contextAssemblyError;
        Source = source;
        KeyPrefix = keyPrefix;
        ExplicitAssembly = explicitAssembly;
    }

    public ILocalizationService Service { get; }
    public string? Source { get; }
    public string? KeyPrefix { get; }
    public Assembly? ExplicitAssembly { get; }
    public MissingResourceBehavior MissingKeyBehavior { get; }

    /// <summary>
    /// Resolves <paramref name="key"/>, throwing <see cref="LocalizationConfigurationException"/>
    /// for a usage that breaks the rules above.
    /// </summary>
    public string Resolve(string key)
    {
        var parsed = LocalizationKey.Parse(key);

        if (parsed.IsQualified)
        {
            if (!string.IsNullOrEmpty(KeyPrefix))
            {
                throw new LocalizationConfigurationException(
                    $"Qualified key '{key}' can't be combined with KeyPrefix '{KeyPrefix}'. Use Source with an unqualified key instead.");
            }

            if (Source is not null && !string.Equals(parsed.Source, Source, StringComparison.Ordinal))
            {
                throw new LocalizationConfigurationException(
                    $"Qualified key '{key}' names resx '{parsed.Source}', but Source is '{Source}'.");
            }

            if (ExplicitAssembly is not null && !ResourceAssemblyLocator.NameMatches(ExplicitAssembly, parsed.AssemblyName!))
            {
                throw new LocalizationConfigurationException(
                    $"Qualified key '{key}' names assembly '{parsed.AssemblyName}', but Assembly is '{ExplicitAssembly.GetName().Name}'.");
            }

            return Service.GetString(key);
        }

        var combined = KeyPrefix + key;

        if (Source is not null)
        {
            return Service.GetString(combined, Source);
        }

        var assembly = _contextAssembly
            ?? throw (_contextAssemblyError ?? new LocalizationConfigurationException(
                $"Could not resolve a resource assembly for unqualified key '{combined}'."));

        return Service.GetString(combined, assembly);
    }

    /// <summary>
    /// <see cref="Resolve"/> for a value produced at runtime by a binding. A bound value that
    /// breaks the rules above is always traced to the XAML binding-failure output, then handled
    /// like a missing key per <see cref="MissingKeyBehavior"/>: rethrown, or displayed as the raw
    /// value or an empty string. (A value that's merely missing from its resx is already handled
    /// by the service itself.)
    /// </summary>
    public string ResolveForBinding(string key)
    {
        try
        {
            return Resolve(key);
        }
        catch (LocalizationConfigurationException ex)
        {
            LocalizationTraceSource.TraceConfigurationError(ex.Message);

            switch (MissingKeyBehavior)
            {
                case MissingResourceBehavior.ReturnKey:
                    return key;
                case MissingResourceBehavior.ReturnEmptyString:
                    return string.Empty;
                default:
                    throw;
            }
        }
    }

    /// <summary>
    /// Text for the design-time <c>[Key]</c> placeholder - the bare resx key name, not the full
    /// qualified form, so the designer surface stays readable.
    /// </summary>
    public static string DesignTimeText(string? keyPrefix, string key)
    {
        var parsed = LocalizationKey.Parse(key);
        return parsed.IsQualified ? parsed.Key : keyPrefix + key;
    }
}
