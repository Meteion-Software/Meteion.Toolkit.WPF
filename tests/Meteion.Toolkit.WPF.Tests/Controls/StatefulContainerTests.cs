using Meteion.Toolkit.WPF.Controls;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Meteion.Toolkit.WPF.Tests.Controls;

/// <summary>
/// Controls need an STA thread. The container is never shown in a window: its template is applied directly and
/// the Loaded/Unloaded events (which drive the PropertyChanged subscription) are raised by hand.
/// </summary>
public class StatefulContainerTests
{
    private sealed class FakeViewState : IViewState
    {
        private ViewStatus _status = ViewStatus.Loaded;
        private PropertyChangedEventHandler? _changed;

        public int SubscriberCount { get; private set; }

        public ViewStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                _changed?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            }
        }

        public string? ErrorMessage { get; set; }

        public Exception? Exception { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged
        {
            add { _changed += value; SubscriberCount++; }
            remove { _changed -= value; SubscriberCount--; }
        }
    }

    private sealed class FakeCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged { add { } remove { } }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter)
        {
        }
    }

    private static StatefulContainer Create(IViewState? state, StatefulContainerMode mode = StatefulContainerMode.Replace)
    {
        var container = new StatefulContainer { Content = new TextBlock { Text = "content" }, State = state, Mode = mode };
        container.ApplyTemplate();
        return container;
    }

    private static void RaiseLoaded(StatefulContainer container)
        => container.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

    private static void RaiseUnloaded(StatefulContainer container)
        => container.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

    private static UIElement Content(StatefulContainer c) => (UIElement)c.Template.FindName("PART_Content", c);

    private static Border Layer(StatefulContainer c) => (Border)c.Template.FindName("PART_StateLayer", c);

    private static ContentPresenter Host(StatefulContainer c) => (ContentPresenter)c.Template.FindName("PART_StateHost", c);

    private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            yield return child;
            foreach (var nested in Descendants(child))
            {
                yield return nested;
            }
        }
    }

    [StaFact]
    public void NullState_ShowsContentOnly()
    {
        var container = Create(null);

        Assert.Equal(Visibility.Visible, Content(container).Visibility);
        Assert.Equal(Visibility.Collapsed, Layer(container).Visibility);
        Assert.Null(Host(container).Content);
    }

    [StaFact]
    public void Loaded_ShowsContentAndRealizesNoStateVisual()
    {
        var container = Create(new FakeViewState { Status = ViewStatus.Loaded });

        Assert.Equal(Visibility.Visible, Content(container).Visibility);
        Assert.Equal(Visibility.Collapsed, Layer(container).Visibility);
        Assert.Null(Host(container).Content);
        Assert.Null(Host(container).ContentTemplate);
    }

    [StaFact]
    public void Loading_Replace_CollapsesContentAndShowsLoadingTemplate()
    {
        var state = new FakeViewState { Status = ViewStatus.Loading };
        var container = Create(state);

        Assert.NotNull(container.LoadingTemplate);
        Assert.Equal(Visibility.Collapsed, Content(container).Visibility);
        Assert.Equal(Visibility.Visible, Layer(container).Visibility);
        Assert.Null(Layer(container).Background);
        Assert.Same(container.LoadingTemplate, Host(container).ContentTemplate);
        Assert.Same(state, Host(container).Content);
    }

    [StaFact]
    public void Loading_Overlay_KeepsContentVisibleBehindDimLayer()
    {
        var container = Create(new FakeViewState { Status = ViewStatus.Loading }, StatefulContainerMode.Overlay);

        Assert.Equal(Visibility.Visible, Content(container).Visibility);
        Assert.Equal(Visibility.Visible, Layer(container).Visibility);
        Assert.Same(container.OverlayBackground, Layer(container).Background);
        Assert.Equal(KeyboardNavigationMode.None, KeyboardNavigation.GetTabNavigation(Content(container)));
    }

    [StaFact]
    public void Loading_Overlay_NullOverlayBackground_StillBlocksHitTesting()
    {
        var container = Create(new FakeViewState { Status = ViewStatus.Loading }, StatefulContainerMode.Overlay);
        container.OverlayBackground = null;

        Assert.Same(Brushes.Transparent, Layer(container).Background);
    }

    [StaFact]
    public void Error_ShowsErrorTemplateWithState()
    {
        var state = new FakeViewState { Status = ViewStatus.Error, ErrorMessage = "boom" };
        var container = Create(state);

        Assert.NotNull(container.ErrorTemplate);
        Assert.Equal(Visibility.Collapsed, Content(container).Visibility);
        Assert.Same(container.ErrorTemplate, Host(container).ContentTemplate);
        Assert.Same(state, Host(container).Content);
    }

    [StaFact]
    public void CustomTemplates_AreUsedInsteadOfDefaults()
    {
        var loading = new DataTemplate();
        var error = new DataTemplate();
        var state = new FakeViewState { Status = ViewStatus.Loading };
        var container = Create(state);
        RaiseLoaded(container);
        container.LoadingTemplate = loading;
        container.ErrorTemplate = error;

        Assert.Same(loading, Host(container).ContentTemplate);

        state.Status = ViewStatus.Error;

        Assert.Same(error, Host(container).ContentTemplate);
    }

    [StaFact]
    public void StateChange_AfterLoaded_UpdatesVisuals_AndReleasesStateVisualWhenLoaded()
    {
        var state = new FakeViewState { Status = ViewStatus.Loaded };
        var container = Create(state);
        RaiseLoaded(container);

        state.Status = ViewStatus.Loading;
        Assert.Equal(Visibility.Collapsed, Content(container).Visibility);
        Assert.NotNull(Host(container).ContentTemplate);

        state.Status = ViewStatus.Loaded;
        Assert.Equal(Visibility.Visible, Content(container).Visibility);
        Assert.Null(Host(container).Content);
        Assert.Null(Host(container).ContentTemplate);
    }

    [StaFact]
    public void StateSet_BeforeLoaded_DoesNotSubscribe()
    {
        var state = new FakeViewState();
        Create(state);

        Assert.Equal(0, state.SubscriberCount);
    }

    [StaFact]
    public void Unloaded_UnsubscribesFromState_AndLoadedResubscribesAndRefreshes()
    {
        var state = new FakeViewState { Status = ViewStatus.Loaded };
        var container = Create(state);

        RaiseLoaded(container);
        Assert.Equal(1, state.SubscriberCount);

        RaiseUnloaded(container);
        Assert.Equal(0, state.SubscriberCount);

        state.Status = ViewStatus.Loading;
        Assert.Equal(Visibility.Visible, Content(container).Visibility);

        RaiseLoaded(container);
        Assert.Equal(1, state.SubscriberCount);
        Assert.Equal(Visibility.Collapsed, Content(container).Visibility);
    }

    [StaFact]
    public void ChangingState_DropsOldSubscription_AndRefreshesFromNewState()
    {
        var first = new FakeViewState();
        var second = new FakeViewState { Status = ViewStatus.Loading };
        var container = Create(first);
        RaiseLoaded(container);
        Assert.Equal(1, first.SubscriberCount);

        container.State = second;

        // A hand-raised Loaded does not set IsLoaded, so the swap itself does not re-subscribe.
        Assert.Equal(0, first.SubscriberCount);
        Assert.Equal(Visibility.Collapsed, Content(container).Visibility);
        RaiseLoaded(container);
        Assert.Equal(1, second.SubscriberCount);
    }

    [StaFact]
    public void ModeChange_UpdatesVisibility()
    {
        var container = Create(new FakeViewState { Status = ViewStatus.Loading });
        Assert.Equal(Visibility.Collapsed, Content(container).Visibility);

        container.Mode = StatefulContainerMode.Overlay;

        Assert.Equal(Visibility.Visible, Content(container).Visibility);
    }

    private static Window Show(StatefulContainer container)
    {
        var window = new Window { Content = container, Width = 400, Height = 300, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000 };
        window.Show();

        // Loaded (and template-realized bindings) settle on the dispatcher, not synchronously.
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
        return window;
    }

    [StaFact]
    public void DefaultErrorTemplate_RetryButton_VisibleOnlyWhenRetryCommandSet()
    {
        var container = new StatefulContainer { State = new FakeViewState { Status = ViewStatus.Error, ErrorMessage = "boom" } };
        var window = Show(container);
        try
        {
            var button = Descendants(Host(container)).OfType<Button>().Single();
            Assert.Equal(Visibility.Collapsed, button.Visibility);

            container.RetryCommand = new FakeCommand();

            Assert.Equal(Visibility.Visible, button.Visibility);
            Assert.Equal("Retry", button.Content);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void DefaultErrorTemplate_ShowsErrorMessageText()
    {
        var container = new StatefulContainer { State = new FakeViewState { Status = ViewStatus.Error, ErrorMessage = "boom" } };
        var window = Show(container);
        try
        {
            var texts = Descendants(Host(container)).OfType<TextBlock>().Select(t => t.Text);

            Assert.Contains("boom", texts);
        }
        finally
        {
            window.Close();
        }
    }
}
