# Changing the current culture at runtime

Set `ILocalizationService.CurrentCulture` (or, outside DI, `ToolkitLocalizer.CurrentCulture`) to switch
the app's language independently of the OS locale — no restart required:

```csharp
_localizationService.CurrentCulture = new CultureInfo("ja-JP");
```

This keeps everything in sync, not just resx string lookups:

- `CultureInfo.CurrentCulture` / `CurrentUICulture` (thread-scoped) — so code that formats without an
  explicit culture/provider (`DateTime.ToString()`, `decimal.ToString()`, etc.) follows the selected
  language instead of the OS locale.
- `FrameworkElement.Language` on every open window — WPF's own binding pipeline (`StringFormat`, and
  any bound value's implicit `ToString` conversion) resolves its formatting culture from there, *not*
  from `CultureInfo.CurrentCulture`, and every element defaults it to `en-US` regardless of the OS.
  Without this, a plain `{Binding Total, StringFormat=C}` would never follow a language change.
