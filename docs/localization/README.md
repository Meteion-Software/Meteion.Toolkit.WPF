# Localization

Localization support for WPF applications using the Meteion Toolkit. It includes resources and utilities to
facilitate the translation of UI elements and messages into different languages.

It stores localization resources in RESX files in your assembly by default.

## Benefits

### Over {x:Static resources:Resources.blah}
- Can change at runtime
- Can be easily displayed in the Designer
- Can handle non-dependency properties

## Guides

| Topic | What it covers |
|---|---|
| [Usage](Usage.md) | `{lx:LocalizedValue}`: literal keys, generated constants, `KeyBinding`, `KeyPrefix`, `Source`. |
| [LocalizedBinding](LocalizedBinding.md) | `{lx:LocalizedBinding}`: one-way localized binding for DataGrid columns and other `BindingBase` properties. |
| [Multiple resx files](MultipleResxFiles.md) | Splitting strings across several `.resx` files per assembly. |
| [Changing the current culture at runtime](ChangingCulture.md) | Switching language independently of the OS locale. |
| [Culture-aware formatting](CultureAwareFormatting.md) | `{lx:CultureAwareFormat}` for numbers, dates and currency. |
| [Conventions and recommendations](Conventions.md) | Naming conventions and tooling suggestions. |
| [Key autocompletion](KeyAutocompletion.md) | `Meteion.Toolkit.Localization.KeysGenerator` strongly-typed keys. |
| [Localization key checking](KeyChecking.md) | `Meteion.Toolkit.Localization.Check` build-time checks. |
