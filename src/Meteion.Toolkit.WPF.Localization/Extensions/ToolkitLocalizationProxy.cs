using Meteion.Toolkit.Localization.Abstractions;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;

namespace Meteion.Toolkit.WPF.Localization.Extensions;

/// <summary>
/// Holds the localized text for one fixed resource key and re-resolves it whenever the active
/// culture changes, so a binding to <see cref="Value"/> stays current.
/// </summary>
internal sealed class ToolkitLocalizationProxy : INotifyPropertyChanged
{
    private readonly LocalizationRequest _request;
    private readonly string _key;

    /// <summary>
    /// Creates a proxy that resolves the key against a single assembly.
    /// </summary>
    /// <param name="localizationService">The service lookups go through.</param>
    /// <param name="key">The resource key to resolve.</param>
    /// <param name="assembly">The assembly an unqualified key resolves against.</param>
    public ToolkitLocalizationProxy(ILocalizationService localizationService, string key, Assembly assembly)
        : this(new LocalizationRequest(localizationService, assembly), key)
    {
    }

    /// <summary>
    /// Creates a proxy, resolves the key once, and subscribes (weakly) to culture changes.
    /// </summary>
    /// <param name="request">The lookup settings used to resolve the key.</param>
    /// <param name="key">The resource key to resolve.</param>
    public ToolkitLocalizationProxy(LocalizationRequest request, string key)
    {
        _request = request;
        _key = key;

        Value = _request.Resolve(_key);

        WeakEventManager<ILocalizationService, CultureChangedEventArgs>.AddHandler(
            _request.Service, nameof(ILocalizationService.CultureChanged), OnCultureChanged);
    }

    /// <summary>The localized text for the key in the current culture.</summary>
    public string Value { get; set; }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnCultureChanged(object? sender, CultureChangedEventArgs culture)
    {
        Value = _request.Resolve(_key);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }
}
