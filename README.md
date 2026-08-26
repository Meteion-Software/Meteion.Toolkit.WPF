# Meteion WPF Toolkit
This toolkit is created for WPF developers like us to help them rapidly build WPF applications by reducing boilerplate.

## Getting started
See the [Getting Started](docs/getting-started.md) guide for instructions on how to install and use the toolkit.

## Features
- **MVVM Support**: Provides base classes and utilities to implement the MVVM pattern effectively.
- **GenericHost Support**: Integrates with .NET Generic Host for dependency injection and configuration.
- **Navigation Management**: Simplifies navigation for views and view models.
- **Localization Key Checking**: Catches missing/undefined localization keys before you run the app. See [Localization key checking](#localization-key-checking) below.

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

## Recommendations
We recommend using this alongside CommunityToolkit.Mvvm for a complete MVVM experience.

## FAQ
Q: Why use Meteion.Toolkit.WPF.MVVM and .Hosting instead of CommunityToolkit.IoC?

A: The CommunityToolkit.IoC is a simple IoC container that is not designed for complex scenarios. Meteion.Toolkit.WPF.MVVM and .Hosting provide a more robust solution for dependency injection and application hosting in WPF applications. It really helps makes your application feel like WPF was designed with Dependency Injection in mind. It also supplies some opinionated defaults, or can be adapted to your use case.


Q: Why use Meteion.Toolkit.Localization instead of ``{x:Static }`` or ``Properties.Resources.Blah``?

A: The localization library includes a lot of helpful features, such as binding keys, and changing the language at runtime.
## TODO

Some short-term goals are:

- Add method for localizing exceptions
- ~~Create a tool/extension/something for Visual Studio to notify you if you are using resource keys that do not exist~~ Done, see [Localization key checking](#localization-key-checking) — though it only covers literal-key XAML usages and same-project resources, not a `LocalizedValueExtension.Assembly` override pointing elsewhere
- Make the sample program better
- Add support for prefixes when using KeyBinding in XAML
- Fix binding failures not appearing in the Binding Failure window when using KeyBinding
