# Meteion WPF Toolkit
This toolkit is created for WPF developers like us to help them rapidly build WPF applications by reducing boilerplate.

## Getting started
See the [Getting Started](docs/GettingStarted.md) guide for instructions on how to install and use the toolkit.

## Features
The toolkit is split into packages so you only take what you use. That means you don't have to pull the Hosting library in to show a splash screen.

### Controls

- **StatefulContainer**: A control for showing a loading indicator, your content, or an error state (with optional Retry), with overridable templates and a Replace or Overlay mode. Drive it from any state object, or use `ViewState`/`IStatefulViewModel` from the MVVM package. See [StatefulContainer](docs/StatefulContainer.md).
- **PlaceholderContainer**: A control that hides its content and shows a placeholder template when a bound value is null or empty. Use it for "no items" lists and "nothing selected" panes. It updates live with `ObservableCollection`. See [PlaceholderContainer](docs/PlaceholderContainer.md).

### GenericHost Hosting
_Requires: `Meteion.Toolkit.WPF.Hosting`_

Integrates with .NET Generic Host for dependency injection and configuration in your WPF applications.

### Splash Screen
_Requires: `Meteion.Toolkit.WPF.SplashScreen`_

Shows a native splash screen before WPF starts, and hands off to your launch window. See [Splash Screen](docs/SplashScreen.md).

Unlike the default WPF splash screen, this one can show progress and status text. 

### MVVM and Navigation Helpers
_Requires: `Meteion.Toolkit.WPF.MVVM`_

It's best used in tandem with CommunityToolkit.Mvvm, but it can be used standalone as well.

- **Navigation Management**: Simplifies navigation for views and view models.
- **Navigation Loading Indicator**: Opt-in events (`INavigationProgress`) for showing a spinner (via `StatefulContainer`) during slow navigations, with built-in flicker protection. See [Navigation Loading Indicator](docs/NavigationLoadingIndicator.md).
- **ViewState / IStatefulViewModel**: One MVVM state object that feeds `StatefulContainer`. See [StatefulContainer](docs/StatefulContainer.md).
- **INavigationAwareViewModel / IAsyncNavigationAwareViewModel** - Interfaces for view models that want to be notified when they are navigated to or from. 

Mostly, it just supports you creating ViewModels and Views that can be navigated to, and provides a way to show a loading indicator during slow navigations.

### Converters
_Requires: `Meteion.Toolkit.WPF`_

Common value converters in the `Meteion.Toolkit.WPF.Converters` namespace. Where a converter exposes a static `Instance`, you can use it straight from XAML without declaring a resource:

```xml
xmlns:conv="clr-namespace:Meteion.Toolkit.WPF.Converters;assembly=Meteion.Toolkit.WPF"

<TextBlock Visibility="{Binding IsBusy, Converter={x:Static conv:BooleanToVisibilityConverter.Instance}}" />
```

Converters return `DependencyProperty.UnsetValue` when the input isn't something they can convert.

| Converter | Converts | Notes |
| --- | --- | --- |
| `BooleanToVisibilityConverter` | `bool` → `Visibility` | `true` is `Visible`, `false` is `Collapsed`. |
| `InverseBooleanToVisibilityConverter` | `bool` → `Visibility` | `false` is `Visible`, `true` is `Collapsed`. |
| `InverseBooleanConverter` | `bool` → `bool` | Negates the value. |
| `VisibleIfNullConverter` | `object` → `Visibility` | `Visible` when the value is `null`, otherwise `Collapsed`. |
| `VisibleIfNotNullConverter` | `object` → `Visibility` | `Visible` when the value is not `null`, otherwise `Collapsed`. |
| `VisibleIfNullOrEmptyConverter` | `string`/collection → `Visibility` | `Visible` when `null`, an empty string, or an empty collection (handy for "no results" messages). |
| `VisibleIfNotNullOrEmptyConverter` | `string`/collection → `Visibility` | `Visible` for a non-empty string or collection; any other non-null object counts as `Visible`. |
| `ToUpperConverter` / `ToLowerConverter` | `string` → `string` | Changes the casing of the text. |
| `DateOnlyDateTimeConverter` | `DateOnly` ⇄ `DateTime` | Works both ways, e.g. for binding a `DateOnly` to a `DatePicker`. |
| `BackgroundToForegroundConverter` | `SolidColorBrush` → `SolidColorBrush` | Picks black or white text for readability on the given background. Also usable as a `MultiBinding` converter. |

### Localization
See the [Localization docs](docs/localization/README.md) for usage and tooling guides.

The Localization library allows for language switching at runtime (unlike the default WPF one). It also helps with binding through
`{lx:LocalizedValue KeyBinding=}` for most instances, and `{lx:LocalizedBinding}` for one-way localized bindings for things like DataGrid columns and other `BindingBase` properties. See [Localization](docs/localization/README.md) to get started.

Multiple resx files are now supported, using generated keys that name their own resx. See [Multiple resx files](docs/localization/MultipleResxFiles.md).

It also supports runtime culture switching, change the app's language independently of the OS locale, keeping `Thread.CurrentCulture`, resx lookups, and `FrameworkElement.Language` all in sync. See [Changing the current culture at runtime](docs/localization/ChangingCulture.md).

`{lx:CultureAwareFormat}` formats bound numbers/dates/currency using the app's current culture, live. See [Culture-aware number/date/currency formatting](docs/localization/CultureAwareFormatting.md).

#### Localization key checking (build-time, optional)

- **Localization Key Checking**: Catches missing/undefined localization keys before you run the app. No runtime dependency and no other toolkit package required. See [Localization key checking](docs/localization/KeyChecking.md).

#### Localization key autocompletion (build-time, optional)

- **Localization Key Autocompletion**: Generates a strongly-typed class from your `.resx` keys for autocompletion in code-behind and XAML. A build-time analyzer only; the generated keys are plain constants. See [Key autocompletion](docs/localization/KeyAutocompletion.md).

Very very useful because you can `{x:Static}` your keys from XAML, helping reduce errors.

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
- Write some better docs
- Separate core localization logic into it's own independent package