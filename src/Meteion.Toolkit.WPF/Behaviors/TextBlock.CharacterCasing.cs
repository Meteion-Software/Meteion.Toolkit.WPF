using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Behaviors;

// Thanks to https://stackoverflow.com/a/49443994
public static class TextBlock
{
    public static readonly DependencyProperty CharacterCasingProperty = DependencyProperty.RegisterAttached(
        "CharacterCasing",
        typeof(CharacterCasing),
        typeof(TextBlock),
        new FrameworkPropertyMetadata(
            CharacterCasing.Normal,
            FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.NotDataBindable,
            OnCharacterCasingChanged));

    private static readonly DependencyProperty TextProxyProperty = DependencyProperty.RegisterAttached(
        "TextProxy",
        typeof(string),
        typeof(TextBlock),
        new PropertyMetadata(default(string), OnTextProxyChanged));

    private static readonly PropertyPath TextPropertyPath = new PropertyPath("Text");

    public static void SetCharacterCasing(DependencyObject element, CharacterCasing value)
    {
        element.SetValue(CharacterCasingProperty, value);
    }

    public static CharacterCasing GetCharacterCasing(DependencyObject element)
    {
        return (CharacterCasing)element.GetValue(CharacterCasingProperty);
    }

    private static void OnCharacterCasingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is System.Windows.Controls.TextBlock textBlock)
        {
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
