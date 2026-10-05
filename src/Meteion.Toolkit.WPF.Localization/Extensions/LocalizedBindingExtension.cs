using Meteion.Toolkit.Localization.Abstractions;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Meteion.Toolkit.WPF.Localization.Extensions;

/// <summary>
/// A one-way binding whose value is the localized text for a bound resource key. Use it where
/// <see cref="LocalizedValueExtension"/> can't be - chiefly <c>DataGridTextColumn.Binding</c>,
/// a plain CLR property typed <see cref="BindingBase"/> that <see cref="LocalizedValueExtension"/>
/// has no live element to attach to.
/// </summary>
/// <remarks>
/// Resolves exactly like <see cref="LocalizedValueExtension.KeyBinding"/> (same
/// <see cref="KeyPrefix"/> / <see cref="Source"/> / <see cref="Assembly"/> rules, same handling of
/// null/unset keys and missing resources), and re-resolves on both a key change and a culture
/// change. It can only ever be one-way: localized text can't be converted back to a key.
/// <para>
/// The declared return type is <see cref="object"/> for the same reason as on
/// <see cref="LocalizedValueExtension"/>.
/// </para>
/// </remarks>
/// <example>
/// <c>&lt;DataGridTextColumn Binding="{lx:LocalizedBinding KeyBinding={Binding StatusKey}}" /&gt;</c>
/// </example>
[MarkupExtensionReturnType(typeof(object))]
public class LocalizedBindingExtension : MarkupExtension
{
    /// <summary>The binding that supplies the resource key to resolve.</summary>
    public BindingBase? KeyBinding { get; set; }

    /// <summary>
    /// Optional literal string prepended to each bound key before lookup - see
    /// <see cref="LocalizedValueExtension.KeyPrefix"/>.
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Optional resx identity (<c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;</c>) that unqualified
    /// keys are resolved from - see <see cref="LocalizedValueExtension.Source"/>. Can't be combined
    /// with <see cref="Assembly"/>.
    /// </summary>
    public string? Source { get; set; }

    /// <summary>The assembly unqualified keys resolve against - see <see cref="LocalizedValueExtension.Assembly"/>.</summary>
    public Assembly? Assembly { get; set; }

    /// <summary>
    /// The resource <b>key</b> (not display text) to localize when <see cref="KeyBinding"/> fails
    /// to resolve. Applied to <see cref="KeyBinding"/> unless it already sets its own.
    /// </summary>
    public object? FallbackValue { get; set; } = DependencyProperty.UnsetValue;

    /// <summary>
    /// The resource <b>key</b> (not display text) to localize when <see cref="KeyBinding"/>
    /// produces <see langword="null"/>. Applied to <see cref="KeyBinding"/> unless it already
    /// sets its own.
    /// </summary>
    public object? TargetNullValue { get; set; } = DependencyProperty.UnsetValue;

    /// <summary>
    /// Optional format arguments - see <see cref="LocalizedValueExtension.Args"/>. Can't be
    /// combined with <see cref="Arg0"/>..<see cref="Arg9"/>.
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

    /// <summary>Creates the extension with its properties to be set by the caller.</summary>
    public LocalizedBindingExtension() { }

    /// <summary>Creates the extension for the given key binding.</summary>
    /// <param name="keyBinding">The binding that supplies the resource key to resolve.</param>
    public LocalizedBindingExtension(BindingBase keyBinding) => KeyBinding = keyBinding;

    /// <summary>
    /// Builds a one-way multi-binding that localizes the bound key and re-runs on culture changes.
    /// </summary>
    /// <param name="serviceProvider">The XAML service provider for the current usage.</param>
    /// <returns>The binding expression for the target.</returns>
    /// <exception cref="LocalizationConfigurationException"><see cref="KeyBinding"/> was not set.</exception>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (KeyBinding == null)
        {
            throw new LocalizationConfigurationException(
                $"{nameof(LocalizedBindingExtension)}.{nameof(KeyBinding)} must be set to the binding " +
                "supplying the resource key to localize.");
        }

        // In the designer there's no host application to resolve the localization service from
        // (Application.Current is the designer's own surface app), so skip it entirely and show
        // the raw key instead. The binding stays a BindingBase, which is what the target needs.
        if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
        {
            return KeyBinding.ProvideValue(serviceProvider);
        }

        // Source + Assembly together is rejected here, with the others from the shared rules.
        var request = LocalizationRequest.Create(Source, Assembly, KeyPrefix, serviceProvider);

        // Fallback/null values supply a key, so they go on the key binding, where the converter
        // then localizes them - never on the outer MultiBinding, which would show them raw.
        if (KeyBinding.FallbackValue == DependencyProperty.UnsetValue)
        {
            KeyBinding.FallbackValue = FallbackValue;
        }

        if (KeyBinding.TargetNullValue == DependencyProperty.UnsetValue)
        {
            KeyBinding.TargetNullValue = TargetNullValue;
        }

        // Unlike LocalizedValueExtension, nothing is pushed into the target, so there's no
        // DependencyProperty / CLR property / template distinction to make - the same
        // MultiBinding works for any BindingBase-typed target.
        var formatArgs = FormatArguments.Collect(Args, _args, nameof(LocalizedBindingExtension));

        MultiBinding multiBinding;
        if (formatArgs.Count > 0)
        {
            multiBinding = FormatArguments.CreateMultiBinding(request, null, KeyBinding, formatArgs);
        }
        else
        {
            multiBinding = new MultiBinding
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
        }

        return multiBinding.ProvideValue(serviceProvider);
    }
}
