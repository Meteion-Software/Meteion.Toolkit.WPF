using System.Windows.Threading;

namespace Meteion.Toolkit.WPF.MVVM.Tests.TestHelpers;

internal static class DispatcherTestHelper
{
    /// <summary>
    /// Frame's Content setter (and anything built on Frame navigation) queues work on
    /// the dispatcher rather than applying synchronously. Pumps the queue once so tests
    /// can assert against the settled state instead of the in-flight one.
    /// </summary>
    public static void DrainDispatcher()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }
}
