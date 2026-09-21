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
    public async Task NavigateTo_FullNavigation_FiresCallbacksInOrderWithParameter()
    {
        var log = new List<string>();
        var fromVm = new FakeNavigationAwareViewModel("From", log);
        var toVm = new FakeNavigationAwareViewModel("To", log);

        var frame = new Frame();
        var fromPage = new FakePageA { DataContext = fromVm };
        frame.Content = fromPage;
        DispatcherTestHelper.DrainDispatcher();

        var toPage = new FakePageB();
        var pageService = new FakePageResolutionService();
        pageService.PageTypesByViewModelType[typeof(FakeViewModelB)] = typeof(FakePageB);
        pageService.PageFactoriesByViewModelType[typeof(FakeViewModelB)] = () => toPage;
        pageService.ViewModelInstancesByViewModelType[typeof(FakeViewModelB)] = toVm;

        var service = new NavigationService(pageService);
        service.Initialize(frame);

        var navigateTask = service.NavigateTo(typeof(FakeViewModelB), "the-parameter");
        DispatcherTestHelper.PumpUntil(() => navigateTask.IsCompleted);
        var navigated = await navigateTask;

        Assert.True(navigated);
        Assert.Same(toPage, frame.Content);
        Assert.Equal("the-parameter", toVm.LastNavigatedToParameter);
        Assert.Equal(
        [
            "From.OnNavigatedFrom",
            "From.OnNavigatedFromAsync:start",
            "From.OnNavigatedFromAsync:end",
            "To.OnNavigatedTo",
            "To.OnNavigatedToAsync:start",
            "To.OnNavigatedToAsync:end",
        ], log);
    }

    [StaFact]
    public async Task GoBack_FullNavigation_FiresOnNavigatedFromOnLeavingPage_AndOnNavigatedToOnDestination()
    {
        // Regression test: GoBack() used to re-read Frame.Content immediately after calling
        // Frame.GoBack(), but that content swap is dispatcher-queued rather than synchronous.
        // That meant the "from" and "to" view models were the same instance (the page being
        // left), so it fired OnNavigatedFrom AND OnNavigatedTo on the departing page, while the
        // page actually being returned to never got notified at all.
        var log = new List<string>();
        var vmA = new FakeNavigationAwareViewModel("A", log);
        var vmB = new FakeNavigationAwareViewModel("B", log);

        var frame = new Frame();
        var pageA = new FakePageA { DataContext = vmA };
        frame.Content = pageA;
        DispatcherTestHelper.DrainDispatcher();

        var pageB = new FakePageB();
        var pageService = new FakePageResolutionService();
        pageService.PageTypesByViewModelType[typeof(FakeViewModelB)] = typeof(FakePageB);
        pageService.PageFactoriesByViewModelType[typeof(FakeViewModelB)] = () => pageB;
        pageService.ViewModelInstancesByViewModelType[typeof(FakeViewModelB)] = vmB;

        var service = new NavigationService(pageService);
        service.Initialize(frame);

        var forwardTask = service.NavigateTo(typeof(FakeViewModelB));
        DispatcherTestHelper.PumpUntil(() => forwardTask.IsCompleted);
        Assert.True(await forwardTask);
        Assert.True(service.CanGoBack);

        log.Clear(); // isolate the assertions below to just the GoBack transition

        var backTask = service.GoBack();
        DispatcherTestHelper.PumpUntil(() => backTask.IsCompleted);
        var wentBack = await backTask;

        Assert.True(wentBack);
        Assert.Same(pageA, frame.Content);
        Assert.Equal(
        [
            "B.OnNavigatedFrom",
            "B.OnNavigatedFromAsync:start",
            "B.OnNavigatedFromAsync:end",
            "A.OnNavigatedTo",
            "A.OnNavigatedToAsync:start",
            "A.OnNavigatedToAsync:end",
        ], log);
    }

    [StaFact]
    public async Task NavigateTo_AsyncCallbackSlowerThanIndicatorDelay_RaisesNavigationStartedAndCompleted()
    {
        var log = new List<string>();
        var toVm = new FakeNavigationAwareViewModel("To", log)
        {
            NavigatedToAsyncGate = new TaskCompletionSource<bool>(),
        };

        var frame = new Frame();
        var toPage = new FakePageB();
        var pageService = new FakePageResolutionService
        {
            PageTypeToReturn = typeof(FakePageB),
            PageInstanceFactory = _ => toPage,
            ViewModelInstanceToReturn = toVm,
        };

        var service = new NavigationService(pageService)
        {
            NavigationIndicatorDelay = TimeSpan.FromMilliseconds(30),
        };
        service.Initialize(frame);

        var startedTcs = new TaskCompletionSource<NavigationProgressEventArgs>();
        var completedTcs = new TaskCompletionSource<NavigationProgressEventArgs>();
        service.NavigationStarted += (_, e) => startedTcs.TrySetResult(e);
        service.NavigationCompleted += (_, e) => completedTcs.TrySetResult(e);

        var navigateTask = service.NavigateTo(typeof(FakeViewModelB), "param");

        // The async callback is gated open, so this only completes once NavigationIndicatorDelay
        // has elapsed and RaiseProgressAndAwait raises NavigationStarted.
        DispatcherTestHelper.PumpUntil(() => startedTcs.Task.IsCompleted, TimeSpan.FromSeconds(2));

        var startedArgs = startedTcs.Task.Result;
        Assert.Equal(NavigationDirection.Forward, startedArgs.Direction);
        Assert.Equal(typeof(FakeViewModelB), startedArgs.ViewModelType);
        Assert.Equal("param", startedArgs.NavigationParameter);
        Assert.False(completedTcs.Task.IsCompleted);

        toVm.NavigatedToAsyncGate!.SetResult(true);
        DispatcherTestHelper.PumpUntil(() => navigateTask.IsCompleted);

        Assert.True(await navigateTask);
        Assert.True(completedTcs.Task.IsCompleted);
    }

    [StaFact]
    public async Task NavigateTo_AsyncCallbackFasterThanIndicatorDelay_DoesNotRaiseNavigationProgressEvents()
    {
        var log = new List<string>();
        var toVm = new FakeNavigationAwareViewModel("To", log); // no gates - completes immediately

        var frame = new Frame();
        var toPage = new FakePageB();
        var pageService = new FakePageResolutionService
        {
            PageTypeToReturn = typeof(FakePageB),
            PageInstanceFactory = _ => toPage,
            ViewModelInstanceToReturn = toVm,
        };

        var service = new NavigationService(pageService)
        {
            NavigationIndicatorDelay = TimeSpan.FromSeconds(5), // long enough a fast nav never crosses it
        };
        service.Initialize(frame);

        var startedRaised = false;
        var completedRaised = false;
        service.NavigationStarted += (_, _) => startedRaised = true;
        service.NavigationCompleted += (_, _) => completedRaised = true;

        var navigateTask = service.NavigateTo(typeof(FakeViewModelB));
        DispatcherTestHelper.PumpUntil(() => navigateTask.IsCompleted);

        Assert.True(await navigateTask);
        Assert.False(startedRaised);
        Assert.False(completedRaised);
    }
}
