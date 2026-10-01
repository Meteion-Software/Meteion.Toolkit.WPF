# Naming Conventions
Each resx "family" is a neutral resx plus its per-language satellites. Follow the standard .NET convention:

- **Neutral/default resx**: `Strings.resx` (no culture suffix). This is the one embedded directly in the assembly and used as the fallback when no more specific culture matches.
- **One resx per additional language**: `Strings.{culture}.resx` (e.g. `Strings.en-CA.resx`, `Strings.ja-JP.resx`). MSBuild recognizes the shared `Strings` base name plus a valid culture suffix and compiles each of these into its own **satellite assembly** automatically — `ResourceManager` finds them at runtime by walking the culture's fallback chain.

Prefer several focused families - one per feature folder, e.g. `Views/Home/HomeStrings.resx` and
`Views/Settings/SettingsStrings.resx` - over a single family with long underscore-separated key prefixes.
See [Multiple resx files](MultipleResxFiles.md).

# Recommendations

The [ResxManager](https://marketplace.visualstudio.com/items?itemName=TomEnglert.ResXManager) is fantastic!
