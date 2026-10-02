using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Meteion.Toolkit.WPF.Controls;

/// <summary>
/// A content control that shows its <see cref="ContentControl.Content"/> when its <see cref="State"/> is
/// <see cref="ViewStatus.Loaded"/>, a loading indicator when <see cref="ViewStatus.Loading"/>, and an error
/// panel (with an optional Retry button) when <see cref="ViewStatus.Error"/>.
/// </summary>
/// <remarks>
/// Only the active state visual is instantiated: the loading and error templates are realized on entry and
/// released on exit. In <see cref="StatefulContainerMode.Replace"/> mode the content is collapsed but kept
/// alive, so its state (scroll position, selection) survives a reload.
/// </remarks>
[TemplatePart(Name = ContentPartName, Type = typeof(UIElement))]
[TemplatePart(Name = StateLayerPartName, Type = typeof(Border))]
[TemplatePart(Name = StateHostPartName, Type = typeof(ContentPresenter))]
public class StatefulContainer : ContentControl
{
    private const string ContentPartName = "PART_Content";
    private const string StateLayerPartName = "PART_StateLayer";
    private const string StateHostPartName = "PART_StateHost";

    private static readonly Brush DefaultOverlayBackground = CreateOverlayBackground();

    /// <summary>Identifies the <see cref="State"/> dependency property.</summary>
    public static readonly DependencyProperty StateProperty =
        DependencyProperty.Register(nameof(State), typeof(IViewState), typeof(StatefulContainer),
            new PropertyMetadata(null, OnStateChanged));

    /// <summary>Identifies the <see cref="Mode"/> dependency property.</summary>
    public static readonly DependencyProperty ModeProperty =
        DependencyProperty.Register(nameof(Mode), typeof(StatefulContainerMode), typeof(StatefulContainer),
            new PropertyMetadata(StatefulContainerMode.Replace, OnPresentationChanged));

    /// <summary>Identifies the <see cref="OverlayBackground"/> dependency property.</summary>
    public static readonly DependencyProperty OverlayBackgroundProperty =
        DependencyProperty.Register(nameof(OverlayBackground), typeof(Brush), typeof(StatefulContainer),
            new PropertyMetadata(DefaultOverlayBackground, OnPresentationChanged));

    /// <summary>Identifies the <see cref="LoadingTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty LoadingTemplateProperty =
        DependencyProperty.Register(nameof(LoadingTemplate), typeof(DataTemplate), typeof(StatefulContainer),
            new PropertyMetadata(null, OnPresentationChanged));

    /// <summary>Identifies the <see cref="ErrorTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty ErrorTemplateProperty =
        DependencyProperty.Register(nameof(ErrorTemplate), typeof(DataTemplate), typeof(StatefulContainer),
            new PropertyMetadata(null, OnPresentationChanged));

