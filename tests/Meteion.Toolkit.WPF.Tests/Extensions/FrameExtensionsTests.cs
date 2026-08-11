using System.Windows.Controls;
using System.Windows.Threading;

namespace Meteion.Toolkit.WPF.Tests.Extensions;

/// <summary>
/// Frame is a FrameworkElement, so constructing one requires an STA thread.
/// </summary>
public class FrameExtensionsTests
{
    /// <summary>
    /// Setting Frame.Content directly queues navigation work on the dispatcher rather
    /// than applying synchronously, so a bare assignment isn't reflected in Content
    /// until the dispatcher gets a chance to process it. Pumps the queue once so tests
    /// can assert against the settled state instead of the in-flight one.
    /// </summary>
    private static void DrainDispatcher()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    [StaFact]
    public void GetDataContext_ContentIsFrameworkElementWithDataContext_ReturnsIt()
    {
        var frame = new Frame
        {
            Content = new Page { DataContext = "the data context" }
        };
        DrainDispatcher();

        Assert.Equal("the data context", frame.GetDataContext());
    }

    [StaFact]
    public void GetDataContext_ContentIsNotFrameworkElement_ReturnsNull()
    {
        var frame = new Frame { Content = "just a string" };

        Assert.Null(frame.GetDataContext());
    }

    [StaFact]
    public void GetDataContext_NoContent_ReturnsNull()
    {
        var frame = new Frame();

        Assert.Null(frame.GetDataContext());
    }

    [StaFact]
    public void CleanNavigation_NoBackEntries_DoesNotThrow()
    {
        var frame = new Frame();

        frame.CleanNavigation();

        Assert.False(frame.CanGoBack);
    }
}
