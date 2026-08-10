using Meteion.Toolkit.Localization.Abstractions;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Meteion.Toolkit.WPF.Localization.Extensions;

/// <summary>
/// Provides a localized value based on the Key, and optionally assembly.
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public class LocalizedValueExtension : MarkupExtension
{
    public string? Key { get; set; }
    public Assembly? Assembly { get; set; }

    public LocalizedValueExtension(string key) : this(key, null) { }

    public LocalizedValueExtension() { }

    public LocalizedValueExtension(string key, Assembly? assembly)
    {
        Key = key;
        Assembly = assembly;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        // TODO: make it so we can toggle functionality
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            return $"[{Key}]";
        }

        if (Key == null)
        {
            return "NOKEY";
        }

        var loc = LocalizationServiceLocator.Resolve<ILocalizationService>();
        var asmResolver = LocalizationServiceLocator.Resolve<IResourceAssemblyResolver>();
        var resolvedAssembly = asmResolver.Resolve(Assembly, serviceProvider);

        var target = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;

        if (target?.TargetObject is DependencyObject depObj &&
            target.TargetProperty is DependencyProperty or PropertyInfo)
        {
            var proxy = new ToolkitLocalizationProxy(loc, Key, resolvedAssembly);
            var binding = new Binding(nameof(ToolkitLocalizationProxy.Value)) { Source = proxy };
            return LocalizedValueTargetBinder.Bind(depObj, target.TargetProperty, binding);
        }

        // Target isn't a DependencyObject at all (rare) — no live update possible.
        return loc.GetString(Key, resolvedAssembly);
    }
}

/// <summary>
/// Workaround for things like Run, which don't take a binding value.
/// </summary>
internal static class LocalizedValueTargetBinder
{
    // Stores which real member (DependencyProperty or PropertyInfo) to push updates into.
    private static readonly DependencyProperty RealTargetProperty =
        DependencyProperty.RegisterAttached("RealTarget", typeof(object), typeof(LocalizedValueTargetBinder));

    // The actual bound property — a real DP, so explicit SetBinding works reliably on it.
    private static readonly DependencyProperty ProxyValueProperty =
        DependencyProperty.RegisterAttached("ProxyValue", typeof(string), typeof(LocalizedValueTargetBinder),
            new PropertyMetadata(null, OnProxyValueChanged));

    public static string Bind(DependencyObject targetObject, object realTargetMember, Binding binding)
    {
        targetObject.SetValue(RealTargetProperty, realTargetMember);
        BindingOperations.SetBinding(targetObject, ProxyValueProperty, binding);
        return (string?)targetObject.GetValue(ProxyValueProperty) ?? string.Empty;
    }

    private static void OnProxyValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var newValue = (string?)e.NewValue ?? string.Empty;
        switch (d.GetValue(RealTargetProperty))
        {
            case DependencyProperty dp: d.SetValue(dp, newValue); break;
            case PropertyInfo pi: pi.SetValue(d, newValue); break;
        }
    }
}