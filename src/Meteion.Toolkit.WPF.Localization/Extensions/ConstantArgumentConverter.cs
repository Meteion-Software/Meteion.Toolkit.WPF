using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Localization.Extensions;

/// <summary>
/// Lets a plain text value stand in for a format argument, so <c>Arg1=5</c> works next to
/// <c>Arg0={Binding Name}</c>. The <c>ArgN</c> properties are typed <see cref="BindingBase"/>
/// because WPF refuses a <c>{Binding}</c> on an <see cref="object"/>-typed property of a markup
/// extension; this converter covers the literal case by wrapping the text in a one-way binding
/// to itself.
/// </summary>
/// <remarks>
/// The value is always a <see cref="string"/>, so a numeric placeholder such as <c>{0:N0}</c> won't
/// format a literal. Use a typed source for that, e.g. <c>Arg0={Binding Source={x:Static ...}}</c>.
/// </remarks>
public sealed class ConstantArgumentConverter : TypeConverter
{
    /// <inheritdoc />
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    /// <inheritdoc />
    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        value is string text
            ? new Binding { Source = text, Mode = BindingMode.OneWay }
            : base.ConvertFrom(context, culture, value);
}
