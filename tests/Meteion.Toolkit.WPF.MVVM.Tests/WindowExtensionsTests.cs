using Meteion.Toolkit.MVVM;
using Meteion.Toolkit.WPF.MVVM.Tests.TestHelpers;
using System.Windows;
using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.MVVM.Tests;

/// <summary>
/// Window/Frame are FrameworkElements, so constructing them requires an STA thread.
/// </summary>
public class WindowExtensionsTests
{
    [StaFact]
    public void GetDataContext_ContentIsFrameWithPage_ReturnsPageDataContext()
    {
        var window = new Window();
        var frame = new Frame();
        window.Content = frame;
        frame.Content = new Page { DataContext = "the data context" };
        DispatcherTestHelper.DrainDispatcher();

        Assert.Equal("the data context", window.GetDataContext());
    }

    [StaFact]
    public void GetDataContext_ContentIsNotFrame_ReturnsNull()
    {
        var window = new Window { Content = new TextBlock() };

        Assert.Null(window.GetDataContext());
    }

    [StaFact]
    public void GetDataContext_NoContent_ReturnsNull()
    {
        var window = new Window();

        Assert.Null(window.GetDataContext());
    }
}
