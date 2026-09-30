# Navigation Loading Indicator

If a destination view model's `OnNavigatedToAsync` does real work (an API call, a slow query,
etc.), the page it belongs to is already on screen while that work runs — by default there's
nothing telling the user a load is in progress.

`NavigationService` also implements `INavigationProgress`, which raises events you can use to
drive a busy indicator during slow navigations. It's opt-in: kept as a separate interface (rather
than added to `INavigationService` directly) so existing `INavigationService` consumers and mocks
aren't broken by it.

## How it works

```cs
public interface INavigationProgress
{
    event EventHandler<NavigationProgressEventArgs>? NavigationStarted;
    event EventHandler<NavigationProgressEventArgs>? NavigationCompleted;
    TimeSpan NavigationIndicatorDelay { get; set; }
}
```

- `NavigationStarted` only fires if the navigation's post-navigation work (`OnNavigatedToAsync`,
  for both the outgoing and incoming view model) is still running after `NavigationIndicatorDelay`
  has elapsed (default 200ms). A fast navigation never raises either event, so you don't get a
  one-frame flash of a spinner on pages that load instantly.
- `NavigationCompleted` fires once that work finishes — but only for a navigation that raised
  `NavigationStarted`.
- Both forward navigation (`NavigateTo`) and `GoBack` participate.
- `NavigationProgressEventArgs` carries `ViewModelType`, `NavigationParameter` (`null` for
  `GoBack`), and a `Direction` (`Forward`/`Back`).

## Usage

`NavigationService` is registered in DI as `INavigationService`, so get to the progress events by
checking for `INavigationProgress` on the resolved instance:

```cs
public MainWindow(INavigationService navService)
{
    InitializeComponent();
    navService.Initialize(ShellFrame);

    if (navService is INavigationProgress navProgress)
    {
        navProgress.NavigationStarted += (_, _) => NavigationBusyOverlay.IsBusy = true;
        navProgress.NavigationCompleted += (_, _) => NavigationBusyOverlay.IsBusy = false;
    }
}
```

Adjust the delay if you want the indicator to appear sooner/later:

```cs
navProgress.NavigationIndicatorDelay = TimeSpan.FromMilliseconds(400);
```

## BusyOverlay

`Meteion.Toolkit.WPF.Controls.BusyOverlay` is a small `IsBusy`-driven control with a dimmed
background and an animated spinner, included for convenience — it's not tied to navigation in any
way, just a generic overlay. Place it in the same panel cell as the content it should cover:

```xaml
<Grid xmlns:meteion="http://wpf.meteion.ca/winfx/xaml">
    <Frame x:Name="ShellFrame" NavigationUIVisibility="Hidden" />
    <meteion:BusyOverlay x:Name="NavigationBusyOverlay" />
</Grid>
```

You don't have to use `BusyOverlay` — `NavigationStarted`/`NavigationCompleted` are enough to drive
any indicator (your own control, a status bar message, disabling input, etc.).
