namespace Meteion.Toolkit.WPF.SplashScreen.Tests;

public class SplashProgressTests
{
    [Fact]
    public void Determinate_SetsBar_LeavesTextUnchanged()
    {
        var p = SplashProgress.Determinate(0.4);

        Assert.Equal(SplashProgress.BarChange.Determinate, p.Bar);
        Assert.Equal(0.4, p.Value);
        Assert.Null(p.StatusText);
    }

    [Fact]
    public void Determinate_WithStatus_SetsBoth()
    {
        var p = SplashProgress.Determinate(0.4, "Loading…");

        Assert.Equal("Loading…", p.StatusText);
    }

    [Fact]
    public void Indeterminate_SwitchesMode_AndOptionallySetsText()
    {
        Assert.Equal(SplashProgress.BarChange.Indeterminate, SplashProgress.Indeterminate().Bar);
        Assert.Null(SplashProgress.Indeterminate().StatusText);
        Assert.Equal("Connecting…", SplashProgress.Indeterminate("Connecting…").StatusText);
    }

    [Fact]
    public void Status_LeavesBarUnchanged()
    {
        var p = SplashProgress.Status("Almost there…");

        Assert.Equal(SplashProgress.BarChange.Unchanged, p.Bar);
        Assert.Equal("Almost there…", p.StatusText);
    }

    [Fact]
    public void Status_Empty_ClearsTheText()
    {
        Assert.Equal(string.Empty, SplashProgress.Status("").StatusText);
    }
}
