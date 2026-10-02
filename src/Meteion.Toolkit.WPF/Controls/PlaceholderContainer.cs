using Meteion.Toolkit.WPF.Converters;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.Controls;

/// <summary>
/// A content control that shows its <see cref="ContentControl.Content"/> while <see cref="Value"/> holds something,
/// and the <see cref="PlaceholderTemplate"/> instead when <see cref="Value"/> is <see langword="null"/>, an empty
/// string, or an empty collection. <see cref="ShowPlaceholder"/> overrides that check with an explicit boolean.
/// </summary>
/// <remarks>
/// Use it for "no items" (bind <see cref="Value"/> to the collection) and "nothing selected" (bind it to the
/// selected item). The content is collapsed but kept alive while the placeholder shows, so its state (scroll
/// position, selection) survives. The placeholder visual is realized only while it is active and released on exit.
/// If <see cref="Value"/> raises <see cref="INotifyCollectionChanged.CollectionChanged"/>, adding or removing items
/// switches between the content and the placeholder live.
/// </remarks>
[TemplatePart(Name = ContentPartName, Type = typeof(UIElement))]
[TemplatePart(Name = PlaceholderHostPartName, Type = typeof(ContentPresenter))]
public class PlaceholderContainer : ContentControl
{
    private const string ContentPartName = "PART_Content";
    private const string PlaceholderHostPartName = "PART_PlaceholderHost";

    // A ContentPresenter only instantiates its template for non-null content, so a null Value is swapped for this.
    private static readonly object NullValue = new();

    /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(object), typeof(PlaceholderContainer),
            new PropertyMetadata(null, OnValueChanged));

    /// <summary>Identifies the <see cref="ShowPlaceholder"/> dependency property.</summary>
    public static readonly DependencyProperty ShowPlaceholderProperty =
        DependencyProperty.Register(nameof(ShowPlaceholder), typeof(bool?), typeof(PlaceholderContainer),
            new PropertyMetadata(null, OnPresentationChanged));

    /// <summary>Identifies the <see cref="PlaceholderTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty PlaceholderTemplateProperty =
        DependencyProperty.Register(nameof(PlaceholderTemplate), typeof(DataTemplate), typeof(PlaceholderContainer),
            new PropertyMetadata(null, OnPresentationChanged));

    private static readonly DependencyPropertyKey IsPlaceholderVisiblePropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(IsPlaceholderVisible), typeof(bool), typeof(PlaceholderContainer),
            new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="IsPlaceholderVisible"/> read-only dependency property.</summary>
    public static readonly DependencyProperty IsPlaceholderVisibleProperty = IsPlaceholderVisiblePropertyKey.DependencyProperty;

    private UIElement? _contentPart;
    private ContentPresenter? _placeholderHostPart;

    // The Value currently observed for collection changes; null when not subscribed.
    private INotifyCollectionChanged? _subscribedValue;

    static PlaceholderContainer()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PlaceholderContainer), new FrameworkPropertyMetadata(typeof(PlaceholderContainer)));
        FocusableProperty.OverrideMetadata(typeof(PlaceholderContainer), new FrameworkPropertyMetadata(false));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PlaceholderContainer"/> class. Collection change
    /// subscriptions are held only while the control is loaded, so it does not leak through its <see cref="Value"/>.
    /// </summary>
    public PlaceholderContainer()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>
    /// Gets or sets the value under test. The placeholder shows when it is <see langword="null"/>, an empty
    /// string, or an empty collection.
    /// </summary>
    public object? Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>
    /// Gets or sets an explicit override of <see cref="Value"/>. <see langword="true"/> always shows the placeholder,
    /// <see langword="false"/> always shows the content, and <see langword="null"/> (the default) lets
    /// <see cref="Value"/> decide.
    /// </summary>
    public bool? ShowPlaceholder
    {
        get => (bool?)GetValue(ShowPlaceholderProperty);
        set => SetValue(ShowPlaceholderProperty, value);
    }

    /// <summary>
    /// Gets or sets the template for the placeholder visual. Its data context is <see cref="Value"/>. When
    /// <see cref="Value"/> is <see langword="null"/> the data context is an empty placeholder object, so a template
    /// for that case should not bind to it. With no template, the content is simply hidden.
    /// </summary>
    public DataTemplate? PlaceholderTemplate
    {
        get => (DataTemplate?)GetValue(PlaceholderTemplateProperty);
        set => SetValue(PlaceholderTemplateProperty, value);
    }

    /// <summary>Gets whether the placeholder is currently replacing the content.</summary>
    public bool IsPlaceholderVisible => (bool)GetValue(IsPlaceholderVisibleProperty);

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _contentPart = GetTemplateChild(ContentPartName) as UIElement;
        _placeholderHostPart = GetTemplateChild(PlaceholderHostPartName) as ContentPresenter;
        UpdateVisualState();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var container = (PlaceholderContainer)d;
        container.Unsubscribe();
        if (container.IsLoaded)
        {
            container.Subscribe();
        }

        container.UpdateVisualState();
    }

    private static void OnPresentationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((PlaceholderContainer)d).UpdateVisualState();

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Subscribe();

        // Changes made while unloaded were not observed.
        UpdateVisualState();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => Unsubscribe();

    private void Subscribe()
    {
        if (_subscribedValue is not null || Value is not INotifyCollectionChanged notifier)
        {
            return;
        }

        notifier.CollectionChanged += OnValueCollectionChanged;
        _subscribedValue = notifier;
    }

    private void Unsubscribe()
    {
        if (_subscribedValue is null)
        {
            return;
        }

        _subscribedValue.CollectionChanged -= OnValueCollectionChanged;
        _subscribedValue = null;
    }

    private void OnValueCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateVisualState();

    private void UpdateVisualState()
    {
        var value = Value;
        var empty = ShowPlaceholder ?? NullOrEmpty.Check(value);
        SetValue(IsPlaceholderVisiblePropertyKey, empty);

        if (_contentPart is null || _placeholderHostPart is null)
        {
            return;
        }

        _contentPart.Visibility = empty ? Visibility.Collapsed : Visibility.Visible;

        var template = empty ? PlaceholderTemplate : null;
        if (template is null)
        {
            // Clearing releases the visual tree instead of keeping hidden elements around.
            _placeholderHostPart.Visibility = Visibility.Collapsed;
            _placeholderHostPart.ClearValue(ContentPresenter.ContentProperty);
            _placeholderHostPart.ClearValue(ContentPresenter.ContentTemplateProperty);
            return;
        }

        if (!ReferenceEquals(_placeholderHostPart.ContentTemplate, template))
        {
            _placeholderHostPart.ClearValue(ContentPresenter.ContentProperty);
            _placeholderHostPart.ContentTemplate = template;
        }

        _placeholderHostPart.Content = value ?? NullValue;
        _placeholderHostPart.Visibility = Visibility.Visible;
    }
}
