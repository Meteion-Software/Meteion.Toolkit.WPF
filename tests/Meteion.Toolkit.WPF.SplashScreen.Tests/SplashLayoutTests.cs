using System.Drawing;
using Meteion.Toolkit.WPF.SplashScreen.Rendering;

namespace Meteion.Toolkit.WPF.SplashScreen.Tests;

public class SplashLayoutTests
{
    private static readonly Size Image = new(500, 300);

    [Fact]
    public void AutoBar_IsSixtyPercentWidth_SixPixelsTall_CenteredAndEightPercentAboveTheBottom()
    {
        var layout = SplashLayout.Compute(new SplashScreenOptions(), Image, 1f);

        Assert.Equal(new Size(300, 6), layout.Bar.Size);
        Assert.Equal(100, layout.Bar.Left);
        Assert.Equal(300 - 24, layout.Bar.Bottom); // 8% of 300 = 24
    }

    [Fact]
    public void AutoText_WithBar_SitsDirectlyAboveTheBar()
    {
        var options = new SplashScreenOptions { ShowProgressBar = true, ShowStatusText = true };

        var layout = SplashLayout.Compute(options, Image, 1f);

        Assert.Equal(layout.Bar.Left, layout.TextRegion.Left);
        Assert.Equal(layout.Bar.Width, layout.TextRegion.Width);
        Assert.True(layout.TextRegion.Bottom < layout.Bar.Top);
    }

    [Fact]
    public void AutoText_WithoutBar_TakesTheBarsPlace()
    {
        var options = new SplashScreenOptions { ShowProgressBar = false, ShowStatusText = true };

        var layout = SplashLayout.Compute(options, Image, 1f);

        Assert.Equal(layout.Bar.Bottom, layout.TextRegion.Bottom);
    }

    [Fact]
    public void ExplicitOverrides_AreHonoredIndividually()
    {
        var options = new SplashScreenOptions { ProgressBarLocation = new Point(10, 20) };

        var layout = SplashLayout.Compute(options, Image, 1f);

        Assert.Equal(new Point(10, 20), layout.Bar.Location);
        Assert.Equal(new Size(300, 6), layout.Bar.Size); // size stays auto
    }

    [Fact]
    public void Scale_AppliesToEverything()
    {
        var options = new SplashScreenOptions { ProgressBarCornerRadius = 2, FontSize = 14 };

        var layout = SplashLayout.Compute(options, Image, 1.5f);

        Assert.Equal(new Size(750, 450), layout.PixelSize);
        Assert.Equal(21f, layout.FontPixelSize);
        Assert.Equal(3f, layout.CornerRadius);
        Assert.Equal(450, layout.Bar.Width);
    }

    [Fact]
    public void CornerRadius_IsClampedToHalfTheBarHeight()
    {
        var options = new SplashScreenOptions { ProgressBarCornerRadius = 50 };

        var layout = SplashLayout.Compute(options, Image, 1f);

        Assert.Equal(3f, layout.CornerRadius);
    }
}