    /// <summary>Identifies the <see cref="RetryCommand"/> dependency property.</summary>
    public static readonly DependencyProperty RetryCommandProperty =
        DependencyProperty.Register(nameof(RetryCommand), typeof(ICommand), typeof(StatefulContainer),
            new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="RetryCommandParameter"/> dependency property.</summary>
    public static readonly DependencyProperty RetryCommandParameterProperty =
        DependencyProperty.Register(nameof(RetryCommandParameter), typeof(object), typeof(StatefulContainer),
            new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="RetryContent"/> dependency property.</summary>
    public static readonly DependencyProperty RetryContentProperty =
        DependencyProperty.Register(nameof(RetryContent), typeof(object), typeof(StatefulContainer),
            new PropertyMetadata("Retry"));

    /// <summary>Identifies the <see cref="ErrorTitle"/> dependency property.</summary>
    public static readonly DependencyProperty ErrorTitleProperty =
        DependencyProperty.Register(nameof(ErrorTitle), typeof(object), typeof(StatefulContainer),
            new PropertyMetadata("Something went wrong"));

    private UIElement? _contentPart;
    private Border? _stateLayerPart;
    private ContentPresenter? _stateHostPart;

    // The State currently observed for property changes; null when not subscribed.
    private IViewState? _subscribedState;

    static StatefulContainer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(StatefulContainer), new FrameworkPropertyMetadata(typeof(StatefulContainer)));
        FocusableProperty.OverrideMetadata(typeof(StatefulContainer), new FrameworkPropertyMetadata(false));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StatefulContainer"/> class. State change subscriptions are
    /// held only while the control is loaded, so it does not leak through its <see cref="State"/>.
    /// </summary>
    public StatefulContainer()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>Gets or sets the state to display. A <see langword="null"/> state is treated as <see cref="ViewStatus.Loaded"/>.</summary>
    public IViewState? State
    {
        get => (IViewState?)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    /// <summary>Gets or sets whether loading/error replace the content or are overlaid on top of it.</summary>
    public StatefulContainerMode Mode
    {
        get => (StatefulContainerMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    /// <summary>Gets or sets the dim layer drawn over the content in <see cref="StatefulContainerMode.Overlay"/> mode.</summary>
    public Brush? OverlayBackground
    {
        get => (Brush?)GetValue(OverlayBackgroundProperty);
        set => SetValue(OverlayBackgroundProperty, value);
    }

    /// <summary>Gets or sets the template for the loading visual. Its data context is the <see cref="State"/>.</summary>
    public DataTemplate? LoadingTemplate
    {
        get => (DataTemplate?)GetValue(LoadingTemplateProperty);
        set => SetValue(LoadingTemplateProperty, value);
    }

    /// <summary>Gets or sets the template for the error visual. Its data context is the <see cref="State"/>.</summary>
    public DataTemplate? ErrorTemplate
    {
        get => (DataTemplate?)GetValue(ErrorTemplateProperty);
        set => SetValue(ErrorTemplateProperty, value);
    }

    /// <summary>Gets or sets the command run by the Retry button. The button is shown only when this is set.</summary>
    public ICommand? RetryCommand
    {
        get => (ICommand?)GetValue(RetryCommandProperty);
        set => SetValue(RetryCommandProperty, value);
    }

    /// <summary>Gets or sets the parameter passed to <see cref="RetryCommand"/> when the Retry button is clicked.</summary>
    public object? RetryCommandParameter
    {
        get => GetValue(RetryCommandParameterProperty);
        set => SetValue(RetryCommandParameterProperty, value);
    }

    /// <summary>Gets or sets the content of the Retry button in the default error template.</summary>
    public object? RetryContent
    {
        get => GetValue(RetryContentProperty);
        set => SetValue(RetryContentProperty, value);
    }

    /// <summary>Gets or sets the heading of the default error template.</summary>
    public object? ErrorTitle
    {
        get => GetValue(ErrorTitleProperty);
        set => SetValue(ErrorTitleProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _contentPart = GetTemplateChild(ContentPartName) as UIElement;
        _stateLayerPart = GetTemplateChild(StateLayerPartName) as Border;
        _stateHostPart = GetTemplateChild(StateHostPartName) as ContentPresenter;
        UpdateVisualState();
    }

    private static Brush CreateOverlayBackground()
    {
        var brush = new SolidColorBrush(Color.FromArgb(0x80, 0, 0, 0));
        brush.Freeze();
        return brush;
    }

    private static void OnStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var container = (StatefulContainer)d;
        container.Unsubscribe();
        if (container.IsLoaded)
        {
            container.Subscribe();
        }

        container.UpdateVisualState();
    }

    private static void OnPresentationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((StatefulContainer)d).UpdateVisualState();

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Subscribe();
        UpdateVisualState();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => Unsubscribe();

    private void Subscribe()
    {
        if (_subscribedState is not null || State is not { } state)
        {
            return;
        }

        state.PropertyChanged += OnStatePropertyChanged;
        _subscribedState = state;
    }

    private void Unsubscribe()
    {
        if (_subscribedState is null)
        {
            return;
        }

        _subscribedState.PropertyChanged -= OnStatePropertyChanged;
        _subscribedState = null;
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateVisualState();

    private void UpdateVisualState()
    {
        if (_contentPart is null || _stateLayerPart is null || _stateHostPart is null)
        {
            return;
        }

        var state = State;
        var status = state?.Status ?? ViewStatus.Loaded;
        var overlay = Mode == StatefulContainerMode.Overlay;
        // "Covered" means the loading or error visual is active and the state layer is shown.
        var covered = status != ViewStatus.Loaded;

        _contentPart.Visibility = covered && !overlay ? Visibility.Collapsed : Visibility.Visible;

        // While an overlay covers the content, keep keyboard tabbing out of it (the layer already blocks the mouse).
        var contentNavigation = covered && overlay ? KeyboardNavigationMode.None : KeyboardNavigationMode.Continue;
        KeyboardNavigation.SetTabNavigation(_contentPart, contentNavigation);
        KeyboardNavigation.SetControlTabNavigation(_contentPart, contentNavigation);

        _stateLayerPart.Visibility = covered ? Visibility.Visible : Visibility.Collapsed;
        _stateLayerPart.Background = covered && overlay ? OverlayBackground ?? Brushes.Transparent : null;

        // Realize the loading/error visual only while it is the active state; clearing it on exit releases the
        // visual tree (and stops the spinner animation) instead of keeping hidden elements around.
        var template = status switch
        {
            ViewStatus.Loading => LoadingTemplate,
            ViewStatus.Error => ErrorTemplate,
            _ => null,
        };

        if (template is null)
        {
            _stateHostPart.ClearValue(ContentPresenter.ContentProperty);
            _stateHostPart.ClearValue(ContentPresenter.ContentTemplateProperty);
            return;
        }

        if (!ReferenceEquals(_stateHostPart.ContentTemplate, template))
        {
            // Switching between loading and error: drop the old visual so it is never reused for the new state.
            _stateHostPart.ClearValue(ContentPresenter.ContentProperty);
            _stateHostPart.ContentTemplate = template;
        }

        _stateHostPart.Content = state;
    }
}
