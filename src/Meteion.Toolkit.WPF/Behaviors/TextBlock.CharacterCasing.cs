using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Behaviors;

// Thanks to https://stackoverflow.com/a/49443994
/// <summary>
/// Attached properties for <see cref="System.Windows.Controls.TextBlock"/>. Named after the element it extends so
/// XAML reads <c>mtk:TextBlock.CharacterCasing</c>.
/// </summary>
public static class TextBlock
{
    /// <summary>
    /// Attached property that applies a <see cref="CharacterCasing"/> to a TextBlock's displayed text. Inherited by
    /// child elements.
    /// </summary>
    public static readonly DependencyProperty CharacterCasingProperty = DependencyProperty.RegisterAttached(
        "CharacterCasing",
        typeof(CharacterCasing),
        typeof(TextBlock),
        new FrameworkPropertyMetadata(
            CharacterCasing.Normal,
            FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.NotDataBindable,
            OnCharacterCasingChanged));

    // Mirrors TextBlock.Text through a binding so the casing is re-applied whenever the text changes.
    private static readonly DependencyProperty TextProxyProperty = DependencyProperty.RegisterAttached(
        "TextProxy",
        typeof(string),
        typeof(TextBlock),
        new PropertyMetadata(default(string), OnTextProxyChanged));

    private static readonly PropertyPath TextPropertyPath = new PropertyPath("Text");

    /// <summary>Sets the <see cref="CharacterCasingProperty"/> value on an element.</summary>
    /// <param name="element">The element to set the casing on.</param>
    /// <param name="value">The casing to apply.</param>
    public static void SetCharacterCasing(DependencyObject element, CharacterCasing value)
    {
        element.SetValue(CharacterCasingProperty, value);
    }

    /// <summary>Gets the <see cref="CharacterCasingProperty"/> value from an element.</summary>
    /// <param name="element">The element to read the casing from.</param>
    /// <returns>The casing applied to the element.</returns>
    public static CharacterCasing GetCharacterCasing(DependencyObject element)
    {
        return (CharacterCasing)element.GetValue(CharacterCasingProperty);
    }

    private static void OnCharacterCasingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is System.Windows.Controls.TextBlock textBlock)
        {
            // Bind the proxy to Text once; later casing changes just reformat via OnTextProxyChanged.
            if (BindingOperations.GetBinding(textBlock, TextProxyProperty) == null)
            {
                BindingOperations.SetBinding(
                    textBlock,
                    TextProxyProperty,
                    new Binding
                    {
                        Path = TextPropertyPath,
                        RelativeSource = RelativeSource.Self,
                        Mode = BindingMode.OneWay,
                    });
            }
        }
    }

    private static void OnTextProxyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        // SetCurrentValue keeps the original Text binding intact; the reformatted text feeds back into the proxy,
        // which is harmless because casing is idempotent.
        d.SetCurrentValue(System.Windows.Controls.TextBlock.TextProperty, Format((string)e.NewValue, GetCharacterCasing(d)));

        static string Format(string text, CharacterCasing casing)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            return casing switch
            {
                CharacterCasing.Normal => text,
                CharacterCasing.Lower => text.ToLower(),
                CharacterCasing.Upper => text.ToUpper(),
                _ => throw new ArgumentOutOfRangeException(nameof(casing), casing, null),
            };
        }
    }
}
