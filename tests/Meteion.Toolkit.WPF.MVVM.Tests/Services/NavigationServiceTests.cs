using Meteion.Toolkit.MVVM.Services;
using Meteion.Toolkit.WPF.MVVM.Tests.Fixtures;
using Meteion.Toolkit.WPF.MVVM.Tests.TestHelpers;
using System.Windows;
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
    public void CanGoBack_NothingNavigatedYet_IsFalse()
    {
        var service = new NavigationService(new FakePageResolutionService());
        service.Initialize(new Frame());

        Assert.False(service.CanGoBack);
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
        var pageB = new FakePageB();
        var pageService = new FakePageResolutionService();
        pageService.PageTypesByViewModelType[typeof(FakeViewModelA)] = typeof(FakePageA);
        pageService.PageFactoriesByViewModelType[typeof(FakeViewModelA)] = () => pageA;
        pageService.PageTypesByViewModelType[typeof(FakeViewModelB)] = typeof(FakePageB);
        pageService.PageFactoriesByViewModelType[typeof(FakeViewModelB)] = () => pageB;
        pageService.ViewModelInstancesByViewModelType[typeof(FakeViewModelB)] = vmB;

        var service = new NavigationService(pageService);
        service.Initialize(frame);

        // The service only tracks pages it navigated to itself, so A has to be reached via NavigateTo to land on the back stack.
        await NavigateAsync(service, typeof(FakeViewModelA));

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

    // Navigates and pumps the dispatcher until the navigation (including callbacks) has finished.
    private static async Task<bool> NavigateAsync(NavigationService service, Type viewModelType, object? parameter = null)
    {
        var task = service.NavigateTo(viewModelType, parameter);
        DispatcherTestHelper.PumpUntil(() => task.IsCompleted);
        return await task;
    }

    private static async Task<bool> GoBackAsync(NavigationService service)
    {
        var task = service.GoBack();
        DispatcherTestHelper.PumpUntil(() => task.IsCompleted);
        return await task;
    }

    // Builds a resolution service for view models A and B. Scoped hands back the same page (and view model) every
    // time; Transient builds a new page with a new view model on each resolve, as DI would.
    private static FakePageResolutionService CreateLifetimeResolution(bool scoped, List<string> log, List<FakeNavigationAwareViewModel> createdA)
    {
        var service = new FakePageResolutionService();
        service.PageTypesByViewModelType[typeof(FakeViewModelA)] = typeof(FakePageA);
        service.PageTypesByViewModelType[typeof(FakeViewModelB)] = typeof(FakePageB);

        FakePageA? scopedA = null;
        service.PageFactoriesByViewModelType[typeof(FakeViewModelA)] = () =>
        {
            if (scoped && scopedA != null)
            {
                return scopedA;
            }

            var vm = new FakeNavigationAwareViewModel($"A{createdA.Count + 1}", log);
            createdA.Add(vm);
            return scopedA = new FakePageA { DataContext = vm };
        };

        var scopedB = new FakePageB { DataContext = new FakeNavigationAwareViewModel("B", log) };
        service.PageFactoriesByViewModelType[typeof(FakeViewModelB)] = () => scoped
            ? scopedB
            : new FakePageB { DataContext = new FakeNavigationAwareViewModel("B", log) };

        return service;
    }

    [StaFact]
    public async Task GoBack_Scoped_ReturnsSamePageAndViewModelInstance()
    {
        var log = new List<string>();
        var createdA = new List<FakeNavigationAwareViewModel>();
        var frame = new Frame();
        var service = new NavigationService(CreateLifetimeResolution(scoped: true, log, createdA));
        service.Initialize(frame);

        await NavigateAsync(service, typeof(FakeViewModelA));
        var pageA = frame.Content;
        await NavigateAsync(service, typeof(FakeViewModelB));
        Assert.True(await GoBackAsync(service));

        Assert.Same(pageA, frame.Content);
        Assert.Single(createdA);
    }

    [StaFact]
    public async Task GoBack_Transient_ReturnsNewInstanceAndNotifiesOldOne()
    {
        var log = new List<string>();
        var createdA = new List<FakeNavigationAwareViewModel>();
        var frame = new Frame();
        var service = new NavigationService(CreateLifetimeResolution(scoped: false, log, createdA));
        service.Initialize(frame);

        await NavigateAsync(service, typeof(FakeViewModelA));
        var pageA = frame.Content;
        await NavigateAsync(service, typeof(FakeViewModelB));
        Assert.Contains("A1.OnNavigatedFrom", log);

        Assert.True(await GoBackAsync(service));

        Assert.NotSame(pageA, frame.Content);
        Assert.Equal(2, createdA.Count);
        Assert.Same(createdA[1], ((FrameworkElement)frame.Content).DataContext);
        Assert.Contains("A2.OnNavigatedTo", log);
    }

    [StaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NavigateTo_AToBToA_PushesEachLeftPage(bool scoped)
    {
        var log = new List<string>();
        var service = new NavigationService(CreateLifetimeResolution(scoped, log, []));
        var frame = new Frame();
        service.Initialize(frame);

        await NavigateAsync(service, typeof(FakeViewModelA));
        await NavigateAsync(service, typeof(FakeViewModelB));
        Assert.True(await NavigateAsync(service, typeof(FakeViewModelA)));
        Assert.IsType<FakePageA>(frame.Content);

        // Stack is now [A, B]: back lands on B, then on A, then there is nothing left.
        Assert.True(await GoBackAsync(service));
        Assert.IsType<FakePageB>(frame.Content);
        Assert.True(await GoBackAsync(service));
        Assert.IsType<FakePageA>(frame.Content);
        Assert.False(service.CanGoBack);
    }

    [StaFact]
    public async Task GoBack_PassesStoredParameterToDestination()
    {
        var log = new List<string>();
        var vmA = new FakeNavigationAwareViewModel("A", log);
        var pageService = new FakePageResolutionService();
        pageService.PageTypesByViewModelType[typeof(FakeViewModelA)] = typeof(FakePageA);
        pageService.PageFactoriesByViewModelType[typeof(FakeViewModelA)] = () => new FakePageA { DataContext = vmA };
        pageService.PageTypesByViewModelType[typeof(FakeViewModelB)] = typeof(FakePageB);
        pageService.PageFactoriesByViewModelType[typeof(FakeViewModelB)] = () => new FakePageB();
        pageService.ViewModelInstancesByViewModelType[typeof(FakeViewModelB)] = new FakeNavigationAwareViewModel("B", log);
        var service = new NavigationService(pageService);
        service.Initialize(new Frame());

        await NavigateAsync(service, typeof(FakeViewModelA), 1);
        await NavigateAsync(service, typeof(FakeViewModelB), "other");
        await GoBackAsync(service);

        Assert.Equal(1, vmA.LastNavigatedToParameter);
    }

    [StaFact]
    public async Task CanGoBack_TracksOwnBackStack()
    {
        var service = new NavigationService(CreateLifetimeResolution(scoped: true, [], []));
        service.Initialize(new Frame());

        await NavigateAsync(service, typeof(FakeViewModelA));
        Assert.False(service.CanGoBack); // first page: nothing to return to

        await NavigateAsync(service, typeof(FakeViewModelB));
        Assert.True(service.CanGoBack);

        service.IsNavigationLocked = true;
        Assert.False(service.CanGoBack);
        service.IsNavigationLocked = false;

        await GoBackAsync(service);
        Assert.False(service.CanGoBack);
    }

    [StaFact]
    public async Task CleanNavigation_EmptiesBackStack()
    {
        var service = new NavigationService(CreateLifetimeResolution(scoped: true, [], []));
        service.Initialize(new Frame());
        await NavigateAsync(service, typeof(FakeViewModelA));
        await NavigateAsync(service, typeof(FakeViewModelB));
        Assert.True(service.CanGoBack);

        service.CleanNavigation();

        Assert.False(service.CanGoBack);
        Assert.False(await GoBackAsync(service));
    }

    [StaFact]
    public async Task Navigation_NeverLeavesEntriesInFrameJournal()
    {
        var frame = new Frame();
        var service = new NavigationService(CreateLifetimeResolution(scoped: false, [], []));
        service.Initialize(frame);

        await NavigateAsync(service, typeof(FakeViewModelA));
        await NavigateAsync(service, typeof(FakeViewModelB));
        Assert.False(frame.CanGoBack);

        await GoBackAsync(service);
        Assert.False(frame.CanGoBack);
    }
}
