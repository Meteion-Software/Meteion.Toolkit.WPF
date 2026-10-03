# Usage

Bind a fixed, XAML-known resource key. With
[Meteion.Toolkit.Localization.KeysGenerator](KeyAutocompletion.md) (recommended), use the generated
constant via `x:Static` - it names the key's assembly and resx, so it resolves correctly no matter
how many resx files your project has, or which assembly the XAML lives in:

```xml
<Page xmlns:lx="http://wpf.meteion.ca/winfx/xaml/localization"
      xmlns:strings="clr-namespace:MyApp.Properties">
    <TextBlock Text="{lx:LocalizedValue {x:Static strings:StringsKeys.Greeting}}" />
</Page>
```

In an assembly with a single resx, a plain unqualified key also works:

```xml
<TextBlock Text="{lx:LocalizedValue Key=Greeting}" />
<TextBlock Text="{lx:LocalizedValue Greeting}" />
```

Bind a *dynamic* per-item key instead — e.g. a key coming from a bound view-model/model
property, such as an item in an `ItemsControl`'s `DataTemplate` — with `KeyBinding`. The
displayed text updates live both when the bound key changes and whenever the display
culture changes. Bound values can be generated constants (qualified keys) too:

```xml
<TextBlock Text="{lx:LocalizedValue KeyBinding={Binding TitleKey}}" />
```

If both `Key` and `KeyBinding` are set, `KeyBinding` takes precedence.

You can use several `LocalizedValue` extensions on one element, as long as each targets a different
property (for example a dialog's `Title`, `PrimaryButtonText` and `SecondaryButtonText`). The one
exception is a plain CLR property that isn't a `DependencyProperty`, such as `Run.Text`: only one of those
is supported per element.

You can also optionally prepend a fixed `KeyPrefix` so the bound (or literal) source only needs to supply a
short per-item suffix, while the shared part of the key lives once in XAML. Since the bound suffix is an
unqualified key, point `Source` at the resx it belongs to - its generated `ResxSource` constant:

```xml
<TextBlock Text="{lx:LocalizedValue Source={x:Static strings:StringsKeys.ResxSource}, KeyPrefix=Feature_, KeyBinding={Binding Key}}" />
```

**When the bound key is missing**: a `KeyBinding` that produces `null`, `DependencyProperty.UnsetValue`
(e.g. a path that doesn't resolve) or `Binding.DoNothing` means "no key", and renders as an empty string.
Nothing is looked up, so `MissingKeyBehavior` doesn't apply. `UnsetValue` also writes a warning to the WPF
binding trace (the XAML Binding Failures window) since it usually points at a broken path or `DataContext`;
`null` is left quiet as a legitimate "not loaded yet". An *empty string* is not special-cased - it's an
ordinary lookup (with `KeyPrefix` applied), so a forgotten key still surfaces through `MissingKeyBehavior`.

To show a placeholder instead of blank text, set `FallbackValue` (unresolved binding) and/or
`TargetNullValue` (null) on the `KeyBinding` itself. They supply a *key*, not display text, so the value
goes through the normal lookup and gets `KeyPrefix` applied:

```xml
<TextBlock Text="{lx:LocalizedValue Source={x:Static strings:StringsKeys.ResxSource}, KeyPrefix=Feature_,
                                    KeyBinding={Binding Key, FallbackValue=Unknown, TargetNullValue=Unknown}}" />
```

(Here `Feature_Unknown` must exist in the resx.)

In code-behind, pass generated constants straight to `ILocalizationService` (or `ToolkitLocalizer` outside DI):

```csharp
var text = loc.GetString(StringsKeys.Greeting);
var feature = loc.GetString("Feature_" + suffix, StringsKeys.ResxSource);
```

For targets that are not a dependency property on a live element - notably `DataGridTextColumn.Binding` - use
[`LocalizedBinding`](LocalizedBinding.md) instead.
