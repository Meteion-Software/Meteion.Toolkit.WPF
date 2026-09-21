using Meteion.Toolkit.WPF;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection.Metadata;
using System.Text;
using System.Windows.Navigation;
using System.Windows.Controls;

namespace Meteion.Toolkit.MVVM.Services;

/// <summary>
/// Implements a default <see cref="INavigationService"/> for frame navigation within a window. This service is responsible for managing navigation between different pages in the application, allowing for navigation to specific view models and handling back navigation.
/// </summary>
public class NavigationService(IPageResolutionService pageService, ILogger<NavigationService>? logger = null) : INavigationService, INavigationProgress
{
    private readonly IPageResolutionService _pageService = pageService;
    private readonly ILogger<NavigationService>? _logger = logger;
    private Frame? _frame;
    private object? _lastParameterUsed;

    // State for the navigation currently in flight, bridging Frame's async Navigating/Navigated
    // events back to the awaiting NavigateTo/GoBack caller. Frame.Content is not guaranteed to
    // reflect the new page synchronously as soon as Navigate()/GoBack() returns, so post-navigation
    // work (HandlePostNav) must be driven off the Navigated event rather than read immediately.
    private bool _navigationInFlight;
    private object? _pendingFromContext;
    private object? _pendingNavigationParameter;
    private Type? _pendingViewModelType;
    private NavigationDirection _pendingDirection;
    private TaskCompletionSource<bool>? _pendingNavigationTcs;

    public bool CanGoBack => (_frame?.CanGoBack ?? false) && !IsNavigationLocked;

    public bool IsNavigationLocked { get; set; }

    public event EventHandler<NavigationProgressEventArgs>? NavigationStarted;

    public event EventHandler<NavigationProgressEventArgs>? NavigationCompleted;

    public TimeSpan NavigationIndicatorDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    public void CleanNavigation()
    {
        _frame?.CleanNavigation();
    }

    public async Task<bool> GoBack()
    {
        if (!CanGoBack || _frame == null || _navigationInFlight)
        {
            return false;
        }

        _pendingNavigationParameter = null;
        // GoBack has no "requested" view model type up front the way NavigateTo does; leave
        // this null so OnFrameNavigated falls back to the resolved DataContext's runtime type.
        _pendingViewModelType = null;
        _pendingNavigationTcs = new TaskCompletionSource<bool>();
        _navigationInFlight = true;

        _frame.GoBack();

        await _pendingNavigationTcs.Task;

        return true;
    }

    /// <summary>
    /// Initialize the navigation service with the specified shell frame. 
    /// This method sets up the navigation service to use the provided frame for navigation and subscribes to the Navigated event of the frame.
    /// </summary>
    /// <param name="shellFrame"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public void Initialize(Frame shellFrame)
    {
        if (_frame == null)
        {
            ArgumentNullException.ThrowIfNull(shellFrame);

            _frame = shellFrame;
            _frame.Navigating += OnFrameNavigating;
            _frame.Navigated += OnFrameNavigated;
            _frame.NavigationStopped += OnFrameNavigationStopped;
            _frame.NavigationFailed += OnFrameNavigationFailed;
            _logger?.LogDebug("Initialized " + nameof(NavigationService));
        }
        else
        {
            _logger?.LogCritical("Initialize called twice?");
        }
    }

    public Task<bool> NavigateTo<TViewModel>(object? navigationParameter = null) where TViewModel : INotifyPropertyChanged
    {
        return NavigateTo(typeof(TViewModel), navigationParameter);
    }

