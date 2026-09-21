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

    /// <summary>
    /// Repeatedly drains the dispatcher until <paramref name="condition"/> is met. Needed to carry
    /// a NavigateTo()/GoBack() call all the way through to completion in a test: nothing else is
    /// running a message loop to process the Frame's dispatcher-queued Navigating/Navigated events,
    /// so simply awaiting the returned Task hangs forever. Throws <see cref="TimeoutException"/>
    /// rather than hanging indefinitely if <paramref name="condition"/> is never met.
    /// </summary>
    public static void PumpUntil(Func<bool> condition, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));

        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException("Condition was not met before the timeout elapsed.");
            }

            DrainDispatcher();
            Thread.Sleep(1);
        }
    }
}
