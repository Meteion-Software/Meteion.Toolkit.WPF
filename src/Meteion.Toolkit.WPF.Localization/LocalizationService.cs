using Meteion.Toolkit.Localization.Abstractions;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;

namespace Meteion.Toolkit.WPF.Localization;

internal sealed class LocalizationService : ILocalizationService
{
    private readonly ILocalizationProvider _provider;
    private readonly LocalizationOptions _options;
    private CultureInfo _currentCulture;

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
    private static void SyncAmbientCulture(CultureInfo culture)
    {
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        FrameworkLanguageSynchronizer.Sync(culture);
    }

    public event EventHandler<CultureChangedEventArgs>? CultureChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    public string GetString(string key, Assembly? resourceAssembly = null)
    {
        var assembly = resourceAssembly ?? _options.DefaultAssembly
            ?? throw new LocalizationConfigurationException(
                   $"Could not resolve a resource assembly for key '{key}': no assembly was specified and no LocalizationOptions.DefaultAssembly is configured.");

        var value = _provider.GetLocalizedString(key, assembly, CurrentCulture);
        if (value is not null) return value;

        // Surface this the same way a genuinely failed {Binding} would, in Visual Studio's
        // XAML Binding Failures window — regardless of MissingKeyBehavior, since ReturnKey/
        // ReturnEmptyString would otherwise degrade with no signal that anything went wrong.
        LocalizationTraceSource.TraceMissingKey(key, assembly, _options.MissingKeyBehavior);

        return _options.MissingKeyBehavior switch
        {
            MissingResourceBehavior.ReturnKey => key,
            MissingResourceBehavior.ReturnEmptyString => string.Empty,
            MissingResourceBehavior.ThrowException => throw new LocalizationKeyNotFoundException(key, assembly),
            _ => key
        };
    }
}
