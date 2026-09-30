using System.Drawing;
using Meteion.Toolkit.WPF.SplashScreen.Rendering;

namespace Meteion.Toolkit.WPF.SplashScreen.Tests;

/// <summary>Renders into an off-screen canvas; no window is involved.</summary>
public class SplashRendererTests
{
    private static readonly Color Opaque = Color.FromArgb(255, 20, 40, 60);

    private static Bitmap MakeImage(int width, int height, Color color)
    {
        var bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        g.Clear(color);
        return bitmap;
    }

    private static SplashScreenOptions BarOptions() => new()
    {
        ShowProgressBar = true,
        ProgressBarSize = new Size(100, 10),
        ProgressBarLocation = new Point(20, 80),
        ProgressBarFillColor = Color.Red,
        ProgressBarTrackColor = Color.Black,
    };

    [Fact]
    public void Background_HonorsPerPixelAlpha()
    {
        using var image = MakeImage(140, 100, Color.Transparent);
        using var renderer = new SplashRenderer(image, new SplashScreenOptions(), 1f, new SplashLog());
        using var canvas = new SplashCanvas(140, 100);

        renderer.Render(canvas, new SplashRenderState(0, false, 0, null));

        Assert.Equal(0, canvas.Bitmap.GetPixel(70, 50).A);
    }

    [Fact]
    public void Background_IsScaledByTheDpiFactor()
    {
        using var image = MakeImage(140, 100, Opaque);
        using var renderer = new SplashRenderer(image, new SplashScreenOptions(), 2f, new SplashLog());

        Assert.Equal(new Size(280, 200), renderer.PixelSize);
    }

    [Fact]
    public void DeterminateBar_FillsProportionally()
    {
        using var image = MakeImage(140, 100, Opaque);
        using var renderer = new SplashRenderer(image, BarOptions(), 1f, new SplashLog());
        using var canvas = new SplashCanvas(140, 100);

        renderer.Render(canvas, new SplashRenderState(0.5, false, 0, null));

        Assert.Equal(Color.Red.ToArgb(), canvas.Bitmap.GetPixel(40, 85).ToArgb());   // inside the filled half
        Assert.Equal(Color.Black.ToArgb(), canvas.Bitmap.GetPixel(100, 85).ToArgb()); // in the track
        Assert.Equal(Opaque.ToArgb(), canvas.Bitmap.GetPixel(10, 85).ToArgb());       // outside the bar
    }

    [Fact]
    public void HiddenBar_DrawsNothing()
    {
        var options = BarOptions();
        options.ShowProgressBar = false;
        using var image = MakeImage(140, 100, Opaque);
        using var renderer = new SplashRenderer(image, options, 1f, new SplashLog());
        using var canvas = new SplashCanvas(140, 100);

        renderer.Render(canvas, new SplashRenderState(1, false, 0, null));

        Assert.Equal(Opaque.ToArgb(), canvas.Bitmap.GetPixel(40, 85).ToArgb());
    }

    [Fact]
    public void IndeterminateBar_DrawsASegmentThatMoves()
    {
        using var image = MakeImage(140, 100, Opaque);
        using var renderer = new SplashRenderer(image, BarOptions(), 1f, new SplashLog());
        using var canvas = new SplashCanvas(140, 100);

        renderer.Render(canvas, new SplashRenderState(0, true, 0.5, null)); // segment (25 px) mid-way

        var redColumns = Enumerable.Range(20, 100).Count(x => canvas.Bitmap.GetPixel(x, 85).ToArgb() == Color.Red.ToArgb());
        Assert.InRange(redColumns, 24, 26);
    }

    [Fact]
    public void StatusText_IsDrawnInsideItsRegion()
    {
        var options = new SplashScreenOptions
        {
            ShowStatusText = true,
            StatusTextRegion = new Rectangle(10, 10, 120, 30),
            TextColor = Color.White,
            FontSize = 20,
        };
        using var image = MakeImage(140, 100, Color.Black);
        using var renderer = new SplashRenderer(image, options, 1f, new SplashLog());
        using var canvas = new SplashCanvas(140, 100);

        renderer.Render(canvas, new SplashRenderState(0, false, 0, "Loading"));

        var lit = 0;
        for (var y = 10; y < 40; y++)
        {
            for (var x = 10; x < 130; x++)
            {
                if (canvas.Bitmap.GetPixel(x, y).R > 128) lit++;
            }
        }

        Assert.True(lit > 20, $"expected text pixels inside the region, found {lit}");
        Assert.Equal(Color.Black.ToArgb(), canvas.Bitmap.GetPixel(5, 95).ToArgb());
    }

    [Fact]
    public void MissingFontFamily_FallsBackAndLogsAWarning()
    {
        var logged = new List<string>();
        var log = new SplashLog(new ListLogger(logged));
        var options = new SplashScreenOptions { ShowStatusText = true, FontFamily = "No Such Font Family 12345" };
        using var image = MakeImage(140, 100, Opaque);

        using var renderer = new SplashRenderer(image, options, 1f, log);

        Assert.Contains(logged, m => m.Contains("No Such Font Family 12345"));
    }
}
