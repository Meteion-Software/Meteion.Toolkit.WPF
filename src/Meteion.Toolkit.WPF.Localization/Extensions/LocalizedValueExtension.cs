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
/// <remarks>
/// The declared return type is <see cref="object"/>, not <see cref="string"/>: ProvideValue can
/// return a <see cref="BindingBase"/> for a DependencyProperty target (see <see cref="ProvideValue"/>),
/// and declaring <c>typeof(string)</c> here causes the XAML compiler to statically assume every
/// call site's value is a string and skip the runtime "is this a BindingBase, call SetBinding
/// instead of SetValue" check — that mismatch throws
/// <c>ArgumentException: 'System.Windows.Data.Binding' is not a valid value for property '...'</c>
/// the moment ProvideValue actually returns one.
/// </remarks>
[MarkupExtensionReturnType(typeof(object))]
public class LocalizedValueExtension : MarkupExtension
{
    /// <summary>
    /// The resource key to resolve. The <see cref="TypeConverterAttribute"/> below drives the
    /// "Key=..." attribute-value dropdown in Visual Studio's XAML editor once
    /// <c>Meteion.Toolkit.Localization.KeysGenerator</c> has generated a keys class for at
    /// least one loaded assembly - see <see cref="LocalizationKeyConverter"/>.
    /// </summary>
    [TypeConverter(typeof(LocalizationKeyConverter))]
    public string? Key { get; set; }
    public Assembly? Assembly { get; set; }

    /// <summary>
    /// Optional binding that supplies the resource key dynamically (e.g. a per-item
    /// key from a bound view-model/model property), instead of the fixed <see cref="Key"/>
    /// literal. When set, the resolved text tracks both the bound key changing and
    /// culture changes. Takes precedence over <see cref="Key"/> when both are set.
    /// </summary>
    public BindingBase? KeyBinding { get; set; }

    /// <summary>
    /// Optional literal string prepended to the resolved key before lookup — for
    /// <see cref="Key"/> as well as each value <see cref="KeyBinding"/> produces. Lets
    /// a bound source supply just a short per-item suffix (e.g. "Info", "Warning")
    /// while the shared resx key namespace (e.g. "Notification_") lives once in XAML.
    /// </summary>
    public string? KeyPrefix { get; set; }

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
            return KeyBinding != null ? $"[{KeyPrefix}…]" : $"[{CombineKey(Key)}]";
        }

        if (Key == null && KeyBinding == null)
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
            // A real, connected element (i.e. NOT inside a DataTemplate/ControlTemplate,
            // where WPF instead supplies a shared placeholder that isn't a DependencyObject —
            // see the fallback below). Bind explicitly against it and return the initial
            // resolved value, exactly as this already worked outside templates before.
            if (KeyBinding != null)
            {
                var dynamicProxy = new DynamicToolkitLocalizationProxy(loc, resolvedAssembly, KeyPrefix);
                DynamicKeyBinder.Bind(depObj, dynamicProxy, KeyBinding);
                var dynamicBinding = new Binding(nameof(DynamicToolkitLocalizationProxy.Value)) { Source = dynamicProxy };
                return LocalizedValueTargetBinder.Bind(depObj, target.TargetProperty, dynamicBinding);
            }

            var proxy = new ToolkitLocalizationProxy(loc, CombineKey(Key)!, resolvedAssembly);
            var binding = new Binding(nameof(ToolkitLocalizationProxy.Value)) { Source = proxy };
            return LocalizedValueTargetBinder.Bind(depObj, target.TargetProperty, binding);
        }

        // From here on, TargetObject isn't a real, connected DependencyObject.
        if (target?.TargetProperty is DependencyProperty)
        {
            // Inside a DataTemplate/ControlTemplate, TargetObject is WPF's shared template
            // placeholder (System.Windows.SharedDp) rather than the real per-row element, so
            // there's nothing to call BindingOperations.SetBinding on directly — that's exactly
            // what silently broke here before. Returning a BindingBase instead works because
            // WPF's deferred template-content loader recognizes it and wires it up itself, once
            // per realized row, against that row's own real element and DataContext.
            if (KeyBinding != null)
            {
                var multiBinding = new MultiBinding
                {
                    Converter = new DynamicKeyLocalizationConverter(loc, resolvedAssembly, KeyPrefix),
                    Mode = BindingMode.OneWay,
                };
                multiBinding.Bindings.Add(KeyBinding);
                multiBinding.Bindings.Add(new Binding(nameof(CultureChangeTrigger.Value))
                {
                    Source = new CultureChangeTrigger(loc),
                    Mode = BindingMode.OneWay,
                });
                return multiBinding;
            }

            var templateProxy = new ToolkitLocalizationProxy(loc, CombineKey(Key)!, resolvedAssembly);
            return new Binding(nameof(ToolkitLocalizationProxy.Value)) { Source = templateProxy, Mode = BindingMode.OneWay };
        }

        // Reached when the target property is a plain CLR property with no real, connected
        // DependencyObject to work with. Inside a DataTemplate/ControlTemplate that's because
        // TargetObject is WPF's shared template placeholder rather than the real per-row
        // element — there's no DependencyObject there to hang a live binding off, and (unlike
        // the DependencyProperty case above) no deferred-loader support to fall back on either.
        // Outside a template it means IProvideValueTarget wasn't available at all.
        if (KeyBinding != null)
        {
            // Never fall back to a silent empty string here — a KeyBinding truly cannot be
            // resolved without a live element to bind the key source against, so say so.
            throw new LocalizationConfigurationException(
                $"{nameof(LocalizedValueExtension)}.{nameof(KeyBinding)} can't be resolved here: the " +
                "target property is a plain CLR property (not a DependencyProperty) with no live, " +
                "connected element to bind the key source against — most likely because this is used " +
                "inside a DataTemplate or ControlTemplate. Target a DependencyProperty instead (e.g. " +
                "TextBlock.Text rather than Run.Text), or use a literal Key.");
        }

        // Literal Key with no live target: resolved once, non-live. Inside a template this
        // means the text won't update on a later culture change — a known limitation for this
        // specific combination (plain CLR property target + template).
        return loc.GetString(CombineKey(Key)!, resolvedAssembly);
    }

    /// <summary>
    /// Prepends <see cref="KeyPrefix"/> (if set) to a resolved key. Used for the literal
    /// <see cref="Key"/> path; the <see cref="KeyBinding"/> path applies the same prefix
    /// per-value instead, via <see cref="DynamicToolkitLocalizationProxy"/> or
    /// <see cref="DynamicKeyLocalizationConverter"/>, since the key isn't known until runtime.
    /// </summary>
    private string? CombineKey(string? key) => key == null ? null : KeyPrefix + key;
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