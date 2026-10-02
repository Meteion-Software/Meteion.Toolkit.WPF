using System.Drawing;

namespace Meteion.Toolkit.WPF.SplashScreen.Rendering;

/// <summary>
/// Where everything goes, in physical pixels. Computed once from the decoded image's size and the DPI scale;
/// options that are <c>null</c> are auto-laid-out, and each can be overridden individually.
/// </summary>
/// <param name="PixelSize">Size of the whole splash window.</param>
/// <param name="Bar">Progress bar track rectangle.</param>
/// <param name="TextRegion">Rectangle the status text is drawn in.</param>
/// <param name="CornerRadius">Progress bar corner radius.</param>
/// <param name="FontPixelSize">Font size in pixels.</param>
internal readonly record struct SplashLayout(Size PixelSize, Rectangle Bar, Rectangle TextRegion, float CornerRadius, float FontPixelSize)
{
    private const float AutoBarWidthFraction = 0.6f;
    private const int AutoBarHeight = 6;
    private const float AutoBarBottomMarginFraction = 0.08f;
    private const int AutoTextGap = 6;
    private const float LineHeightFactor = 1.4f;

    /// <summary>Resolves auto-layout defaults and scales every rectangle to physical pixels.</summary>
    /// <param name="options">The splash options (values in image pixels).</param>
    /// <param name="imageSize">The decoded image's size in pixels.</param>
    /// <param name="scale">Image pixels to physical pixels (monitor DPI / 96, or 1 when DPI scaling is off).</param>
    /// <returns>The layout in physical pixels.</returns>
    public static SplashLayout Compute(SplashScreenOptions options, Size imageSize, float scale)
    {
        var barSize = options.ProgressBarSize
            ?? new Size((int)MathF.Round(imageSize.Width * AutoBarWidthFraction), AutoBarHeight);

        var barLocation = options.ProgressBarLocation
            ?? new Point(
                (int)MathF.Round((imageSize.Width - barSize.Width) / 2f),
                (int)MathF.Round(imageSize.Height - (imageSize.Height * AutoBarBottomMarginFraction)) - barSize.Height);

        var bar = new Rectangle(barLocation, barSize);

        Rectangle text;
        if (options.StatusTextRegion is { } region)
        {
            text = region;
        }
        else
        {
            var lineHeight = (int)MathF.Ceiling(options.FontSize * LineHeightFactor);
            var top = options.ShowProgressBar ? bar.Top - AutoTextGap - lineHeight : bar.Bottom - lineHeight;
            text = new Rectangle(bar.Left, top, bar.Width, lineHeight);
        }

        var pixelBar = Scale(bar, scale);
        var radius = Math.Min(options.ProgressBarCornerRadius * scale, Math.Min(pixelBar.Width, pixelBar.Height) / 2f);

        return new SplashLayout(
            new Size(Math.Max(1, (int)MathF.Round(imageSize.Width * scale)), Math.Max(1, (int)MathF.Round(imageSize.Height * scale))),
            pixelBar,
            Scale(text, scale),
            radius,
            options.FontSize * scale);
    }

    // Scale by edges rather than by origin and size, so neighbouring rectangles don't drift apart by a pixel.
    private static Rectangle Scale(Rectangle r, float scale)
    {
        var left = (int)MathF.Round(r.Left * scale);
        var top = (int)MathF.Round(r.Top * scale);
        var right = (int)MathF.Round(r.Right * scale);
        var bottom = (int)MathF.Round(r.Bottom * scale);
        return new Rectangle(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
    }
}
