using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.Localization.Resolution;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;

namespace Meteion.Toolkit.WPF.Localization;

/// <summary>
/// Default <see cref="ILocalizationService"/>: tracks the active culture, keeps ambient WPF/.NET
/// culture settings in step with it, and looks strings up through an
/// <see cref="ILocalizationProvider"/>, applying the configured missing-key behavior.
/// </summary>
internal sealed class LocalizationService : ILocalizationService
{
    private readonly ILocalizationProvider _provider;
    private readonly LocalizationOptions _options;
    private CultureInfo _currentCulture;

    /// <summary>
    /// Creates the service and applies the starting culture.
    /// </summary>
    /// <param name="provider">The provider that reads localized strings from resources.</param>
    /// <param name="options">Options supplying the default culture, assembly and missing-key behavior.</param>
    public LocalizationService(ILocalizationProvider provider, IOptions<LocalizationOptions> options)
    {
        _provider = provider;
        _options = options.Value;
        _currentCulture = _options.DefaultCulture ?? CultureInfo.CurrentUICulture;

        // Apply the starting culture too, not just later changes — otherwise a
        // configured DefaultCulture that differs from the OS locale would only take
        // effect the first time someone explicitly sets CurrentCulture.
        SyncAmbientCulture(_currentCulture);
    }

    /// <inheritdoc />
    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture.Equals(value)) return;
            _currentCulture = value;
            SyncAmbientCulture(value);
            CultureChanged?.Invoke(this, new CultureChangedEventArgs(value));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
        }
    }

    /// <summary>
    /// Propagates the selected culture to everything outside this service that would
    /// otherwise silently keep following the OS locale instead:
    /// <list type="bullet">
    /// <item><description><see cref="CultureInfo.CurrentCulture"/>/<see cref="CultureInfo.CurrentUICulture"/>
    /// (thread-scoped), so code that formats without an explicit culture/provider —
    /// <c>DateTime.ToString()</c>, <c>decimal.ToString()</c>, etc. — follows the app's
    /// selected language instead of the OS locale.</description></item>
    /// <item><description><see cref="FrameworkElement.LanguageProperty"/> on every open
    /// window, via <see cref="FrameworkLanguageSynchronizer"/> — WPF's own binding
    /// pipeline (StringFormat, implicit ToString conversions) resolves its formatting
    /// culture from there, not from <see cref="CultureInfo.CurrentCulture"/>.</description></item>
    /// </list>
    /// </summary>
    /// <param name="culture">The culture to apply.</param>
    private static void SyncAmbientCulture(CultureInfo culture)
    {
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        FrameworkLanguageSynchronizer.Sync(culture);
    }

    /// <inheritdoc />
    public event EventHandler<CultureChangedEventArgs>? CultureChanged;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public string GetString(string key)
    {
        var parsed = LocalizationKey.Parse(key);
        if (parsed.IsQualified)
        {
            return Lookup(parsed, ResourceAssemblyLocator.Find(parsed.AssemblyName!));
        }

        var assembly = _options.DefaultAssembly
            ?? throw new LocalizationConfigurationException(
                   $"Could not resolve a resource assembly for unqualified key '{key}': no assembly was specified and no LocalizationOptions.DefaultAssembly is configured.");

        return Lookup(parsed, assembly);
    }

    /// <inheritdoc />
    public string GetString(string key, Assembly resourceAssembly)
    {
        ArgumentNullException.ThrowIfNull(resourceAssembly);

        var parsed = LocalizationKey.Parse(key);
        if (parsed.IsQualified && !ResourceAssemblyLocator.NameMatches(resourceAssembly, parsed.AssemblyName!))
        {
            throw new LocalizationConfigurationException(
                $"Key '{key}' names assembly '{parsed.AssemblyName}', but assembly '{resourceAssembly.GetName().Name}' was specified.");
        }

        return Lookup(parsed, resourceAssembly);
    }

    /// <inheritdoc />
    public string GetString(string key, string source)
    {
        var parsed = LocalizationKey.FromSource(source, key);
        return Lookup(parsed, ResourceAssemblyLocator.Find(parsed.AssemblyName!));
    }

    // Looks the key up in the given assembly; a miss is traced and handled per MissingKeyBehavior.
    private string Lookup(LocalizationKey key, Assembly assembly)
    {
        var value = _provider.GetLocalizedString(key, assembly, CurrentCulture);
        if (value is not null) return value;

        var keyText = key.ToString();

        // Surface this the same way a genuinely failed {Binding} would, in Visual Studio's
        // XAML Binding Failures window — regardless of MissingKeyBehavior, since ReturnKey/
        // ReturnEmptyString would otherwise degrade with no signal that anything went wrong.
        LocalizationTraceSource.TraceMissingKey(keyText, assembly, _options.MissingKeyBehavior);

        return _options.MissingKeyBehavior switch
        {
            MissingResourceBehavior.ReturnKey => keyText,
            MissingResourceBehavior.ReturnEmptyString => string.Empty,
            MissingResourceBehavior.ThrowException => throw new LocalizationKeyNotFoundException(keyText, assembly),
            _ => keyText
        };
    }
}
