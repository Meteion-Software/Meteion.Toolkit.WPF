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
checking for `INavigationProgress` on the resolved instance. Drive a [`StatefulContainer`](StatefulContainer.md)
in `Overlay` mode from them with a `ViewState` owned by the window:

```xaml
<Grid xmlns:meteion="http://wpf.meteion.ca/winfx/xaml">
    <meteion:StatefulContainer Mode="Overlay" State="{Binding NavigationState, RelativeSource={RelativeSource AncestorType=Window}}">
        <Frame x:Name="ShellFrame" NavigationUIVisibility="Hidden" />
    </meteion:StatefulContainer>
</Grid>
```

```cs
public ViewState NavigationState { get; } = CreateLoadedState();

public MainWindow(INavigationService navService)
{
    InitializeComponent();
    navService.Initialize(ShellFrame);

    if (navService is INavigationProgress navProgress)
    {
        navProgress.NavigationStarted += (_, _) => NavigationState.SetLoading();
        navProgress.NavigationCompleted += (_, _) => NavigationState.SetLoaded();
    }
}

private static ViewState CreateLoadedState()
{
    var state = new ViewState();   // starts as Loading
    state.SetLoaded();
    return state;
}
```

Adjust the delay if you want the indicator to appear sooner/later:

```cs
navProgress.NavigationIndicatorDelay = TimeSpan.FromMilliseconds(400);
```

## Migrating from BusyOverlay

`BusyOverlay` has been removed in favour of [`StatefulContainer`](StatefulContainer.md), which does
the same dim-and-spinner job in `Overlay` mode and also covers loading/error states for ordinary
content. Replace `<meteion:BusyOverlay x:Name="..."/>` (placed next to the content) with a
`StatefulContainer` wrapping that content, and replace `overlay.IsBusy = true/false` with
`state.SetLoading()` / `state.SetLoaded()`.

You don't have to use it — `NavigationStarted`/`NavigationCompleted` are enough to drive any
indicator (your own control, a status bar message, disabling input, etc.).
