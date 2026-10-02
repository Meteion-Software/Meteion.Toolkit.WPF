using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Resolution;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Windows;
using System.Windows.Data;

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

    /// <summary>
    /// Creates a request from already-resolved settings. Prefer <see cref="Create"/> for a
    /// markup-extension usage.
    /// </summary>
    /// <param name="service">The service lookups go through.</param>
    /// <param name="contextAssembly">
    /// The assembly an unqualified key without a <paramref name="source"/> resolves against -
    /// null when it couldn't be resolved, in which case <paramref name="contextAssemblyError"/>
    /// is rethrown only if such a key actually needs it.
    /// </param>
    /// <param name="source">The usage's <c>Source</c> resx identity, if any.</param>
    /// <param name="keyPrefix">Optional text prepended to each unqualified key before lookup.</param>
    /// <param name="explicitAssembly">The usage's explicit <c>Assembly</c>, if any - checked against qualified keys.</param>
    /// <param name="missingKeyBehavior">
    /// How <see cref="ResolveForBinding"/> handles a bound value that breaks the rules above -
    /// the app's <see cref="LocalizationOptions.MissingKeyBehavior"/>.
    /// </param>
    /// <param name="contextAssemblyError">The failure from resolving the context assembly, if it failed.</param>
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

    /// <summary>
    /// Builds the request for one markup-extension usage. The context assembly is resolved
    /// eagerly, while the XAML service provider is still valid, but a failure is only surfaced if
    /// an unqualified key without a <paramref name="source"/> actually needs it - a qualified key
    /// names its own assembly.
    /// </summary>
    /// <exception cref="LocalizationConfigurationException">
    /// <paramref name="source"/> and <paramref name="assembly"/> were both set.
    /// </exception>
    /// <param name="source">The usage's <c>Source</c> resx identity, if any.</param>
    /// <param name="assembly">The usage's explicit <c>Assembly</c>, if any.</param>
    /// <param name="keyPrefix">Optional text prepended to each unqualified key before lookup.</param>
    /// <param name="serviceProvider">The XAML service provider used to infer the context assembly.</param>
    /// <returns>The request for this usage.</returns>
    public static LocalizationRequest Create(
        string? source,
        Assembly? assembly,
        string? keyPrefix,
        IServiceProvider serviceProvider)
    {
        if (source is not null && assembly is not null)
        {
            throw new LocalizationConfigurationException(
                $"Source and Assembly can't both be set - Source '{source}' already names its assembly.");
        }

        var loc = LocalizationServiceLocator.Resolve<ILocalizationService>();

        Assembly? contextAssembly = null;
        Exception? contextAssemblyError = null;
        if (source is null)
        {
            try
            {
                contextAssembly = LocalizationServiceLocator.Resolve<IResourceAssemblyResolver>().Resolve(assembly, serviceProvider);
            }
            catch (LocalizationConfigurationException ex)
            {
                contextAssemblyError = ex;
            }
        }

        // Options are registered by AddWpfLocalization; fall back to their defaults when a host
        // (or a test) wires up the services without them.
        var missingKeyBehavior = LocalizationServiceLocator.TryResolve<IOptions<LocalizationOptions>>()?.Value.MissingKeyBehavior
            ?? new LocalizationOptions().MissingKeyBehavior;

        return new LocalizationRequest(loc, contextAssembly, source, keyPrefix, assembly, contextAssemblyError, missingKeyBehavior);
    }

    /// <summary>The service lookups go through.</summary>
    public ILocalizationService Service { get; }

    /// <summary>The resx identity unqualified keys resolve from, if set.</summary>
    public string? Source { get; }

    /// <summary>Text prepended to each unqualified key before lookup, if set.</summary>
    public string? KeyPrefix { get; }

    /// <summary>The usage's explicit assembly, checked against qualified keys, if set.</summary>
    public Assembly? ExplicitAssembly { get; }

    /// <summary>How a bound value that breaks the lookup rules is handled.</summary>
    public MissingResourceBehavior MissingKeyBehavior { get; }

    /// <summary>
    /// Resolves <paramref name="key"/>, throwing <see cref="LocalizationConfigurationException"/>
    /// for a usage that breaks the rules above.
    /// </summary>
    /// <param name="key">The raw key, qualified or unqualified.</param>
    /// <returns>The localized text.</returns>
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

        if (_contextAssembly is null)
        {
            // The resolver ran before any key was known, so name the key that needed it here.
            throw new LocalizationConfigurationException(
                $"Could not resolve a resource assembly for unqualified key '{combined}'. " +
                (_contextAssemblyError?.Message ?? string.Empty),
                _contextAssemblyError);
        }

        return Service.GetString(combined, _contextAssembly);
    }

    /// <summary>
    /// <see cref="Resolve"/> for a value produced at runtime by a binding. A bound value that
    /// breaks the rules above is always traced to the XAML binding-failure output, then handled
    /// like a missing key per <see cref="MissingKeyBehavior"/>: rethrown, or displayed as the raw
    /// value or an empty string. (A value that's merely missing from its resx is already handled
    /// by the service itself.)
    /// </summary>
    /// <param name="key">The key produced by a binding.</param>
    /// <returns>The localized text, or the fallback dictated by <see cref="MissingKeyBehavior"/>.</returns>
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
    /// <see cref="ResolveForBinding"/> for a raw value handed over by a binding. "No key" values -
    /// null, <see cref="DependencyProperty.UnsetValue"/> and <see cref="Binding.DoNothing"/> -
    /// render as an empty string and are never looked up, so <see cref="MissingKeyBehavior"/>
    /// doesn't apply to them. Only UnsetValue (a binding that failed to resolve) is traced; null
    /// is a legitimate "nothing yet". Empty strings are deliberately still ordinary lookups.
    /// </summary>
    /// <param name="raw">The value a key binding produced.</param>
    /// <returns>The localized text, or an empty string when there is no key.</returns>
    public string ResolveBoundKey(object? raw)
    {
        if (ReferenceEquals(raw, DependencyProperty.UnsetValue))
        {
            LocalizationTraceSource.TraceUnsetKey();
            return string.Empty;
        }

        if (raw is null || ReferenceEquals(raw, Binding.DoNothing))
        {
            return string.Empty;
        }

        // ToString() matches WPF's implicit conversion on the DependencyProperty path (e.g. enums).
        return raw.ToString() is { } key ? ResolveForBinding(key) : string.Empty;
    }

    /// <summary>
    /// Text for the design-time <c>[Key]</c> placeholder - the bare resx key name, not the full
    /// qualified form, so the designer surface stays readable.
    /// </summary>
    /// <param name="keyPrefix">The usage's key prefix, applied to unqualified keys only.</param>
    /// <param name="key">The raw key, qualified or unqualified.</param>
    /// <returns>The text to show inside the placeholder brackets.</returns>
    public static string DesignTimeText(string? keyPrefix, string key)
    {
        var parsed = LocalizationKey.Parse(key);
        return parsed.IsQualified ? parsed.Key : keyPrefix + key;
    }
}
