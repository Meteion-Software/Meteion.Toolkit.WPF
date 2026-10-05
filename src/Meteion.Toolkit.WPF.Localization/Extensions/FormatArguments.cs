using Meteion.Toolkit.Localization.Abstractions;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Meteion.Toolkit.WPF.Localization.Extensions;

/// <summary>
/// The shared rules for format arguments: collecting them from a markup extension's
/// <c>Args</c> / <c>Arg0</c>..<c>Arg9</c> properties, building the <see cref="MultiBinding"/> that
/// feeds them to <see cref="FormattedLocalizationConverter"/>, and applying them to a composite
/// format string with the app's culture and <see cref="MissingResourceBehavior"/>.
/// </summary>
internal static class FormatArguments
{
    /// <summary>How many inline <c>ArgN</c> shorthand properties a markup extension exposes.</summary>
    public const int ShorthandCount = 10;

    /// <summary>
    /// Merges an extension's <c>Args</c> and <c>ArgN</c> properties into one ordered list of bindings.
    /// A literal <c>ArgN</c> has already been wrapped in a binding by
    /// <see cref="ConstantArgumentConverter"/>, so every argument reaches the converter the same way.
    /// </summary>
    /// <param name="args">The extension's <c>Args</c> multi-binding, if set.</param>
    /// <param name="shorthand">The extension's <c>Arg0</c>..<c>Arg9</c> bindings; <see langword="null"/> means not set.</param>
    /// <param name="owner">The extension's name, for error messages.</param>
    /// <returns>The ordered argument bindings; empty when the usage has no arguments.</returns>
    /// <exception cref="LocalizationConfigurationException">
    /// <c>Args</c> is combined with <c>ArgN</c>, or an <c>ArgN</c> is set while a lower one isn't.
    /// </exception>
    public static IReadOnlyList<BindingBase> Collect(MultiBinding? args, BindingBase?[] shorthand, string owner)
    {
        var last = Array.FindLastIndex(shorthand, value => value is not null);

        if (args is not null)
        {
            if (last >= 0)
            {
                throw new LocalizationConfigurationException(
                    $"{owner}.Args can't be combined with Arg0..Arg{ShorthandCount - 1}; use one or the other.");
            }

            return [.. args.Bindings];
        }

        var bindings = new List<BindingBase>(last + 1);
        for (var i = 0; i <= last; i++)
        {
            var value = shorthand[i]
                ?? throw new LocalizationConfigurationException(
                    $"{owner}.Arg{last} is set but Arg{i} isn't. Format arguments are positional, so set them in order.");

            bindings.Add(value);
        }

        return bindings;
    }

    /// <summary>
    /// Builds the one-way multi-binding that localizes a key and formats it with the arguments,
    /// re-running when the key, an argument or the culture changes. Values reach the converter as
    /// <c>[key (only with a key binding), culture trigger, args...]</c>.
    /// </summary>
    /// <param name="request">The lookup settings used to resolve the key.</param>
    /// <param name="fixedKey">The literal key, used when <paramref name="keyBinding"/> is <see langword="null"/>.</param>
    /// <param name="keyBinding">The binding that supplies the key, if any.</param>
    /// <param name="args">The ordered argument bindings.</param>
    /// <returns>The multi-binding, not yet attached to a target.</returns>
    public static MultiBinding CreateMultiBinding(
        LocalizationRequest request,
        string? fixedKey,
        BindingBase? keyBinding,
        IReadOnlyList<BindingBase> args)
    {
        if (keyBinding is null)
        {
            // Resolve once up front so a bad literal key fails at load time, as it does on the
            // no-args path (ToolkitLocalizationProxy resolves in its constructor).
            request.Resolve(fixedKey!);
        }

        var multiBinding = new MultiBinding
        {
            Converter = new FormattedLocalizationConverter(request, keyBinding is null ? fixedKey : null),
            Mode = BindingMode.OneWay,
        };

        if (keyBinding is not null)
        {
            multiBinding.Bindings.Add(keyBinding);
        }

        multiBinding.Bindings.Add(new Binding(nameof(CultureChangeTrigger.Value))
        {
            Source = new CultureChangeTrigger(request.Service),
            Mode = BindingMode.OneWay,
        });

        foreach (var arg in args)
        {
            multiBinding.Bindings.Add(arg);
        }

        return multiBinding;
    }

    /// <summary>
    /// Applies <paramref name="args"/> to the composite format string <paramref name="template"/>.
    /// Arguments a failed binding produced (<see cref="DependencyProperty.UnsetValue"/>,
    /// <see cref="Binding.DoNothing"/>) render as empty, like <see langword="null"/>.
    /// </summary>
    /// <param name="culture">The culture used for number, date and currency placeholders.</param>
    /// <param name="template">The composite format string, e.g. <c>"{0} has {1:N0} items"</c>.</param>
    /// <param name="args">The raw argument values.</param>
    /// <returns>The formatted text.</returns>
    /// <exception cref="LocalizationConfigurationException">The template is invalid or needs more arguments than were given.</exception>
    public static string Format(CultureInfo culture, string template, object?[] args)
    {
        var normalized = new object?[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            normalized[i] = ReferenceEquals(args[i], DependencyProperty.UnsetValue) || ReferenceEquals(args[i], Binding.DoNothing)
                ? null
                : args[i];
        }

        try
        {
            return string.Format(culture, template, normalized);
        }
        catch (FormatException ex)
        {
            throw new LocalizationConfigurationException(
                $"Could not format '{template}' with {args.Length} argument(s): {ex.Message}", ex);
        }
    }

    /// <summary>
    /// <see cref="Format"/> for a binding: a failure is always traced to the XAML binding-failure
    /// output, then handled per <paramref name="behavior"/> - rethrown, or shown as the unformatted
    /// template or an empty string.
    /// </summary>
    /// <param name="culture">The culture used for number, date and currency placeholders.</param>
    /// <param name="template">The composite format string.</param>
    /// <param name="args">The raw argument values.</param>
    /// <param name="behavior">The app's <see cref="LocalizationOptions.MissingKeyBehavior"/>.</param>
    /// <returns>The formatted text, or the fallback dictated by <paramref name="behavior"/>.</returns>
    public static string FormatForBinding(CultureInfo culture, string template, object?[] args, MissingResourceBehavior behavior)
    {
        try
        {
            return Format(culture, template, args);
        }
        catch (LocalizationConfigurationException ex)
        {
            LocalizationTraceSource.TraceConfigurationError(ex.Message);

            switch (behavior)
            {
                case MissingResourceBehavior.ReturnKey:
                    return template;
                case MissingResourceBehavior.ReturnEmptyString:
                    return string.Empty;
                default:
                    throw;
            }
        }
    }
}
