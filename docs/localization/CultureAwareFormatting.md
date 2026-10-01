# Culture-aware number/date/currency formatting

A plain `{Binding ..., StringFormat=...}` only reformats when the *bound value itself* changes — not
when the user later switches languages via `CurrentCulture` above. `CultureAwareFormatExtension`
formats a bound `IFormattable` value (numbers, dates, currency, ...) using
`ILocalizationService.CurrentCulture`, live: the displayed text updates both when the bound value
changes and whenever the current culture changes.

```xml
<TextBlock Text="{lx:CultureAwareFormat Value={Binding Total}, FormatString=C}" />
<TextBlock Text="{lx:CultureAwareFormat Value={Binding Today}, FormatString=D}" />
```

`FormatString` accepts any standard or custom .NET format string (e.g. `C`, `N2`, `d`, `D`).

For a one-off, non-live conversion — or to use it as an ordinary `Binding.Converter` — the extension's
underlying `CultureAwareFormatConverter` can also be used directly:

```xml
<TextBlock Text="{Binding Total, Converter={StaticResource CultureAwareFormatConverter}, ConverterParameter=C}" />
```
