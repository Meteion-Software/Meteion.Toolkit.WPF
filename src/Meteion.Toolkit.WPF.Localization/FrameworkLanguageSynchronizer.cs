using System.Globalization;
using System.Windows;
using System.Windows.Markup;

namespace Meteion.Toolkit.WPF.Localization;

/// <summary>
/// Keeps <see cref="FrameworkElement.LanguageProperty"/> in sync with the active
/// <see cref="Meteion.Toolkit.Localization.Abstractions.ILocalizationService.CurrentCulture"/>.
/// </summary>
/// <remarks>
/// WPF's own binding pipeline — <c>StringFormat</c>, and any bound value's implicit
/// <c>ToString</c> conversion — resolves its formatting culture from the target element's
/// <see cref="FrameworkElement.Language"/>, not from <see cref="CultureInfo.CurrentCulture"/>.
/// Every <see cref="FrameworkElement"/> defaults <c>Language</c> to "en-US" regardless of the
/// OS locale, which is why numbers/dates/currency in a plain
/// <c>{Binding ..., StringFormat=...}</c> never followed a culture change even though resx
/// string lookups (which go through <c>ILocalizationService.GetString</c> directly) did.
///
/// <see cref="FrameworkElement.LanguageProperty"/>'s default value can only be overridden
/// once per type — a second <c>OverrideMetadata</c> call throws — so it can't be used to react
/// to a culture change after startup. Instead, <see cref="Sync"/> walks every currently open
/// window and sets its <c>Language</c> explicitly: <c>Language</c> is inherited, so this also
/// updates every descendant that hasn't set its own <c>Language</c> locally. The chosen
/// language is remembered so it can also be applied to windows created afterwards, via a
/// one-time class handler on <see cref="Window"/>'s <see cref="FrameworkElement.LoadedEvent"/>.
/// </remarks>
internal static class FrameworkLanguageSynchronizer
{
    private static XmlLanguage? _currentLanguage;
    private static bool _windowHandlerRegistered;

    /// <summary>
    /// Applies <paramref name="culture"/> as the <c>Language</c> of every open window, and of
    /// windows loaded later.
    /// </summary>
    /// <param name="culture">The culture whose IETF language tag becomes the WPF language.</param>
    public static void Sync(CultureInfo culture)
    {
        EnsureWindowHandlerRegistered();

        _currentLanguage = XmlLanguage.GetLanguage(culture.IetfLanguageTag);

        if (Application.Current is not { } app)
        {
            // No running WPF Application (e.g. unit tests, or called before the
            // Application object exists) — nothing to propagate to yet. The class
            // handler registered above still picks up every window created later.
            return;
        }

        // Snapshot before iterating: setting Language can run arbitrary user code (e.g.
        // a bound Language-driven trigger) that could add/remove windows mid-enumeration.
        var windows = new Window[app.Windows.Count];
        app.Windows.CopyTo(windows, 0);

        foreach (var window in windows)
        {
            window.Language = _currentLanguage;
        }
    }

    private static void EnsureWindowHandlerRegistered()
    {
        if (_windowHandlerRegistered) return;
        _windowHandlerRegistered = true;

        EventManager.RegisterClassHandler(
            typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (_currentLanguage is { } language && sender is Window window)
        {
            window.Language = language;
        }
    }
}
