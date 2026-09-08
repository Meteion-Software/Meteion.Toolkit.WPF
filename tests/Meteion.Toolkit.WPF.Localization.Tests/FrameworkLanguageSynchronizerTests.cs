using System.Globalization;

namespace Meteion.Toolkit.WPF.Localization.Tests;

public class FrameworkLanguageSynchronizerTests
{
    // No real System.Windows.Application runs in this test host (Application.Current is
    // null), and only one Application instance may ever exist per process — so this can't
    // assert Window.Language actually changes. It does prove the no-running-app path (and
    // the one-time Window class-handler registration behind it) is safe to call repeatedly,
    // which is what every LocalizationService construction/CurrentCulture set does.
    [Fact]
    public void Sync_NoRunningApplication_DoesNotThrow()
    {
        FrameworkLanguageSynchronizer.Sync(new CultureInfo("ja-JP"));
        FrameworkLanguageSynchronizer.Sync(new CultureInfo("fr-CA"));
    }
}