    public async Task<bool> NavigateTo(Type viewModelType, object? navigationParameter = null)
    {
        // Sanity check we have a frame.
        if (_frame == null)
        {
            throw new Exception("Navigation frame has not been set!");
        }

        if (IsNavigationLocked)
        {
            // Throw exception I guess? Implementor should be checking first
            throw new Exception("NavigateTo was called but navigation is locked.");
        }

        if (_navigationInFlight)
        {
            throw new Exception("NavigateTo was called while a navigation is already in flight.");
        }

        // First resolve the page for the provided view model type so we can check if we are navigating to the same page with the same parameter.
        var pageType = _pageService.GetPageFor(viewModelType);

        // Make sure we aren't navigating to the same page with the same parameter. If we are, don't navigate and just return false.
        if (_frame.Content?.GetType() != pageType || (navigationParameter != null && !navigationParameter.Equals(_lastParameterUsed)))
        {
            var page = _pageService.GetPageInstance(viewModelType);
            // page did not set datacontext in constructor; set for them. Otherwise it would have been automatically resolved by DI.
            page.DataContext ??= _pageService.GetViewModelInstance(viewModelType);

            _logger?.LogDebug("Navigating to page of type {pageType} with datacontext {dataContext} and parameter {parameter}", pageType, viewModelType, navigationParameter);

            _pendingNavigationParameter = navigationParameter;
            _pendingViewModelType = viewModelType;
            _pendingNavigationTcs = new TaskCompletionSource<bool>();
            _navigationInFlight = true;

            var navigated = _frame.Navigate(page, navigationParameter);
            if (navigated)
            {
                _lastParameterUsed = navigationParameter;

                await _pendingNavigationTcs.Task;
            }
            else
            {
                // Navigating was cancelled synchronously (e.g. by a NavigatingCancelEventHandler
                // elsewhere) before Navigated could ever fire for it - unwind the pending state.
                _navigationInFlight = false;
                _pendingNavigationTcs = null;
            }

            _logger?.LogDebug("Navigation success: {b}", navigated);

            return navigated;
        }

        return false;
    }

    private void OnFrameNavigating(object sender, NavigatingCancelEventArgs e)
    {
        if (!_navigationInFlight || _frame == null)
        {
            return;
        }

        _pendingFromContext = _frame.GetDataContext();
        _pendingDirection = e.NavigationMode == NavigationMode.Back ? NavigationDirection.Back : NavigationDirection.Forward;
    }

    private async void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        if (!_navigationInFlight || _frame == null)
        {
            return;
        }

        var currentContext = _frame.GetDataContext();
        var viewModelType = _pendingViewModelType ?? currentContext?.GetType() ?? typeof(object);

        try
        {
            await RaiseProgressAndAwait(
                HandlePostNav(_pendingFromContext, currentContext),
                viewModelType,
                _pendingNavigationParameter,
                _pendingDirection);
        }
        finally
        {
            CompletePendingNavigation(true);
        }
    }

    private void OnFrameNavigationStopped(object sender, NavigationEventArgs e) => CompletePendingNavigation(false);

    private void OnFrameNavigationFailed(object sender, NavigationFailedEventArgs e) => CompletePendingNavigation(false);

    private void CompletePendingNavigation(bool result)
    {
        if (!_navigationInFlight)
        {
            return;
        }

        _navigationInFlight = false;
        _pendingFromContext = null;
        _pendingNavigationParameter = null;
        _pendingViewModelType = null;

        var tcs = _pendingNavigationTcs;
        _pendingNavigationTcs = null;
        tcs?.TrySetResult(result);
    }

    /// <summary>
    /// Awaits <paramref name="postNavTask"/>, raising <see cref="NavigationStarted"/>/<see cref="NavigationCompleted"/>
    /// around it only if it is still running after <see cref="NavigationIndicatorDelay"/> has elapsed. This avoids
    /// flickering a busy indicator on navigations that resolve quickly.
    /// </summary>
    private async Task RaiseProgressAndAwait(Task postNavTask, Type viewModelType, object? navigationParameter, NavigationDirection direction)
    {
        var delayTask = Task.Delay(NavigationIndicatorDelay);
        var firstCompleted = await Task.WhenAny(postNavTask, delayTask);

        if (firstCompleted == delayTask)
        {
            var args = new NavigationProgressEventArgs(viewModelType, navigationParameter, direction);
            NavigationStarted?.Invoke(this, args);

            await postNavTask;

            NavigationCompleted?.Invoke(this, args);
        }
        else
        {
            await postNavTask;
        }
    }

    private async Task HandlePostNav(object? lastContext, object? currentContext)
    {
        if (lastContext is INavigationAwareViewModel lastNavAware)
        {
            lastNavAware.OnNavigatedFrom();
        }
        if (lastContext is IAsyncNavigationAwareViewModel lastAsyncNavAware)
        {
            await lastAsyncNavAware.OnNavigatedFromAsync();
        }
        if (currentContext is INavigationAwareViewModel currentNavAware)
        {
            currentNavAware.OnNavigatedTo(_lastParameterUsed);
        }
        if (currentContext is IAsyncNavigationAwareViewModel currentAsyncNavAware)
        {
            await currentAsyncNavAware.OnNavigatedToAsync(_lastParameterUsed);
        }
    }
}
