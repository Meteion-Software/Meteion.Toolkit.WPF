using Meteion.Toolkit.WPF.Controls;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace Meteion.Toolkit.WPF.Tests.Controls;

/// <summary>
/// Controls need an STA thread. The container is never shown in a window unless a test needs the template
/// realized: its template is applied directly and Loaded/Unloaded (which drive the CollectionChanged
/// subscription) are raised by hand.
/// </summary>
public class PlaceholderContainerTests
{
    private sealed class CountingCollection : ObservableCollection<string>
    {
        private NotifyCollectionChangedEventHandler? _changed;

        public int SubscriberCount { get; private set; }

        public override event NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add { _changed += value; SubscriberCount++; }
            remove { _changed -= value; SubscriberCount--; }
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e) => _changed?.Invoke(this, e);
    }

    private static PlaceholderContainer Create(object? value, DataTemplate? template = null)
    {
        var container = new PlaceholderContainer
        {
            Content = new TextBlock { Text = "content" },
            Value = value,
            PlaceholderTemplate = template,
        };
        container.ApplyTemplate();
        return container;
    }

    private static void RaiseLoaded(PlaceholderContainer container)
        => container.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));

    private static void RaiseUnloaded(PlaceholderContainer container)
        => container.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));

    private static UIElement Content(PlaceholderContainer c) => (UIElement)c.Template.FindName("PART_Content", c);

    private static ContentPresenter Host(PlaceholderContainer c) => (ContentPresenter)c.Template.FindName("PART_PlaceholderHost", c);

    private static DataTemplate TextTemplate()
        => new() { VisualTree = new FrameworkElementFactory(typeof(TextBlock)) };

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
    public void NullValue_ShowsPlaceholderAndCollapsesContent()
    {
        var template = TextTemplate();
        var container = Create(null, template);

        Assert.True(container.IsPlaceholderVisible);
        Assert.Equal(Visibility.Collapsed, Content(container).Visibility);
        Assert.Equal(Visibility.Visible, Host(container).Visibility);
        Assert.Same(template, Host(container).ContentTemplate);
        Assert.NotNull(Host(container).Content);
    }

    [StaFact]
    public void EmptyString_ShowsPlaceholder()
    {
        var container = Create(string.Empty, TextTemplate());

        Assert.True(container.IsPlaceholderVisible);
        Assert.Equal(Visibility.Collapsed, Content(container).Visibility);
    }

    [StaFact]
    public void EmptyCollection_ShowsPlaceholder_WithValueAsContent()
    {
        var value = new ObservableCollection<string>();
        var container = Create(value, TextTemplate());

        Assert.Equal(Visibility.Collapsed, Content(container).Visibility);
        Assert.Same(value, Host(container).Content);
    }

    [StaFact]
    public void NonEmptyValue_ShowsContentAndRealizesNoPlaceholder()
    {
        var container = Create(new ObservableCollection<string> { "a" }, TextTemplate());

        Assert.False(container.IsPlaceholderVisible);
        Assert.Equal(Visibility.Visible, Content(container).Visibility);
        Assert.Equal(Visibility.Collapsed, Host(container).Visibility);
        Assert.Null(Host(container).Content);
        Assert.Null(Host(container).ContentTemplate);
    }

    [StaFact]
    public void NonCollectionObject_IsNotEmpty()
    {
        var container = Create(new object(), TextTemplate());

        Assert.False(container.IsPlaceholderVisible);
        Assert.Equal(Visibility.Visible, Content(container).Visibility);
    }

    [StaFact]
    public void ZeroNumber_IsNotEmpty()
    {
        var container = Create(0, TextTemplate());

        Assert.False(container.IsPlaceholderVisible);
    }

    [StaFact]
    public void EmptyValue_WithNoTemplate_HidesContentAndShowsNothing()
    {
        var container = Create(null);

        Assert.True(container.IsPlaceholderVisible);
        Assert.Equal(Visibility.Collapsed, Content(container).Visibility);
        Assert.Equal(Visibility.Collapsed, Host(container).Visibility);
        Assert.Null(Host(container).Content);
    }

    [StaFact]
    public void ValueChange_SwitchesBetweenContentAndPlaceholder()
    {
        var container = Create(null, TextTemplate());

        container.Value = "selected";
        Assert.Equal(Visibility.Visible, Content(container).Visibility);
        Assert.Null(Host(container).Content);
        Assert.Null(Host(container).ContentTemplate);

        container.Value = null;
        Assert.Equal(Visibility.Collapsed, Content(container).Visibility);
        Assert.NotNull(Host(container).ContentTemplate);
    }

    [StaFact]
    public void TemplateChange_WhileEmpty_SwapsPlaceholderVisual()
    {
        var first = TextTemplate();
        var second = TextTemplate();
        var container = Create(null, first);

        container.PlaceholderTemplate = second;

        Assert.Same(second, Host(container).ContentTemplate);
    }

    [StaFact]
    public void CollectionChange_AfterLoaded_UpdatesLive()
    {
        var items = new CountingCollection();
        var container = Create(items, TextTemplate());
        RaiseLoaded(container);

        items.Add("a");
        Assert.False(container.IsPlaceholderVisible);
        Assert.Equal(Visibility.Visible, Content(container).Visibility);
        Assert.Null(Host(container).ContentTemplate);

        items.Clear();
        Assert.True(container.IsPlaceholderVisible);
        Assert.Equal(Visibility.Collapsed, Content(container).Visibility);
        Assert.NotNull(Host(container).ContentTemplate);
    }

    [StaFact]
    public void ValueSet_BeforeLoaded_DoesNotSubscribe()
    {
        var items = new CountingCollection();
        Create(items);

        Assert.Equal(0, items.SubscriberCount);
    }

    [StaFact]
    public void Unloaded_Unsubscribes_AndLoadedResubscribesAndRefreshes()
    {
        var items = new CountingCollection();
        var container = Create(items, TextTemplate());

        RaiseLoaded(container);
        Assert.Equal(1, items.SubscriberCount);

        RaiseUnloaded(container);
        Assert.Equal(0, items.SubscriberCount);

        items.Add("a");
        Assert.Equal(Visibility.Collapsed, Content(container).Visibility);

        RaiseLoaded(container);
        Assert.Equal(1, items.SubscriberCount);
        Assert.Equal(Visibility.Visible, Content(container).Visibility);
    }

    [StaFact]
    public void ChangingValue_DropsOldSubscription()
    {
        var first = new CountingCollection();
        var second = new CountingCollection();
        var container = Create(first);
        RaiseLoaded(container);
        Assert.Equal(1, first.SubscriberCount);

        container.Value = second;

        // A hand-raised Loaded does not set IsLoaded, so the swap itself does not re-subscribe.
        Assert.Equal(0, first.SubscriberCount);
        RaiseLoaded(container);
        Assert.Equal(1, second.SubscriberCount);
    }

    [StaFact]
    public void ShowPlaceholderTrue_OverridesNonEmptyValue()
    {
        var container = Create("selected", TextTemplate());

        container.ShowPlaceholder = true;

        Assert.True(container.IsPlaceholderVisible);
        Assert.Equal(Visibility.Collapsed, Content(container).Visibility);
        Assert.Equal("selected", Host(container).Content);
    }

    [StaFact]
    public void ShowPlaceholderFalse_OverridesEmptyValue()
    {
        var container = Create(null, TextTemplate());

        container.ShowPlaceholder = false;

        Assert.False(container.IsPlaceholderVisible);
        Assert.Equal(Visibility.Visible, Content(container).Visibility);
        Assert.Null(Host(container).ContentTemplate);
    }

    [StaFact]
    public void ShowPlaceholderTrue_WithNullValue_StillRealizesTemplate()
    {
        var container = Create(null, TextTemplate());

        container.ShowPlaceholder = true;

        Assert.True(container.IsPlaceholderVisible);
        Assert.NotNull(Host(container).Content);
    }

    [StaFact]
    public void ShowPlaceholderNull_DefersBackToValue()
    {
        var container = Create(null, TextTemplate());
        container.ShowPlaceholder = false;

        container.ShowPlaceholder = null;

        Assert.True(container.IsPlaceholderVisible);
        Assert.Equal(Visibility.Collapsed, Content(container).Visibility);
    }

    [StaFact]
    public void ShowPlaceholderOverride_StillIgnoresCollectionChanges()
    {
        var items = new CountingCollection();
        var container = Create(items, TextTemplate());
        container.ShowPlaceholder = false;
        RaiseLoaded(container);

        items.Add("a");
        items.Clear();

        Assert.False(container.IsPlaceholderVisible);
        Assert.Equal(Visibility.Visible, Content(container).Visibility);
    }

    [StaFact]
    public void NullValue_PlaceholderTemplate_IsRealized()
    {
        // A ContentPresenter skips null content, so this guards the null-selection case end to end.
        var container = new PlaceholderContainer { Value = null, PlaceholderTemplate = TextTemplate() };
        var window = new Window
        {
            Content = container,
            Width = 400,
            Height = 300,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = -10000,
        };
        window.Show();
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
        try
        {
            Assert.Single(Descendants(Host(container)).OfType<TextBlock>());
        }
        finally
        {
            window.Close();
        }
    }
}
