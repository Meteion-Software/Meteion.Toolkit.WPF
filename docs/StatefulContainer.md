# StatefulContainer

`StatefulContainer` shows one of three things depending on a state object: your content (loaded), a
loading indicator, or an error panel with an optional Retry button. It comes with default visuals
for loading and error, and every part is overridable.

- `Meteion.Toolkit.WPF.Controls.StatefulContainer` — the control (in `Meteion.Toolkit.WPF`).
- `Meteion.Toolkit.MVVM.ViewState` and `IStatefulViewModel` — the view-model side (in
  `Meteion.Toolkit.WPF.MVVM`).

## Quick start

```cs
public sealed class OrdersViewModel : IStatefulViewModel, IAsyncNavigationAwareViewModel
{
    public ViewState State { get; } = new();
    public ICommand ReloadCommand { get; }

    public OrdersViewModel(IOrderApi api, ILogger<OrdersViewModel> log)
    {
        ReloadCommand = new AsyncRelayCommand(Load);
    }

    public Task OnNavigatedToAsync(object? parameter) => Load();

    public Task OnNavigatedFromAsync()
    {
        State.Cancel();
        return Task.CompletedTask;
    }

    private Task Load() => State.RunAsync(
        async ct => Orders = await _api.GetOrdersAsync(ct),
        ex =>
        {
            _log.LogError(ex, "Failed to load orders");
            return "Couldn't load your orders.";
        });
}
```

```xaml
<mtk:StatefulContainer State="{Binding State}" RetryCommand="{Binding ReloadCommand}">
    <DataGrid ItemsSource="{Binding Orders}" />
</mtk:StatefulContainer>
```

`mtk` is `xmlns:mtk="http://wpf.meteion.ca/winfx/xaml"`.

## ViewState

`ViewState` holds `Status` (`Loading`, `Loaded` or `Error`), `ErrorMessage` and `Exception`, and raises
`PropertyChanged` only for the properties that actually changed. It starts as `Loading`, so nothing
flashes before the first load begins. Create and use it from the UI thread.

### RunAsync

```cs
Task RunAsync(Func<CancellationToken, Task> work, string? errorMessage = null);
Task RunAsync(Func<CancellationToken, Task> work, Func<Exception, string?> onException);
```

1. Cancels any run still in flight and shows `Loading`.
2. Awaits `work`. On success the state becomes `Loaded`.
3. If `work` throws, the state becomes `Error`; the exception is **not** rethrown.
   - String overload: shows `errorMessage`, or the exception's own message when it is `null`.
   - `onException` overload: called with the exception (log it, translate it, ...); the string it
     returns is shown. Returning `null`, or throwing, falls back to the exception's message.
   - `ViewState.Exception` always holds the exception for custom templates; the default template
     only shows the message, never a stack trace.
4. A run that was cancelled (by a newer run, `Cancel()` or a manual setter) never changes the state,
   so a slow stale request can't overwrite a newer result.

Pass the token to your async calls so cancellation actually stops the work.

### Manual state

For flows without a single awaitable (for example, driving the indicator from events), set the state
directly. Each of these also cancels any run in flight:

```cs
state.SetLoading();
state.SetLoaded();
state.SetError("Something broke", exception); // message null => exception.Message
```

`Cancel()` only cancels the run; it leaves the state as it is.

### IStatefulViewModel

A one-property marker interface (`ViewState State { get; }`) for view models that expose a state.
The control itself binds to `IViewState`, so a plain `ViewState` property works with or without it.

## Modes

| `Mode` | Behaviour |
|---|---|
| `Replace` (default) | While loading or in error, the content is collapsed and the state visual is shown instead. The content stays alive, so scroll position, selection, etc. survive a reload. |
| `Overlay` | The content stays visible, covered by a dimmed layer (`OverlayBackground`, default `#80000000`) holding the spinner or error panel. The layer blocks mouse input and tabbing into the content. |

Use `Overlay` for refreshing in place or for a shell like a navigation `Frame`:

```xaml
<mtk:StatefulContainer Mode="Overlay" State="{Binding NavigationState}">
    <Frame x:Name="ShellFrame" NavigationUIVisibility="Hidden" />
</mtk:StatefulContainer>
```

Keyboard focus that is already inside the content when the overlay appears is not moved.

## Retry

The Retry button is opt-in: it appears in the default error panel only when `RetryCommand` is set
(`RetryCommandParameter` is passed along; `CanExecute` is respected). The container does not know how
to reload — point the command at your view model's load method.

```xaml
<mtk:StatefulContainer
    State="{Binding State}"
    RetryCommand="{Binding ReloadCommand}"
    RetryContent="Try again"
    ErrorTitle="We couldn't load this" />
```

