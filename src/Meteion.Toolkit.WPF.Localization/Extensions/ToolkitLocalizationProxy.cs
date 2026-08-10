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
    private readonly ILocalizationService _service;
    private readonly string _key;
    private readonly Assembly _assembly;

    public ToolkitLocalizationProxy(ILocalizationService localizationService, string key, Assembly assembly)
    {
        _service = localizationService;
        _key = key;
        _assembly = assembly;

        Value = _service.GetString(_key, _assembly);

        WeakEventManager<ILocalizationService, CultureChangedEventArgs>.AddHandler(
            _service, nameof(ILocalizationService.CultureChanged), OnCultureChanged);
    }

    public string Value { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnCultureChanged(object? sender, CultureChangedEventArgs culture)
    {
        Value = _service.GetString(_key, _assembly);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }
}
