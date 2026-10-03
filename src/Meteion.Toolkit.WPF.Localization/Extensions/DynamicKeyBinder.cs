using System.Windows;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Localization.Extensions;

/// <summary>
/// Attaches a caller-supplied key-source binding (e.g. <c>{Binding TitleKey}</c>) onto
/// the real target element, so it resolves against that element's inherited DataContext,
/// and forwards each resolved key into a <see cref="DynamicToolkitLocalizationProxy"/>.
/// </summary>
/// <remarks>
/// Mirrors the attached-property forwarding trick <see cref="LocalizedValueTargetBinder"/>
/// already uses to push the final localized value into the real target member — here the
/// same shape is used in reverse, to pull a dynamic key in from the target's DataContext.
/// <para>
/// Only used for plain CLR property targets (e.g. <c>Run.Text</c>). The helper properties are
/// attached once per element, so a second use on the same element would overwrite the first;
/// <see cref="LocalizedValueExtension"/> binds <see cref="DependencyProperty"/> targets with a
/// <see cref="MultiBinding"/> instead, which has no such limit.
/// </para>
/// </remarks>
internal static class DynamicKeyBinder
{
    private static readonly DependencyProperty ProxyReferenceProperty =
        DependencyProperty.RegisterAttached("ProxyReference", typeof(DynamicToolkitLocalizationProxy), typeof(DynamicKeyBinder));

    private static readonly DependencyProperty KeySourceProperty =
        DependencyProperty.RegisterAttached("KeySource", typeof(string), typeof(DynamicKeyBinder),
            new PropertyMetadata(null, OnKeySourceChanged));

    /// <summary>
    /// Binds <paramref name="keyBinding"/> onto <paramref name="targetObject"/> and routes each
    /// resolved key to <paramref name="proxy"/>.
    /// </summary>
    /// <param name="targetObject">The real element whose DataContext the key binding resolves against.</param>
    /// <param name="proxy">The proxy that receives each key through <c>Key</c>.</param>
    /// <param name="keyBinding">The caller-supplied binding that produces the resource key.</param>
    public static void Bind(DependencyObject targetObject, DynamicToolkitLocalizationProxy proxy, BindingBase keyBinding)
    {
        targetObject.SetValue(ProxyReferenceProperty, proxy);
        BindingOperations.SetBinding(targetObject, KeySourceProperty, keyBinding);
    }

    private static void OnKeySourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d.GetValue(ProxyReferenceProperty) is DynamicToolkitLocalizationProxy proxy)
        {
            proxy.Key = e.NewValue as string;
        }
    }
}
