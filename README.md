# Meteion WPF Toolkit
This toolkit is created for WPF developers like us to help them rapidly build WPF applications by reducing boilerplate.

## Getting started
See the [Getting Started](docs/getting-started.md) guide for instructions on how to install and use the toolkit.

## Features
- **MVVM Support**: Provides base classes and utilities to implement the MVVM pattern effectively.
- **GenericHost Support**: Integrates with .NET Generic Host for dependency injection and configuration.
- **Navigation Management**: Simplifies navigation for views and view models.
- **Navigation Loading Indicator**: Opt-in events (`INavigationProgress`) and a `BusyOverlay` control for showing a spinner during slow navigations, with built-in flicker protection. See [Navigation Loading Indicator](docs/NavigationLoadingIndicator.md).
- **Splash Screen**: An optional package that shows a native, per-pixel-alpha splash screen (with optional progress bar and status text) before WPF starts, and hands off to your launch window. See [Splash Screen](docs/SplashScreen.md).
- **Localization Key Checking**: Catches missing/undefined localization keys before you run the app. See [Localization key checking](docs/localization/KeyChecking.md).
- **LocalizedBinding**: `{lx:LocalizedBinding}` gives a one-way localized binding for DataGrid columns and other `BindingBase` properties. See [LocalizedBinding](docs/localization/LocalizedBinding.md).
- **Multiple resx Files**: Split localized strings across any number of `.resx` files per assembly (e.g. one per feature), using generated keys that name their own resx. See [Multiple resx files](docs/localization/MultipleResxFiles.md).
- **Localization Key Autocompletion**: Generates a strongly-typed class from your `.resx` keys for autocompletion in code-behind and XAML. See [Key autocompletion](docs/localization/KeyAutocompletion.md).
- **Runtime Culture Switching**: Change the app's language independently of the OS locale — keeps `Thread.CurrentCulture`, resx lookups, and `FrameworkElement.Language` all in sync. See [Changing the current culture at runtime](docs/localization/ChangingCulture.md).
- **Culture-Aware Formatting**: `{lx:CultureAwareFormat}` formats bound numbers/dates/currency using the app's current culture, live. See [Culture-aware number/date/currency formatting](docs/localization/CultureAwareFormatting.md).

## Localization
See the [Localization docs](docs/localization/README.md) for usage and tooling guides.

## Recommendations
We recommend using this alongside CommunityToolkit.Mvvm for a complete MVVM experience.

## FAQ
Q: Why use Meteion.Toolkit.WPF.MVVM and .Hosting instead of CommunityToolkit.IoC?

A: The CommunityToolkit.IoC is a simple IoC container that is not designed for complex scenarios. Meteion.Toolkit.WPF.MVVM and .Hosting provide a more robust solution for dependency injection and application hosting in WPF applications. It really helps makes your application feel like WPF was designed with Dependency Injection in mind. It also supplies some opinionated defaults, or can be adapted to your use case.


Q: Why use Meteion.Toolkit.Localization instead of ``{x:Static }`` or ``Properties.Resources.Blah``?

A: The localization library includes a lot of helpful features, such as binding keys, changing the language at runtime (with `Thread.CurrentCulture` and `FrameworkElement.Language` kept in sync automatically), and culture-aware number/date/currency formatting that updates live when the language changes.
## TODO

Some short-term goals are:

- Make the sample program better
- Add support for prefixes when using KeyBinding in XAML
- Fix binding failures not appearing in the Binding Failure window when using KeyBinding
