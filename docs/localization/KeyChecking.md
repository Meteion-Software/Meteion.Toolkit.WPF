# Localization key checking
[Meteion.Toolkit.Localization.Check](https://www.nuget.org/packages/Meteion.Toolkit.Localization.Check) scans your `.resx` resources and XAML for localization gaps that would otherwise only surface at runtime (or not at all).

Add it to your app project:
```bash
dotnet add package Meteion.Toolkit.Localization.Check
```
That's it - it wires itself into your build via an MSBuild target and prints a warning for every issue it finds, before you ever hit F5:
```
Strings.ja-JP.resx: warning LOC001: Key 'ScopeID' is defined in 'Strings.resx' but is missing from the 'ja-JP' locale.
View.xaml(12): warning LOC003: Key 'SomeTypo' is used here but is not defined in the .resx it resolves to and will throw or fail to resolve at runtime.
```

| Code | Default | Meaning |
| --- | --- | --- |
| LOC001 | Warning | A key in a neutral resx is missing from one of its satellites (silently falls back to the default culture) |
| LOC002 | Warning | A satellite has a key its neutral resx doesn't (likely a typo or leftover from a rename) |
| LOC003 | Warning | A key used in XAML doesn't exist in the resx it resolves to - a literal, a generated constant via `x:Static`, or `Source` + an unqualified key. This is the one that throws at runtime |
| LOC004 | Warning | An unqualified key with no `Source` in a project with more than one resx - ambiguous at runtime |
| LOC005 | Warning | A qualified key combined with `KeyPrefix` |
| LOC006 | Warning | `Source` names a resx that doesn't exist in this project |
| LOC007 | Info | A key or `Source` points at another assembly, so it can't be checked from this project |
| LOC008 | Warning | `Source` and `Assembly` are both set - `Source` already names its assembly, so this always throws at runtime |
| LOC009 | Warning | A plain string literal in a user-visible property or element text (opt-in, see below) |
| LOC010 | Warning | A `<!-- loc-ignore -->` comment gives no reason (opt-in, see below) |

## Flagging un-localized literals

The checker can also flag plain string literals that were never localized. It's off by default:

```xml
<PropertyGroup>
  <MeteionLocalizationCheckLiterals>true</MeteionLocalizationCheckLiterals>
</PropertyGroup>
```

LOC009 is reported for a plain literal in `Text`, `Content`, `Header`, `ToolTip`, `Title`, `Watermark`,
`AutomationProperties.Name` and the Telerik `NullText`, `EmptyText`, `WatermarkContent`, `Label` and `Caption`,
as an attribute, a property element (`<Button.Content>`), or as the text of an element such as `<TextBlock>` or
`<Button>`. Markup extensions (`{lx:LocalizedValue ...}`, `{Binding}`, `{x:Static}`, `{StaticResource}`) and
strings with no letters (`":"`, `"..."`, `"1"`) are not flagged. Add more properties (`;`-separated):

```xml
<MeteionLocalizationLiteralProperties>Hint;PlaceholderText</MeteionLocalizationLiteralProperties>
```

To intentionally leave an element un-localized, put a comment with a reason immediately before it. It covers that
element's own properties and text, not its children:

```xml
<!-- loc-ignore: product name -->
<TextBlock Text="Meteion" />
```

A comment without a reason still suppresses LOC009 but raises LOC010. Both codes can be promoted to errors like any
other. By hand, the switches are `--check-literals` and `--literal-property <name>`.

## Treating localization issues as errors

Every code except LOC007 can be promoted to a build error. Make them all errors:

```xml
<PropertyGroup>
  <MeteionLocalizationWarningsAsErrors>true</MeteionLocalizationWarningsAsErrors>
</PropertyGroup>
```

Or only specific codes (`;`-separated):

```xml
<PropertyGroup>
  <MeteionLocalizationErrorCodes>LOC003;LOC004</MeteionLocalizationErrorCodes>
</PropertyGroup>
```

Set `RunLocalizationKeyCheck` to `false` to skip the check entirely. When running the checker by hand,
the same switches are `--warnaserror` and `--error <codes>`.

You are also able to use the checker's API directly via the `PackageReference`, enabling you to check resources on startup or in unit tests:
```csharp
var result = Meteion.Toolkit.Localization.Check.LocalizationKeyChecker.CheckDirectory("path/to/project");
```

**Known limitations**: `KeyBinding`-sourced dynamic keys can't be checked statically, by design (only
their `Source` is), and C# code-behind usages aren't checked. Keys that point at another assembly
(qualified keys, `Source`, or an `x:Static` into a `clr-namespace:...;assembly=...` mapping) report
LOC007 rather than being verified, since the checker only scans the current project. The generator's
per-file overrides (`MeteionResourceBaseName`, `MeteionKeysNamespace`, `MeteionKeysClassName`) aren't
read by the checker, so keys from a resx using them can't be verified either.

If you'd rather not ship the checker's own assemblies inside your app at all (they're a build-time tool, not a runtime dependency), reference it as:

```
<PackageReference Include="Meteion.Toolkit.Localization.Check" PrivateAssets="all" IncludeAssets="build;buildTransitive" />
```

Then the automatic pre-build check still runs, you just lose the ability to call the API directly from that project.
