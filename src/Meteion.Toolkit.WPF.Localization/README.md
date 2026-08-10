# Meteion.Toolkit.WPF.Localization

This project provides localization support for WPF applications using the Meteion Toolkit. It includes resources and utilities to facilitate the translation of UI elements and messages into different languages.

It stores localization resources in RESX files in your assembly by default.

## Benefits

### Over {x:Static resources:Resources.blah}
- Can change at runtime
- Can be easily displayed in the Designer
- Can handle non-dependency properties

## Naming Conventions
This library expects **one resx "family" per assembly** — `ILocalizationProvider` only receives an `Assembly`, not a file/dictionary name, so all your localized strings for a given assembly need to live under a single shared base name.

Follow the standard .NET convention:

- **Neutral/default resx**: `Resources.resx` (no culture suffix). This is the one embedded directly in the assembly and used as the fallback when no more specific culture matches.
- **One resx per additional language**: `Resources.{culture}.resx` (e.g. `Resources.en-CA.resx`, `Resources.jp.resx`). MSBuild recognizes the shared `Resources` base name plus a valid culture suffix and compiles each of these into its own **satellite assembly** automatically — `ResourceManager` finds them at runtime by walking the culture's fallback chain.

## Recommendations

The [ResxManager](https://marketplace.visualstudio.com/items?itemName=TomEnglert.ResXManager) is fantastic!