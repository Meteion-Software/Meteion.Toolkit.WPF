using Meteion.Toolkit.Localization.Abstractions;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Localization.Extensions;

/// <summary>
/// A trivial <see cref="INotifyPropertyChanged"/> source whose only job is to raise a change
/// notification whenever the active culture changes. Used as the second input of the
/// <see cref="MultiBinding"/> that resolves a <see cref="LocalizedValueExtension.KeyBinding"/>
/// against a <see cref="DependencyProperty"/> target: the key-source binding alone would never
/// re-fire on a culture change (the key itself hasn't changed), so this gives the MultiBinding
/// a second reason to re-run its converter.
/// </summary>
internal sealed class CultureChangeTrigger : INotifyPropertyChanged
{
    private readonly ILocalizationService _service;

    public CultureChangeTrigger(ILocalizationService service)
    {
        _service = service;

        WeakEventManager<ILocalizationService, CultureChangedEventArgs>.AddHandler(
            _service, nameof(ILocalizationService.CultureChanged), OnCultureChanged);
    }

    /// <summary>
    /// The value itself is meaningless — only the accompanying <see cref="PropertyChanged"/>
    /// notification matters to whatever is bound to it.
    /// </summary>
    public object? Value => null;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnCultureChanged(object? sender, CultureChangedEventArgs e) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
}
