using Meteion.Toolkit.Localization.Abstractions;
using System.ComponentModel;
using System.Reflection;
using System.Windows;

namespace Meteion.Toolkit.WPF.Localization.Extensions;

/// <summary>
/// Like <see cref="ToolkitLocalizationProxy"/>, but the resource key isn't fixed at
/// construction time — it's fed in via <see cref="Key"/> (driven by a bound source
/// property through <see cref="DynamicKeyBinder"/>) and can change independently of
/// culture changes. <see cref="Value"/> is recomputed whenever either changes.
/// </summary>
/// <remarks>
/// This is a <see cref="DependencyObject"/> (rather than a plain class, like
/// <see cref="ToolkitLocalizationProxy"/>) purely so <see cref="DynamicKeyBinder"/> has
/// something to attach the caller's key-source binding to.
/// </remarks>
internal sealed class DynamicToolkitLocalizationProxy : DependencyObject, INotifyPropertyChanged
{
    private readonly ILocalizationService _service;
    private readonly Assembly _assembly;
    private string? _key;

    public DynamicToolkitLocalizationProxy(ILocalizationService service, Assembly assembly)
    {
        _service = service;
        _assembly = assembly;
        Value = Resolve();

        WeakEventManager<ILocalizationService, CultureChangedEventArgs>.AddHandler(
            _service, nameof(ILocalizationService.CultureChanged), OnCultureChanged);
    }

    /// <summary>
    /// The resource key to resolve. Set by <see cref="DynamicKeyBinder"/> as the caller's
    /// KeyBinding source value changes.
    /// </summary>
    public string? Key
    {
        get => _key;
        set
        {
            if (_key == value)
            {
                return;
            }

            _key = value;
            Recompute();
        }
    }

    public string Value { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e) => Recompute();

    private void Recompute()
    {
        Value = Resolve();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }

    private string Resolve() => _key == null ? string.Empty : _service.GetString(_key, _assembly);
}
