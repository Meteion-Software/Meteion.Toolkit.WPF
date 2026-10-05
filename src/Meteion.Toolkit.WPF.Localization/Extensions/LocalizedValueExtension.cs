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

    /// <summary>
    /// Optional assembly whose resx unqualified keys resolve against. When not set, it is inferred
    /// from the XAML context. Can't be combined with <see cref="Source"/>.
    /// </summary>
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

    /// <summary>
    /// Optional resx identity (<c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;</c>) that
    /// unqualified keys - a literal <see cref="Key"/>, or each value <see cref="KeyBinding"/>
    /// produces, with <see cref="KeyPrefix"/> applied - are resolved from. Normally a generated
    /// keys class's constant: <c>Source={x:Static strings:StringsKeys.ResxSource}</c>. Can't be
    /// combined with <see cref="Assembly"/>, since it already names the assembly.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>
    /// Optional format arguments: the resolved text is treated as a composite format string
    /// (e.g. <c>"Hello {0}, you have {1:N0} items"</c>) and formatted with each child binding of
    /// this <see cref="MultiBinding"/>, in order, using the localization service's current culture.
    /// The text re-formats when an argument or the culture changes. Set it with property-element
    /// syntax; for a few arguments, <see cref="Arg0"/>..<see cref="Arg9"/> are an inline
    /// alternative. Can't be combined with them.
    /// </summary>
    public MultiBinding? Args { get; set; }

    // Backing store for the Arg0..Arg9 shorthand; a null entry means "not set".
    private readonly BindingBase?[] _args = new BindingBase?[FormatArguments.ShorthandCount];

    /// <summary>Format argument {0}: a binding, or a literal text constant. Inline shorthand for <see cref="Args"/>.</summary>
    [TypeConverter(typeof(ConstantArgumentConverter))]
    public BindingBase? Arg0 { get => _args[0]; set => _args[0] = value; }

    /// <summary>Format argument {1}: a binding, or a literal text constant. Inline shorthand for <see cref="Args"/>.</summary>
    [TypeConverter(typeof(ConstantArgumentConverter))]
    public BindingBase? Arg1 { get => _args[1]; set => _args[1] = value; }

    /// <summary>Format argument {2}: a binding, or a literal text constant. Inline shorthand for <see cref="Args"/>.</summary>
    [TypeConverter(typeof(ConstantArgumentConverter))]
    public BindingBase? Arg2 { get => _args[2]; set => _args[2] = value; }

    /// <summary>Format argument {3}: a binding, or a literal text constant. Inline shorthand for <see cref="Args"/>.</summary>
    [TypeConverter(typeof(ConstantArgumentConverter))]
    public BindingBase? Arg3 { get => _args[3]; set => _args[3] = value; }

    /// <summary>Format argument {4}: a binding, or a literal text constant. Inline shorthand for <see cref="Args"/>.</summary>
    [TypeConverter(typeof(ConstantArgumentConverter))]
    public BindingBase? Arg4 { get => _args[4]; set => _args[4] = value; }

    /// <summary>Format argument {5}: a binding, or a literal text constant. Inline shorthand for <see cref="Args"/>.</summary>
    [TypeConverter(typeof(ConstantArgumentConverter))]
    public BindingBase? Arg5 { get => _args[5]; set => _args[5] = value; }

    /// <summary>Format argument {6}: a binding, or a literal text constant. Inline shorthand for <see cref="Args"/>.</summary>
    [TypeConverter(typeof(ConstantArgumentConverter))]
    public BindingBase? Arg6 { get => _args[6]; set => _args[6] = value; }

    /// <summary>Format argument {7}: a binding, or a literal text constant. Inline shorthand for <see cref="Args"/>.</summary>
    [TypeConverter(typeof(ConstantArgumentConverter))]
    public BindingBase? Arg7 { get => _args[7]; set => _args[7] = value; }

    /// <summary>Format argument {8}: a binding, or a literal text constant. Inline shorthand for <see cref="Args"/>.</summary>
    [TypeConverter(typeof(ConstantArgumentConverter))]
    public BindingBase? Arg8 { get => _args[8]; set => _args[8] = value; }

    /// <summary>Format argument {9}: a binding, or a literal text constant. Inline shorthand for <see cref="Args"/>.</summary>
    [TypeConverter(typeof(ConstantArgumentConverter))]
    public BindingBase? Arg9 { get => _args[9]; set => _args[9] = value; }

    /// <summary>Creates the extension for a fixed resource key.</summary>
    /// <param name="key">The resource key to resolve.</param>
    public LocalizedValueExtension(string key) : this(key, null) { }

    /// <summary>Creates the extension with its properties to be set by the caller.</summary>
    public LocalizedValueExtension() { }

    /// <summary>Creates the extension for a fixed resource key in a specific assembly.</summary>
    /// <param name="key">The resource key to resolve.</param>
    /// <param name="assembly">The assembly unqualified keys resolve against, or <see langword="null"/> to infer it.</param>
    public LocalizedValueExtension(string key, Assembly? assembly)
    {
        Key = key;
        Assembly = assembly;
    }

    /// <summary>
    /// Returns the localized text, or a live binding that keeps it current, depending on what the
    /// target allows (real element, template placeholder, or plain CLR property). Any number of
    /// these can be used on one element, as long as each targets a different DependencyProperty.
    /// </summary>
    /// <param name="serviceProvider">The XAML service provider for the current usage.</param>
    /// <returns>The localized text, or a <see cref="BindingBase"/> for a template target.</returns>
    /// <exception cref="LocalizationConfigurationException">
    /// The settings are inconsistent, or a <see cref="KeyBinding"/> targets a plain CLR property.
    /// </exception>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        // TODO: make it so we can toggle functionality
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            return KeyBinding != null || Key == null ? $"[{KeyPrefix}…]" : $"[{LocalizationRequest.DesignTimeText(KeyPrefix, Key)}]";
        }

        if (Key == null && KeyBinding == null)
        {
            return "NOKEY";
        }

        var request = CreateRequest(serviceProvider);
        var formatArgs = FormatArguments.Collect(Args, _args, nameof(LocalizedValueExtension));

        var target = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;

        if (target?.TargetProperty is DependencyProperty)
        {
            // The target is a DependencyProperty, so the binding is handed back to WPF to attach
            // rather than being attached here. Every extension on an element then gets its own
            // binding on its own property; an earlier version attached helper properties to the
            // element itself, which a second extension on the same element silently overwrote
            // (e.g. a ContentDialog's Title, PrimaryButtonText and SecondaryButtonText).
            BindingBase binding;
            if (formatArgs.Count > 0)
            {
                binding = FormatArguments.CreateMultiBinding(request, Key, KeyBinding, formatArgs);
            }
            else if (KeyBinding != null)
            {
                var multiBinding = new MultiBinding
                {
                    Converter = new DynamicKeyLocalizationConverter(request),
                    Mode = BindingMode.OneWay,
                };
                multiBinding.Bindings.Add(KeyBinding);
                multiBinding.Bindings.Add(new Binding(nameof(CultureChangeTrigger.Value))
                {
                    Source = new CultureChangeTrigger(request.Service),
                    Mode = BindingMode.OneWay,
                });
                binding = multiBinding;
            }
            else
            {
                // Explicitly one-way: a property such as TextBox.Text defaults to two-way.
                binding = new Binding(nameof(ToolkitLocalizationProxy.Value))
                {
                    Source = new ToolkitLocalizationProxy(request, Key!),
                    Mode = BindingMode.OneWay,
                };
            }

            // A real, connected element: produce the expression for it, as Binding itself does.
            // Inside a DataTemplate/ControlTemplate, TargetObject is WPF's shared template
            // placeholder (System.Windows.SharedDp) rather than the real per-row element, so
            // there's nothing to attach to yet. Returning the BindingBase itself works because
            // WPF's deferred template-content loader recognizes it and wires it up itself, once
            // per realized row, against that row's own real element and DataContext.
            return target.TargetObject is DependencyObject
                ? binding.ProvideValue(serviceProvider)
                : binding;
        }

        if (target?.TargetObject is DependencyObject depObj && target.TargetProperty is PropertyInfo)
        {
            // A plain CLR property (e.g. Run.Text) can't take a binding, so a proxy property on
            // the element is bound instead and pushes each new value into the CLR property. That
            // proxy is one per element, so it only suits a single such property per element.
            if (formatArgs.Count > 0)
            {
                // The argument bindings resolve against the element's DataContext like any other,
                // so one multi-binding covers the key (fixed or bound) and every argument.
                var formattedBinding = FormatArguments.CreateMultiBinding(request, Key, KeyBinding, formatArgs);
                return LocalizedValueTargetBinder.Bind(depObj, target.TargetProperty, formattedBinding);
            }

            if (KeyBinding != null)
            {
                var dynamicProxy = new DynamicToolkitLocalizationProxy(request);
                DynamicKeyBinder.Bind(depObj, dynamicProxy, KeyBinding);
                var dynamicBinding = new Binding(nameof(DynamicToolkitLocalizationProxy.Value)) { Source = dynamicProxy };
                return LocalizedValueTargetBinder.Bind(depObj, target.TargetProperty, dynamicBinding);
            }

            var proxy = new ToolkitLocalizationProxy(request, Key!);
            var binding = new Binding(nameof(ToolkitLocalizationProxy.Value)) { Source = proxy };
            return LocalizedValueTargetBinder.Bind(depObj, target.TargetProperty, binding);
        }

        // Reached when the target property is a plain CLR property with no real, connected
        // DependencyObject to work with. Inside a DataTemplate/ControlTemplate that's because
        // TargetObject is WPF's shared template placeholder rather than the real per-row
        // element — there's no DependencyObject there to hang a live binding off, and (unlike
        // the DependencyProperty case above) no deferred-loader support to fall back on either.
        // Outside a template it means IProvideValueTarget wasn't available at all.
        if (KeyBinding != null || formatArgs.Count > 0)
        {
            // Never fall back to a silent empty string here — a KeyBinding or format argument
            // truly cannot be resolved without a live element to bind the source against, so say so.
            throw new LocalizationConfigurationException(
                $"{nameof(LocalizedValueExtension)}.{nameof(KeyBinding)} and format arguments ({nameof(Args)}, " +
                "Arg0..Arg9) can't be resolved here: the target property is a plain CLR property (not a " +
                "DependencyProperty) with no live, connected element to bind the source against — most " +
                "likely because this is used inside a DataTemplate or ControlTemplate. Target a " +
                "DependencyProperty instead (e.g. TextBlock.Text rather than Run.Text), or use a literal " +
                "Key with no arguments.");
        }

        // Literal Key with no live target: resolved once, non-live. Inside a template this
        // means the text won't update on a later culture change — a known limitation for this
        // specific combination (plain CLR property target + template).
        return request.Resolve(Key!);
    }

    /// <summary>
    /// Builds the <see cref="LocalizationRequest"/> both the literal <see cref="Key"/> and the
    /// <see cref="KeyBinding"/> paths resolve through - see <see cref="LocalizationRequest.Create"/>.
    /// </summary>
    /// <param name="serviceProvider">The XAML service provider used to infer the context assembly.</param>
    private LocalizationRequest CreateRequest(IServiceProvider serviceProvider)
    {
        if (Source is not null && Assembly is not null)
        {
            throw new LocalizationConfigurationException(
                $"{nameof(LocalizedValueExtension)}.{nameof(Source)} and .{nameof(Assembly)} can't both be set - " +
                $"Source '{Source}' already names its assembly.");
        }

        return LocalizationRequest.Create(Source, Assembly, KeyPrefix, serviceProvider);
    }
}

