using Meteion.Toolkit.Localization.Abstractions;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

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
    }

    public CultureInfo CurrentCulture
    {
        get => _currentCulture;
        set
        {
            if (_currentCulture.Equals(value)) return;
            _currentCulture = value;
            CultureChanged?.Invoke(this, new CultureChangedEventArgs(value));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
        }
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
