# Meteion.Toolkit.WPF.Localization

This project provides localization support for WPF applications using the Meteion Toolkit. It includes resources and utilities to facilitate the translation of UI elements and messages into different languages.

It stores localization resources in RESX files in your assembly by default.

## Benefits

### Over {x:Static resources:Resources.blah}
- Can change at runtime
- Can be easily displayed in the Designer
- Can handle non-dependency properties

## Usage

Bind a fixed, XAML-known resource key:

```xml
<TextBlock Text="{lx:LocalizedValue Key=Greeting}" />
<TextBlock Text="{lx:LocalizedValue Greeting}" />
```

Bind a *dynamic* per-item key instead — e.g. a key coming from a bound view-model/model
property, such as an item in an `ItemsControl`'s `DataTemplate` — with `KeyBinding`. The
displayed text updates live both when the bound key changes and whenever the display
culture changes:

```xml
<TextBlock Text="{lx:LocalizedValue KeyBinding={Binding TitleKey}}" />
```

If both `Key` and `KeyBinding` are set, `KeyBinding` takes precedence.

You can also optionally prepend a fixed `KeyPrefix` so the bound (or literal) source only needs to supply a
short per-item suffix, while the shared resx key namespace lives once in XAML:

```xml
<TextBlock Text="{lx:LocalizedValue KeyPrefix=Feature_, KeyBinding={Binding Key}}" />
```


## Naming Conventions
This library expects **one resx "family" per assembly** — `ILocalizationProvider` only receives an `Assembly`, not a file/dictionary name, so all your localized strings for a given assembly need to live under a single shared base name.

Follow the standard .NET convention:

- **Neutral/default resx**: `Resources.resx` (no culture suffix). This is the one embedded directly in the assembly and used as the fallback when no more specific culture matches.
- **One resx per additional language**: `Resources.{culture}.resx` (e.g. `Resources.en-CA.resx`, `Resources.jp.resx`). MSBuild recognizes the shared `Resources` base name plus a valid culture suffix and compiles each of these into its own **satellite assembly** automatically — `ResourceManager` finds them at runtime by walking the culture's fallback chain.

## Recommendations

The [ResxManager](https://marketplace.visualstudio.com/items?itemName=TomEnglert.ResXManager) is fantastic!


## Key autocompletion
[Meteion.Toolkit.Localization.KeysGenerator](https://www.nuget.org/packages/Meteion.Toolkit.Localization.KeysGenerator) is a Roslyn source generator that turns every key in your neutral `.resx` into a `public const string`, so both code-behind and XAML get autocompletion over your resource keys - and a rename or typo becomes a compile error instead of a silent runtime miss.

Add it to your app project:
```bash
dotnet add package Meteion.Toolkit.Localization.KeysGenerator
```
That's it - it wires itself into your build automatically, no other setup required. For `Resources.resx`, it generates a class named `ResourcesKeys` (in a namespace mirroring the resx's folder, same convention the .NET SDK's own resx code generator uses) with one const per key, and the resx value as that const's XML-doc summary:

```csharp
namespace MyApp.Resources
{
    public static partial class ResourcesKeys
    {
        /// <summary>
        /// <c>"Hello there!"</c>
        /// </summary>
        public const string Greeting = "Greeting";
    }
}
```

Use it in code-behind exactly like any other const:
```csharp
var text = loc.GetString(ResourcesKeys.Greeting);
```

In XAML, reference it via `x:Static` for full IntelliSense and a compile-time-checked key:
```xml
<TextBlock Text="{lx:LocalizedValue Key={x:Static resources:ResourcesKeys.Greeting}}" />
```

You can also just type `Key="..."` as a plain string literal - `LocalizedValueExtension.Key` carries a `TypeConverter` that lists every key discovered from a generated class in any loaded assembly, so Visual Studio's XAML editor offers it as an attribute-value dropdown. This is a convenience list, not a validator (it can't see a key from a different, not-yet-loaded assembly, or one that's only ever supplied via `KeyBinding`), so prefer `x:Static` when you want the key checked at compile time.

If a project has more than one resx family, or a class/namespace name would collide with something else (e.g. the standard `ResXFileCodeGenerator`'s own `Resources.Designer.cs`, which the generator's default `{ResxBaseName}Keys` naming is deliberately chosen to avoid), override either per file:
```xml
<AdditionalFiles Update="Resources\Resources.resx" MeteionKeysClassName="Strings" MeteionKeysNamespace="MyApp.Localization" />
```

If you'd rather not ship the generator inside your app at all (it's a build-time-only analyzer, never a runtime dependency - `dotnet add package` already sets this up correctly), reference it as:
```
<PackageReference Include="Meteion.Toolkit.Localization.KeysGenerator" PrivateAssets="all" />
```

## Localization key checking
[Meteion.Toolkit.Localization.Check](https://www.nuget.org/packages/Meteion.Toolkit.Localization.Check) scans your `.resx` resources and XAML for two kinds of localization gaps that would otherwise only surface at runtime (or not at all):

- A key exists in your neutral (default) resx but is missing from a satellite resx (e.g. `Resources.ja-JP.resx`) — silently falls back to the default culture today.
- A key is used in XAML (`{lx:LocalizedValue SomeKey}`) that doesn't exist in *any* resx — this is the one that throws at runtime.

Add it to your app project:
```bash
dotnet add package Meteion.Toolkit.Localization.Check
```
That's it - it wires itself into your build via an MSBuild target and prints a warning for every issue it finds, before you ever hit F5:
```
Resources.ja-JP.resx: warning LOC001: Key 'ScopeID' is defined in 'Resources.resx' but is missing from the 'ja-JP' locale.
View.xaml(12): warning LOC003: Key 'SomeTypo' is used here but is not defined in any scanned .resx file and will throw or fail to resolve at runtime.
```

You are also able to use the checker's API directly via the `PackageReference`, enabling you to check resources on startup or in unit tests:
```csharp
var result = Meteion.Toolkit.Localization.Check.LocalizationKeyChecker.CheckDirectory("path/to/project");
```

**Known limitations**: the XAML check only understands literal `Key="..."` usages (a `KeyBinding`-sourced dynamic key can't be checked statically, by design) and assumes resources live in the same project being scanned — it doesn't resolve a `LocalizedValueExtension.Assembly` override that points at a different assembly.

If you'd rather not ship the checker's own assemblies inside your app at all (they're a build-time tool, not a runtime dependency), reference it as:

```
<PackageReference Include="Meteion.Toolkit.Localization.Check" PrivateAssets="all" IncludeAssets="build;buildTransitive" />
```

Then the automatic pre-build check still runs, you just lose the ability to call the API directly from that project.
