# LocalizedBinding (DataGrid columns and other `BindingBase` properties)

`LocalizedValue` assigns text to a target element, so it can't be used where the target is a plain CLR
property with no element to attach to - most commonly `DataGridTextColumn.Binding`. Use `LocalizedBinding`
there. It returns a one-way binding whose value is the localized text for the bound key, and accepts any
`BindingBase`-typed property (or a normal dependency property):

```xml
<DataGridTextColumn Header="Status"
                    Binding="{lx:LocalizedBinding Source={x:Static strings:StringsKeys.ResxSource},
                                                  KeyPrefix=Status_,
                                                  KeyBinding={Binding StatusKey}}" />
```

`KeyBinding`, `KeyPrefix`, `Source` and `Assembly` behave exactly as on `LocalizedValue`, as does the handling
of `null` / unresolved keys and missing resources, and the text re-resolves live on a key or culture change.
`KeyBinding` is required.

- **One way only.** Localized text can't be converted back to a key, so the column can't be edited through this
  binding - use a separate, non-localized column for editing.
- **`FallbackValue` / `TargetNullValue`** may be set on the extension or on the `KeyBinding`. As with
  `LocalizedValue`, they are *keys*, not display text, and go through the normal lookup (with `KeyPrefix`
  applied). A value already set on the `KeyBinding` itself wins over the extension's.
