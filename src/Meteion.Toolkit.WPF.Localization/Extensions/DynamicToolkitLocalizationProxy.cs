using Meteion.Toolkit.Localization.Abstractions;
using System.ComponentModel;
using System.Reflection;
using System.Windows;

namespace Meteion.Toolkit.WPF.Localization.Extensions;

/// <summary>
/// Like <see cref="ToolkitLocalizationProxy"/>, but the resource key isn't fixed at
/// construction time — it's fed in via <see cref="Key"/> (driven by a bound source
/// property through <see cref="DynamicKeyBinder"/>) and can change independently of
/// culture changes. <see cref="Value"/> is recomputed whenever either changes. Only used
/// for plain CLR property targets such as <c>Run.Text</c>.
/// </summary>
/// <remarks>
/// This is a <see cref="DependencyObject"/> (rather than a plain class, like
/// <see cref="ToolkitLocalizationProxy"/>) purely so <see cref="DynamicKeyBinder"/> has
/// something to attach the caller's key-source binding to.
/// </remarks>
internal sealed class DynamicToolkitLocalizationProxy : DependencyObject, INotifyPropertyChanged
{
    private readonly LocalizationRequest _request;
    private string? _key;

    /// <summary>
    /// Creates a proxy that resolves keys against a single assembly.
    /// </summary>
    /// <param name="service">The service lookups go through.</param>
    /// <param name="assembly">The assembly unqualified keys resolve against.</param>
    /// <param name="keyPrefix">Optional text prepended to each key before lookup.</param>
    public DynamicToolkitLocalizationProxy(ILocalizationService service, Assembly assembly, string? keyPrefix = null)
        : this(new LocalizationRequest(service, assembly, keyPrefix: keyPrefix))
    {
    }

    /// <summary>
    /// Creates a proxy and subscribes (weakly) to culture changes.
    /// </summary>
    /// <param name="request">The lookup settings used to resolve each key.</param>
    public DynamicToolkitLocalizationProxy(LocalizationRequest request)
    {
        _request = request;
        Value = Resolve();

        WeakEventManager<ILocalizationService, CultureChangedEventArgs>.AddHandler(
            _request.Service, nameof(ILocalizationService.CultureChanged), OnCultureChanged);
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

    /// <summary>
    /// The localized text for <see cref="Key"/>, or an empty string while no key is set.
    /// </summary>
    public string Value { get; private set; }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e) => Recompute();

    private void Recompute()
    {
        Value = Resolve();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
    }

    private string Resolve() => _key == null ? string.Empty : _request.ResolveForBinding(_key);
}
