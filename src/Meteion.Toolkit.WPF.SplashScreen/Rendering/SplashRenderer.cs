using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace Meteion.Toolkit.WPF.SplashScreen.Rendering;

/// <summary>What changes between frames; everything else is fixed for the life of the splash.</summary>
internal readonly record struct SplashRenderState(double Progress, bool Indeterminate, double IndeterminatePhase, string? Text);

/// <summary>
/// Draws the splash with GDI+. The DPI-scaled background is rendered once; each frame copies it and draws only the
/// bar and the text over the copy. Thread-affine: owns GDI+ objects, so create, use and dispose on the splash thread.
/// </summary>
internal sealed unsafe class SplashRenderer : IDisposable
{
    internal const double IndeterminateSegmentFraction = 0.25;

    private const string FallbackFontFamily = "Segoe UI";

    private readonly SplashScreenOptions _options;
    private readonly SplashCanvas _background;
    private readonly SolidBrush _fillBrush;
    private readonly SolidBrush _trackBrush;
    private readonly SolidBrush _textBrush;
    private readonly StringFormat _textFormat;
    private readonly Font _font;

    /// <param name="decoded">The decoded PNG. Only used during construction; the caller still owns it.</param>
    /// <param name="options">The splash options.</param>
    /// <param name="scale">Image pixels to physical pixels.</param>
    /// <param name="log">Receives a warning if the font family is missing.</param>
    public SplashRenderer(Bitmap decoded, SplashScreenOptions options, float scale, SplashLog log)
    {
        _options = options;
        Layout = SplashLayout.Compute(options, decoded.Size, scale);

        _background = new SplashCanvas(Layout.PixelSize.Width, Layout.PixelSize.Height);
        _fillBrush = new SolidBrush(options.ProgressBarFillColor);
        _trackBrush = new SolidBrush(options.ProgressBarTrackColor);
        _textBrush = new SolidBrush(options.TextColor);
        _textFormat = new StringFormat(StringFormatFlags.NoWrap)
        {
            Alignment = options.TextHorizontalAlignment,
            LineAlignment = options.TextVerticalAlignment,
            Trimming = StringTrimming.EllipsisCharacter,
        };
        _font = CreateFont(options, Layout.FontPixelSize, log);

        DrawBackground(decoded);
    }

    public SplashLayout Layout { get; }

    public Size PixelSize => Layout.PixelSize;

    /// <summary>Draws one frame into <paramref name="frame"/>, which must be <see cref="PixelSize"/> in size.</summary>
    public void Render(SplashCanvas frame, in SplashRenderState state)
    {
        Buffer.MemoryCopy(_background.Bits, frame.Bits, frame.ByteCount, _background.ByteCount);

        var g = frame.Graphics;
        g.CompositingMode = CompositingMode.SourceOver;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit; // grayscale; ClearType cannot work on a surface with alpha

        if (_options.ShowProgressBar)
        {
            DrawBar(g, state);
        }

        if (_options.ShowStatusText && !string.IsNullOrEmpty(state.Text))
        {
            g.DrawString(state.Text, _font, _textBrush, Layout.TextRegion, _textFormat);
        }
    }

    public void Dispose()
    {
        _background.Dispose();
        _fillBrush.Dispose();
        _trackBrush.Dispose();
        _textBrush.Dispose();
        _textFormat.Dispose();
        _font.Dispose();
    }

    private void DrawBackground(Bitmap decoded)
    {
        var g = _background.Graphics;
        g.CompositingMode = CompositingMode.SourceCopy;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.SmoothingMode = SmoothingMode.HighQuality;

        // Wrap mode avoids a faded edge pixel row/column when the image is resampled. The explicit pixel-unit source
        // rectangle keeps the PNG's own DPI metadata from changing the drawn size.
        using var attributes = new ImageAttributes();
        attributes.SetWrapMode(WrapMode.TileFlipXY);
        g.DrawImage(
            decoded,
            new Rectangle(Point.Empty, Layout.PixelSize),
            0, 0, decoded.Width, decoded.Height,
            GraphicsUnit.Pixel,
            attributes);
    }

    private void DrawBar(Graphics g, in SplashRenderState state)
    {
        var bar = Layout.Bar;
        using (var track = RoundedRect(bar, Layout.CornerRadius))
        {
            g.FillPath(_trackBrush, track);
        }

        Rectangle fill;
        if (state.Indeterminate)
        {
            var segment = (int)Math.Round(bar.Width * IndeterminateSegmentFraction);
            var offset = (int)Math.Round(((bar.Width + segment) * state.IndeterminatePhase) - segment);
            var left = Math.Max(bar.Left, bar.Left + offset);
            var right = Math.Min(bar.Right, bar.Left + offset + segment);
            fill = new Rectangle(left, bar.Top, right - left, bar.Height);
        }
        else
        {
            var width = (int)Math.Round(bar.Width * Math.Clamp(state.Progress, 0, 1));
            fill = new Rectangle(bar.Left, bar.Top, width, bar.Height);
        }

        if (fill.Width > 0)
        {
            using var path = RoundedRect(fill, Layout.CornerRadius);
            g.FillPath(_fillBrush, path);
        }
    }

    private static GraphicsPath RoundedRect(Rectangle r, float radius)
    {
        var path = new GraphicsPath();
        var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        if (d <= 0)
        {
            path.AddRectangle(r);
            return path;
        }

        path.AddArc(r.Left, r.Top, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Font CreateFont(SplashScreenOptions options, float pixelSize, SplashLog log)
    {
        FontFamily family;
        try
        {
            family = new FontFamily(options.FontFamily);
        }
        catch (ArgumentException)
        {
            log.Warning($"Font family '{options.FontFamily}' was not found; falling back to '{FallbackFontFamily}'.");
            family = new FontFamily(FallbackFontFamily);
        }

        using (family)
        {
            var style = options.FontStyle;
            if (!family.IsStyleAvailable(style))
            {
                log.Warning($"Font family '{family.Name}' does not support style '{style}'; using Regular.");
                style = FontStyle.Regular;
            }

            return new Font(family, pixelSize, style, GraphicsUnit.Pixel);
        }
    }
}
