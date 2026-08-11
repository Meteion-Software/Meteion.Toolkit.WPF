using Meteion.Toolkit.MVVM.Services;
using Meteion.Toolkit.WPF.MVVM.Tests.Fixtures;
using Meteion.Toolkit.WPF.MVVM.Tests.TestHelpers;
using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.MVVM.Tests.Services;

/// <summary>
/// Frame is a FrameworkElement, so constructing one requires an STA thread.
/// </summary>
public class NavigationServiceTests
{
    [StaFact]
    public void Initialize_NullFrame_ThrowsArgumentNullException()
    {
        var service = new NavigationService(new FakePageResolutionService());

        Assert.Throws<ArgumentNullException>(() => service.Initialize(null!));
    }

    [StaFact]
    public void Initialize_CalledTwice_DoesNotThrow()
    {
        var service = new NavigationService(new FakePageResolutionService());
        service.Initialize(new Frame());

        // Documented as a no-op (just logs critical) on the second call, not an error.
        service.Initialize(new Frame());
    }

    [StaFact]
    public void CanGoBack_BeforeInitialize_IsFalse()
    {
        var service = new NavigationService(new FakePageResolutionService());

        Assert.False(service.CanGoBack);
    }

    [StaFact]
    public void CanGoBack_ReflectsFrameState()
    {
        var service = new NavigationService(new FakePageResolutionService());
        var frame = new Frame();
        service.Initialize(frame);

        Assert.Equal(frame.CanGoBack, service.CanGoBack);
    }

    [StaFact]
    public async Task NavigateTo_BeforeInitialize_Throws()
    {
        var service = new NavigationService(new FakePageResolutionService());

        await Assert.ThrowsAsync<Exception>(() => service.NavigateTo(typeof(FakeViewModelA)));
    }

    [StaFact]
    public async Task NavigateTo_AlreadyOnTargetPageWithNoParameter_ReturnsFalseWithoutNavigating()
    {
        // Setting Frame.Content directly (rather than via Navigate) and short-circuiting
        // on it lets this be tested without pumping a full async navigation to completion.
        var frame = new Frame();
        var currentPage = new FakePageA();
        frame.Content = currentPage;
        DispatcherTestHelper.DrainDispatcher();

        var pageService = new FakePageResolutionService { PageTypeToReturn = typeof(FakePageA) };
        var service = new NavigationService(pageService);
        service.Initialize(frame);

        var navigated = await service.NavigateTo(typeof(FakeViewModelA));

        Assert.False(navigated);
        Assert.Same(currentPage, frame.Content);
    }

    [StaFact]
    public async Task GoBack_CannotGoBack_ReturnsFalse()
    {
        var service = new NavigationService(new FakePageResolutionService());
        service.Initialize(new Frame());

        var result = await service.GoBack();

        Assert.False(result);
    }

    [StaFact]
    public void CleanNavigation_IsNotYetImplemented()
    {
        // Documents the current, real state of the code rather than an intended one —
        // NavigationService.CleanNavigation is literally `throw new NotImplementedException()`.
        var service = new NavigationService(new FakePageResolutionService());

        Assert.Throws<NotImplementedException>(() => service.CleanNavigation());
    }
}