/// <summary>
/// Workaround for things like Run, which don't take a binding value.
/// </summary>
/// <remarks>
/// The helper properties are attached once per element, so this supports only one plain CLR
/// property per element. <see cref="DependencyProperty"/> targets don't use it.
/// </remarks>
internal static class LocalizedValueTargetBinder
{
    // Stores which real member (DependencyProperty or PropertyInfo) to push updates into.
    private static readonly DependencyProperty RealTargetProperty =
        DependencyProperty.RegisterAttached("RealTarget", typeof(object), typeof(LocalizedValueTargetBinder));

    // The actual bound property — a real DP, so explicit SetBinding works reliably on it.
    private static readonly DependencyProperty ProxyValueProperty =
        DependencyProperty.RegisterAttached("ProxyValue", typeof(string), typeof(LocalizedValueTargetBinder),
            new PropertyMetadata(null, OnProxyValueChanged));

    /// <summary>
    /// Binds <paramref name="binding"/> to a proxy property on the target, which pushes each new
    /// value into the real target member.
    /// </summary>
    /// <param name="targetObject">The element the markup extension is applied to.</param>
    /// <param name="realTargetMember">The <see cref="DependencyProperty"/> or <see cref="PropertyInfo"/> to update.</param>
    /// <param name="binding">The binding that supplies the localized text.</param>
    /// <returns>The initial localized text, or an empty string if none is available yet.</returns>
    public static string Bind(DependencyObject targetObject, object realTargetMember, BindingBase binding)
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