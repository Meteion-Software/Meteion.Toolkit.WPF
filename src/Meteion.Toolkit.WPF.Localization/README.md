# Meteion.Toolkit.WPF.Localization

This project provides localization support for WPF applications using the Meteion Toolkit. It includes resources and utilities to facilitate the translation of UI elements and messages into different languages.

It stores localization resources in RESX files in your assembly by default.

## Benefits

### Over {x:Static resources:Resources.blah}
- Can change at runtime
- Can be easily displayed in the Designer
- Can handle non-dependency properties

## Usage

Bind a fixed, XAML-known resource key. With
[Meteion.Toolkit.Localization.KeysGenerator](#key-autocompletion) (recommended), use the generated
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

You can also optionally prepend a fixed `KeyPrefix` so the bound (or literal) source only needs to supply a
short per-item suffix, while the shared part of the key lives once in XAML. Since the bound suffix is an
unqualified key, point `Source` at the resx it belongs to - its generated `ResxSource` constant:

```xml
<TextBlock Text="{lx:LocalizedValue Source={x:Static strings:StringsKeys.ResxSource}, KeyPrefix=Feature_, KeyBinding={Binding Key}}" />
```

In code-behind, pass generated constants straight to `ILocalizationService` (or `ToolkitLocalizer` outside DI):

```csharp
var text = loc.GetString(StringsKeys.Greeting);
var feature = loc.GetString("Feature_" + suffix, StringsKeys.ResxSource);
```

## Multiple resx files

An assembly can hold any number of resx files - e.g. one per feature folder, rather than one huge
`Resources.resx` full of `Category_Page_Thing` keys. Every key then has to say which resx it's in, so
keys come in two forms:

| Form | Example | Resolves from |
| --- | --- | --- |
| Qualified | `MyApp/MyApp.Properties.Strings:Greeting` | The named assembly and resx |
| Unqualified | `Greeting` | `Source`, or else the assembly's only resx |

A qualified key is `<AssemblyName>/<ResourceBaseName>:<Key>`, where the resource base name is the
resx's manifest resource name (what `ResourceManager` takes) - `RootNamespace` + folder + file name by
default. You'll rarely type one: the generator's constants hold qualified keys, and each generated
class's `ResxSource` constant holds the `<AssemblyName>/<ResourceBaseName>` part for `Source`.

The rules `LocalizedValue` applies, to a literal `Key` and to every value a `KeyBinding` produces:

1. `Source` and `Assembly` can't both be set - `Source` already names the assembly.
2. A qualified key can't be combined with `KeyPrefix`.
3. A qualified key must agree with `Source`/`Assembly` when either is set.
4. An unqualified key + `Source` resolves `Source:KeyPrefix+key`.
5. An unqualified key without `Source` resolves against the assembly (`Assembly`, else the XAML's own
   assembly, else `LocalizationOptions.DefaultAssembly`)'s only resx - and throws if it has several.

A literal `Key` that breaks a rule throws `LocalizationConfigurationException`. A bound value that
breaks one is always reported in Visual Studio's XAML Binding Failures output, then handled like a
missing key according to `LocalizationOptions.MissingKeyBehavior`: `ThrowException` rethrows the
`LocalizationConfigurationException`, `ReturnKey` displays the raw value, and `ReturnEmptyString`
displays nothing.

Because a qualified key carries its assembly, keys from a referenced library just work:
`{lx:LocalizedValue {x:Static shared:CommonKeys.Ok}}` resolves from the library's resx even from your
app's XAML, with no `Assembly=` needed.

## Changing the current culture at runtime

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

## Culture-aware number/date/currency formatting

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

## Naming Conventions
Each resx "family" is a neutral resx plus its per-language satellites. Follow the standard .NET convention:

- **Neutral/default resx**: `Strings.resx` (no culture suffix). This is the one embedded directly in the assembly and used as the fallback when no more specific culture matches.
- **One resx per additional language**: `Strings.{culture}.resx` (e.g. `Strings.en-CA.resx`, `Strings.ja-JP.resx`). MSBuild recognizes the shared `Strings` base name plus a valid culture suffix and compiles each of these into its own **satellite assembly** automatically — `ResourceManager` finds them at runtime by walking the culture's fallback chain.

Prefer several focused families - one per feature folder, e.g. `Views/Home/HomeStrings.resx` and
`Views/Settings/SettingsStrings.resx` - over a single family with long underscore-separated key prefixes.
See [Multiple resx files](#multiple-resx-files).

## Recommendations

The [ResxManager](https://marketplace.visualstudio.com/items?itemName=TomEnglert.ResXManager) is fantastic!


## Key autocompletion
[Meteion.Toolkit.Localization.KeysGenerator](https://www.nuget.org/packages/Meteion.Toolkit.Localization.KeysGenerator) is a Roslyn source generator that turns every key in your neutral `.resx` files into a `public const string`, so both code-behind and XAML get autocompletion over your resource keys - and a rename or typo becomes a compile error instead of a silent runtime miss.

Add it to your app project:
```bash
dotnet add package Meteion.Toolkit.Localization.KeysGenerator
```
That's it - it wires itself into your build automatically, no other setup required. For `Properties\Strings.resx` in `MyApp`, it generates a class named `StringsKeys` (in a namespace mirroring the resx's folder, same convention the .NET SDK's own resx code generator uses) with a `ResxSource` constant plus one qualified-key constant per resx key, with the resx value as that const's XML-doc summary:

```csharp
namespace MyApp.Properties
{
    public static partial class StringsKeys
    {
        /// <summary>
        /// Identifies this resx (<c>&lt;AssemblyName&gt;/&lt;ResourceBaseName&gt;</c>) - for <c>LocalizedValueExtension.Source</c>.
        /// </summary>
        public const string ResxSource = "MyApp/MyApp.Properties.Strings";

        /// <summary>
        /// <c>"Hello there!"</c>
        /// </summary>
        public const string Greeting = "MyApp/MyApp.Properties.Strings:Greeting";
    }
}
```

Use it in code-behind exactly like any other const:
```csharp
var text = loc.GetString(StringsKeys.Greeting);
```

In XAML, reference it via `x:Static` for full IntelliSense and a compile-time-checked key:
```xml
<TextBlock Text="{lx:LocalizedValue {x:Static strings:StringsKeys.Greeting}}" />
```

You can also just type `Key="..."` as a plain string literal - `LocalizedValueExtension.Key` carries a `TypeConverter` that lists every qualified key discovered from a generated class in any loaded assembly, so Visual Studio's XAML editor offers it as an attribute-value dropdown. This is a convenience list, not a validator (it can't see a key from a different, not-yet-loaded assembly, or one that's only ever supplied via `KeyBinding`), so prefer `x:Static` when you want the key checked at compile time.

If a class/namespace name would collide with something else (e.g. the standard `ResXFileCodeGenerator`'s own `Strings.Designer.cs`, which the generator's default `{ResxBaseName}Keys` naming is deliberately chosen to avoid), override either per file. If the resx's manifest resource name isn't the MSBuild default (e.g. it has `LogicalName` or `DependentUpon` set), override the resource base name baked into its keys too:
```xml
<AdditionalFiles Update="Properties\Strings.resx" MeteionKeysClassName="Text" MeteionKeysNamespace="MyApp.Localization" />
<AdditionalFiles Update="Legacy\Old.resx" MeteionResourceBaseName="MyApp.LegacyStrings" />
```

A resx key literally named `ResxSource` collides with the generated constant of the same name: its constant is generated as `ResxSource_2` instead, with warning `MTKGEN001`.

If you'd rather not ship the generator inside your app at all (it's a build-time-only analyzer, never a runtime dependency - `dotnet add package` already sets this up correctly), reference it as:
```
<PackageReference Include="Meteion.Toolkit.Localization.KeysGenerator" PrivateAssets="all" />
```

## Localization key checking
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

### Treating localization issues as errors

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