## Customizing

| Property | Purpose |
|---|---|
| `LoadingTemplate` | `DataTemplate` for the loading visual. `DataContext` is the `IViewState`. |
| `ErrorTemplate` | `DataTemplate` for the error visual. `DataContext` is the `IViewState` (`ErrorMessage`, `Exception`). |
| `RetryContent`, `ErrorTitle` | Button content / heading of the default error panel (defaults are English literals). |
| `OverlayBackground` | The dim layer in `Overlay` mode. |
| `Style` / `Template` | Replace the whole control template. Keep `PART_Content`, `PART_StateLayer` (a `Border`) and `PART_StateHost` (a `ContentPresenter` with `ContentSource=""`). |

To use a custom error panel with your own retry button, bind to the state and your view model:

```xaml
<mtk:StatefulContainer State="{Binding State}">
    <mtk:StatefulContainer.ErrorTemplate>
        <DataTemplate>
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                <TextBlock Text="{Binding ErrorMessage}" />
                <Button Command="{Binding DataContext.ReloadCommand, RelativeSource={RelativeSource AncestorType=Page}}"
                        Content="Reload" />
            </StackPanel>
        </DataTemplate>
    </mtk:StatefulContainer.ErrorTemplate>
    ...
</mtk:StatefulContainer>
```

## Changing the templates application-wide

The default loading and error visuals are applied by the control's default style, so to change them
everywhere, override that style once in `App.xaml` rather than setting `LoadingTemplate` /
`ErrorTemplate` on every container. Declare an implicit style (no `x:Key`) based on the default one:

```xaml
<Application xmlns:mtk="http://wpf.meteion.ca/winfx/xaml" ...>
    <Application.Resources>
        <DataTemplate x:Key="App.LoadingTemplate">
            <ProgressBar IsIndeterminate="True" Width="200" Height="6"
                         HorizontalAlignment="Center" VerticalAlignment="Center" />
        </DataTemplate>

        <DataTemplate x:Key="App.ErrorTemplate">
            <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                <TextBlock Text="{Binding ErrorMessage}" TextWrapping="Wrap" />
                <Button Content="{Binding RetryContent, RelativeSource={RelativeSource AncestorType=mtk:StatefulContainer}}"
                        Command="{Binding RetryCommand, RelativeSource={RelativeSource AncestorType=mtk:StatefulContainer}}"
                        CommandParameter="{Binding RetryCommandParameter, RelativeSource={RelativeSource AncestorType=mtk:StatefulContainer}}" />
            </StackPanel>
        </DataTemplate>

        <Style TargetType="mtk:StatefulContainer"
               BasedOn="{StaticResource {x:Type mtk:StatefulContainer}}">
            <Setter Property="LoadingTemplate" Value="{StaticResource App.LoadingTemplate}" />
            <Setter Property="ErrorTemplate" Value="{StaticResource App.ErrorTemplate}" />
        </Style>
    </Application.Resources>
</Application>
```

Things to know:

- **Keep `BasedOn`.** An implicit style replaces the toolkit's default style entirely. Without
  `BasedOn`, the control loses its template (`PART_Content`, `PART_StateLayer`, `PART_StateHost`) and
  renders nothing.
- You can set just one of the two; the other keeps the toolkit default.
- Inside the templates, `DataContext` is the `IViewState`, so bind `ErrorMessage`, `Exception` and
  `Status` directly. Properties of the container itself (`RetryCommand`, `RetryContent`, `ErrorTitle`, ...)
  are reached with `RelativeSource AncestorType=mtk:StatefulContainer`, as above, which keeps the
  per-container Retry wiring working with your app-wide template.
- A container that sets `LoadingTemplate` / `ErrorTemplate` locally still wins over the app-wide style,
  so individual screens can opt out.
- To scope the change to part of the app, put the same style in a window's, page's or panel's
  `Resources` instead of `App.xaml`.
- To change a whole look (including the overlay layer), restyle with a custom `Template` in the same
  style; see the part names under [Customizing](#customizing).

## Performance notes

- Only the active state's visual exists: the loading and error templates are created on entry and
  released on exit, so the spinner animation runs only while loading and nothing is built for states
  never reached.
- The container listens to `PropertyChanged` only while it is loaded and unsubscribes on unload, so a
  long-lived view model doesn't keep a closed view alive.
- Visibility is toggled directly in code; there is no `VisualStateManager` or per-frame layout work.

## Replacing BusyOverlay

`BusyOverlay` (and its `IsBusy` property) has been removed. Use `StatefulContainer` with
`Mode="Overlay"` and a `ViewState`; see [Navigation Loading Indicator](NavigationLoadingIndicator.md)
for the navigation example.
