using Meteion.Toolkit.Localization.Abstractions;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows;

namespace Meteion.Toolkit.WPF.Localization.Extensions;

/// <summary>
/// Holds a localized value
/// </summary>
internal sealed class ToolkitLocalizationProxy : INotifyPropertyChanged
{
    private readonly LocalizationRequest _request;
    private readonly string _key;

    public ToolkitLocalizationProxy(ILocalizationService localizationService, string key, Assembly assembly)
        : this(new LocalizationRequest(localizationService, assembly), key)
    {
    }

    public ToolkitLocalizationProxy(LocalizationRequest request, string key)
    {
        _request = request;
        _key = key;

        Value = _request.Resolve(_key);

        WeakEventManager<ILocalizationService, CultureChangedEventArgs>.AddHandler(
            _request.Service, nameof(ILocalizationService.CultureChanged), OnCultureChanged);
    }

    public string Value { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnCultureChanged(object? sender, CultureChangedEventArgs culture)
    {
        Value = _request.Resolve(_key);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }
}
