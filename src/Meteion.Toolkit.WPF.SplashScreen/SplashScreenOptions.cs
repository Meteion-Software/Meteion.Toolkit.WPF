using System.Drawing;

namespace Meteion.Toolkit.WPF.SplashScreen;

/// <summary>
/// Appearance and timing of a splash screen. All sizes, locations and font sizes are in <b>image pixels</b>
/// (relative to the image's top-left corner), not points or DIPs; with <see cref="DpiScaling"/> they are scaled
/// to the target monitor. Nullable layout properties mean auto-layout.
/// </summary>
public sealed class SplashScreenOptions
{
    // ---- Progress bar ----

    /// <summary>Whether a progress bar is drawn. Progress reports are ignored while this is <c>false</c>.</summary>
    public bool ShowProgressBar { get; set; }

    /// <summary>Top-left of the bar, in image pixels. <c>null</c> = centered, 8% of the image height above the bottom.</summary>
    public Point? ProgressBarLocation { get; set; }

    /// <summary>Size of the bar, in image pixels. <c>null</c> = 60% of the image width by 6 px.</summary>
    public Size? ProgressBarSize { get; set; }

    /// <summary>Color of the filled part of the bar (also used by the indeterminate segment).</summary>
    public Color ProgressBarFillColor { get; set; } = Color.FromArgb(0xFF, 0x00, 0x78, 0xD4);

    /// <summary>Color of the bar's track.</summary>
    public Color ProgressBarTrackColor { get; set; } = Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF);

    /// <summary>Corner radius of the bar, in image pixels.</summary>
    public int ProgressBarCornerRadius { get; set; }

    /// <summary>When <c>true</c> the fill eases toward each new value instead of jumping.</summary>
    public bool SmoothProgress { get; set; } = true;

    /// <summary>How long the fill takes to ease toward a new value.</summary>
    public TimeSpan ProgressSmoothingDuration { get; set; } = TimeSpan.FromMilliseconds(150);

    // ---- Status text ----

    /// <summary>Whether status text is drawn. Status reports are ignored while this is <c>false</c>.</summary>
    public bool ShowStatusText { get; set; }

    /// <summary>The rectangle the text is laid out in, in image pixels. <c>null</c> = one line directly above the bar.</summary>
    public Rectangle? StatusTextRegion { get; set; }

    /// <summary>Font family name. Falls back to Segoe UI (with a logged warning) if it is not installed.</summary>
    public string FontFamily { get; set; } = "Segoe UI";

    /// <summary>Font size in <b>image pixels</b> (not points).</summary>
    public float FontSize { get; set; } = 14f;

    /// <summary>Bold / italic flags.</summary>
    public FontStyle FontStyle { get; set; } = FontStyle.Regular;

    /// <summary>Color of the status text.</summary>
    public Color TextColor { get; set; } = Color.White;

    /// <summary>Horizontal alignment of the text within <see cref="StatusTextRegion"/>.</summary>
    public StringAlignment TextHorizontalAlignment { get; set; } = StringAlignment.Center;

    /// <summary>Vertical alignment of the text within <see cref="StatusTextRegion"/>.</summary>
    public StringAlignment TextVerticalAlignment { get; set; } = StringAlignment.Center;

    /// <summary>Status text shown before the first report.</summary>
    public string? InitialStatusText { get; set; }

    // ---- Window and timing ----

    /// <summary>Keep the splash above other windows while loading. Off by default; the splash always goes topmost during handoff.</summary>
    public bool TopMost { get; set; }

    /// <summary>Fade the splash in instead of showing it instantly.</summary>
    public bool FadeIn { get; set; }

    /// <summary>Length of the fade-in.</summary>
    public TimeSpan FadeInDuration { get; set; } = TimeSpan.FromMilliseconds(150);

    /// <summary>Fade the splash out when it closes (otherwise it disappears instantly).</summary>
    public bool FadeOut { get; set; } = true;

    /// <summary>Length of the fade-out.</summary>
    public TimeSpan FadeOutDuration { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// The splash stays visible at least this long, measured from <see cref="SplashScreenBuilder.Show"/>. This only
    /// delays the splash disappearing, never the main window.
    /// </summary>
    public TimeSpan MinimumDisplayTime { get; set; } = TimeSpan.Zero;

    /// <summary>Scale the whole composite by the target monitor's DPI. When <c>false</c> it renders 1:1 in physical pixels.</summary>
    public bool DpiScaling { get; set; } = true;

    /// <summary>Cheap synchronous validation, called from <see cref="SplashScreenBuilder.Show"/>.</summary>
    internal void Validate()
    {
        if (FontSize <= 0 || float.IsNaN(FontSize)) throw new ArgumentOutOfRangeException(nameof(FontSize), FontSize, "Must be greater than zero.");
        if (ProgressBarCornerRadius < 0) throw new ArgumentOutOfRangeException(nameof(ProgressBarCornerRadius), ProgressBarCornerRadius, "Must not be negative.");
        if (ProgressBarSize is { Width: <= 0 } or { Height: <= 0 }) throw new ArgumentOutOfRangeException(nameof(ProgressBarSize), ProgressBarSize, "Width and height must be greater than zero.");
        if (StatusTextRegion is { Width: <= 0 } or { Height: <= 0 }) throw new ArgumentOutOfRangeException(nameof(StatusTextRegion), StatusTextRegion, "Width and height must be greater than zero.");
        if (ProgressSmoothingDuration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(ProgressSmoothingDuration), "Must not be negative.");
        if (FadeInDuration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(FadeInDuration), "Must not be negative.");
        if (FadeOutDuration < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(FadeOutDuration), "Must not be negative.");
        if (MinimumDisplayTime < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(MinimumDisplayTime), "Must not be negative.");
    }
}
