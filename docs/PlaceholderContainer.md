# PlaceholderContainer

`PlaceholderContainer` shows your content while a value holds something, and a placeholder of your
choosing when it doesn't. It covers two common cases with one control: a list with no items, and a
details pane with nothing selected. The content is hidden (not just covered) while the placeholder
shows.

- `Meteion.Toolkit.WPF.Controls.PlaceholderContainer` — the control (in `Meteion.Toolkit.WPF`).

## Quick start

```xaml
<!-- Empty list -->
<mtk:PlaceholderContainer Value="{Binding Orders}">
    <mtk:PlaceholderContainer.PlaceholderTemplate>
        <DataTemplate>
            <TextBlock Text="No orders yet" HorizontalAlignment="Center" VerticalAlignment="Center" />
        </DataTemplate>
    </mtk:PlaceholderContainer.PlaceholderTemplate>
    <DataGrid ItemsSource="{Binding Orders}" />
</mtk:PlaceholderContainer>

<!-- Nothing selected -->
<mtk:PlaceholderContainer Value="{Binding SelectedOrder}">
    <mtk:PlaceholderContainer.PlaceholderTemplate>
        <DataTemplate>
            <TextBlock Text="Select an order" HorizontalAlignment="Center" VerticalAlignment="Center" />
        </DataTemplate>
    </mtk:PlaceholderContainer.PlaceholderTemplate>
    <OrderDetailsView DataContext="{Binding SelectedOrder}" />
</mtk:PlaceholderContainer>
```

`mtk` is `xmlns:mtk="http://wpf.meteion.ca/winfx/xaml"`.

## What counts as empty

`Value` is empty, so the placeholder shows, when it is:

- `null`
- an empty `string`
- an empty collection or sequence (`ICollection`, or any `IEnumerable` with no items)

Anything else is non-empty, including `0`, `false` and ordinary objects. To drive the placeholder from
a different rule, bind `Value` to a property on your view model that is `null` when the placeholder
should show.

## Binding a boolean

When the rule isn't "null or empty", bind `ShowPlaceholder` instead. It overrides `Value`:

| `ShowPlaceholder` | Result |
|---|---|
| `null` (default) | `Value` decides, as above |
| `true` | The placeholder always shows |
| `false` | The content always shows |

```xaml
<mtk:PlaceholderContainer ShowPlaceholder="{Binding HasNoResults}" PlaceholderTemplate="{StaticResource NoResults}">
    <ResultsView />
</mtk:PlaceholderContainer>
```

You can still set `Value` alongside it to give the template a data context. While `ShowPlaceholder`
is `true` with no `Value`, the template's `DataContext` is the empty placeholder object (see below).

## Live updates

If `Value` implements `INotifyCollectionChanged` (for example `ObservableCollection<T>`), adding the
first item or removing the last one switches between the content and the placeholder immediately. A
plain `List<T>` or array is evaluated only when `Value` is set; replace the reference to refresh it.

The container listens only while it is loaded and unsubscribes on unload, so a long-lived collection
doesn't keep a closed view alive.

## PlaceholderTemplate

| Property | Purpose |
|---|---|
| `Value` | The object under test. |
| `ShowPlaceholder` | `bool?` override of `Value`: `true` shows the placeholder, `false` shows the content, `null` defers to `Value`. |
| `PlaceholderTemplate` | `DataTemplate` for the placeholder. `DataContext` is `Value`. |
| `IsPlaceholderVisible` | Read-only. `true` while the placeholder replaces the content, from either `Value` or `ShowPlaceholder`. Useful for triggers or bindings elsewhere. |

- With no `PlaceholderTemplate`, the content is hidden and nothing is shown in its place.
- When `Value` is `null`, the template's `DataContext` is an empty placeholder object, so a template
  for that case should not bind to it. For an empty collection or string, it is that collection or string.
- There is deliberately no `PlaceholderContent` property: use a template, which is only built while it is
  needed.

## Nesting with StatefulContainer

Put `StatefulContainer` outside so loading and errors take precedence, and the "no items" message never
flashes while data is still on its way:

```xaml
<mtk:StatefulContainer State="{Binding State}" RetryCommand="{Binding ReloadCommand}">
    <mtk:PlaceholderContainer Value="{Binding Orders}" PlaceholderTemplate="{StaticResource NoOrders}">
        <DataGrid ItemsSource="{Binding Orders}" />
    </mtk:PlaceholderContainer>
</mtk:StatefulContainer>
```

## Customizing

| Property | Purpose |
|---|---|
| `Style` / `Template` | Replace the whole control template. Keep `PART_Content` and `PART_PlaceholderHost` (a `ContentPresenter` with `ContentSource=""`). |

To share a placeholder across the app, set it in an implicit style. Keep `BasedOn`, or the control
loses its template:

```xaml
<Style TargetType="mtk:PlaceholderContainer"
       BasedOn="{StaticResource {x:Type mtk:PlaceholderContainer}}">
    <Setter Property="PlaceholderTemplate" Value="{StaticResource App.NothingHereTemplate}" />
</Style>
```

## Performance notes

- The content stays alive while collapsed, so scroll position and selection survive.
- The placeholder visual is created when it becomes active and released when it goes away.
- Visibility is toggled directly in code; there is no `VisualStateManager` or per-frame layout work.
