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
/// Implements a default <see cref="INavigationService"/> for frame navigation within a window. It resolves pages from view
/// model types, invokes the navigation-aware view model callbacks, and reports slow navigations through
/// <see cref="INavigationProgress"/>.
/// </summary>
/// <param name="pageService">Resolves pages and view models from view model types.</param>
/// <param name="logger">Optional logger for navigation diagnostics.</param>
public class NavigationService(IPageResolutionService pageService, ILogger<NavigationService>? logger = null) : INavigationService, INavigationProgress
{
    private readonly IPageResolutionService _pageService = pageService;
    private readonly ILogger<NavigationService>? _logger = logger;
    private Frame? _frame;

    // The service owns the back stack rather than relying on Frame's journal, so old pages (and their view
    // models) are never rooted by the frame. Entries hold only the type and parameter, never instances; going
    // back resolves a fresh page/view model from IPageResolutionService (same instance for Scoped, new for Transient).
    private readonly Stack<BackStackEntry> _backStack = new();
    private Type? _currentViewModelType;
    private object? _currentParameter;

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

    /// <summary>A page the user can return to: the view model type that was shown and the parameter it was navigated to with.</summary>
    private sealed record BackStackEntry(Type ViewModelType, object? Parameter);

    /// <inheritdoc />
    public bool CanGoBack => _backStack.Count > 0 && !IsNavigationLocked;

    /// <inheritdoc />
    public bool IsNavigationLocked { get; set; }

    /// <inheritdoc />
    public event EventHandler<NavigationProgressEventArgs>? NavigationStarted;

    /// <inheritdoc />
    public event EventHandler<NavigationProgressEventArgs>? NavigationCompleted;

    /// <inheritdoc />
    public TimeSpan NavigationIndicatorDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <inheritdoc />
    public void CleanNavigation()
    {
        _backStack.Clear();
        _frame?.CleanNavigation();
    }

    /// <inheritdoc />
    public async Task<bool> GoBack()
    {
        if (!CanGoBack || _frame == null || _navigationInFlight)
        {
            return false;
        }

        // Resolve before popping so a resolution failure leaves the stack untouched.
        var entry = _backStack.Peek();
        var page = _pageService.GetPageInstance(entry.ViewModelType);
        page.DataContext ??= _pageService.GetViewModelInstance(entry.ViewModelType);

        _backStack.Pop();
        var leftType = _currentViewModelType;
        var leftParameter = _currentParameter;
        _currentViewModelType = entry.ViewModelType;
        _currentParameter = entry.Parameter;

        _logger?.LogDebug("Navigating back to page with datacontext {dataContext} and parameter {parameter}", entry.ViewModelType, entry.Parameter);

        BeginPendingNavigation(entry.ViewModelType, entry.Parameter, NavigationDirection.Back);

        // A normal Navigate (not Frame.GoBack) so the frame never keeps a journal; the page being left is not pushed.
        var navigated = false;
        try
        {
            navigated = _frame.Navigate(page, entry.Parameter) && await _pendingNavigationTcs!.Task;
        }
        finally
        {
            if (!navigated)
            {
                // Cancelled, stopped or failed: put the popped entry back and keep the page we are still on.
                CompletePendingNavigation(false);
                _backStack.Push(entry);
                _currentViewModelType = leftType;
                _currentParameter = leftParameter;
            }
        }

        return navigated;
    }

    /// <summary>
    /// Initialize the navigation service with the specified shell frame. 
    /// This method sets up the navigation service to use the provided frame for navigation and subscribes to the Navigated event of the frame.
    /// </summary>
    /// <param name="shellFrame">The frame to navigate.</param>
    /// <exception cref="ArgumentNullException"><paramref name="shellFrame"/> is <see langword="null"/>.</exception>
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

    /// <inheritdoc />
    public Task<bool> NavigateTo<TViewModel>(object? navigationParameter = null) where TViewModel : INotifyPropertyChanged
    {
        return NavigateTo(typeof(TViewModel), navigationParameter);
    }

    /// <inheritdoc />
    /// <exception cref="Exception">The service is not initialized, navigation is locked, or another navigation is in flight.</exception>
    public async Task<bool> NavigateTo(Type viewModelType, object? navigationParameter = null)
    {
        // Sanity check we have a frame.
        if (_frame == null)
        {
            throw new Exception("Navigation frame has not been set!");
        }

        if (IsNavigationLocked)
        {
            // Callers are expected to check IsNavigationLocked before navigating.
            throw new Exception("NavigateTo was called but navigation is locked.");
        }

        if (_navigationInFlight)
        {
            throw new Exception("NavigateTo was called while a navigation is already in flight.");
        }

        // First resolve the page for the provided view model type so we can check if we are navigating to the same page with the same parameter.
        var pageType = _pageService.GetPageFor(viewModelType);

        // Make sure we aren't navigating to the same page with the same parameter. If we are, don't navigate and just return false.
        if (_frame.Content?.GetType() != pageType || (navigationParameter != null && !navigationParameter.Equals(_currentParameter)))
        {
            var page = _pageService.GetPageInstance(viewModelType);
            // page did not set datacontext in constructor; set for them. Otherwise it would have been automatically resolved by DI.
            page.DataContext ??= _pageService.GetViewModelInstance(viewModelType);

            _logger?.LogDebug("Navigating to page of type {pageType} with datacontext {dataContext} and parameter {parameter}", pageType, viewModelType, navigationParameter);

            // Push the page being left (if any) and make the new one current up front; undone below if the navigation doesn't complete.
            var leftType = _currentViewModelType;
            var leftParameter = _currentParameter;
            if (leftType != null)
            {
                _backStack.Push(new BackStackEntry(leftType, leftParameter));
            }
            _currentViewModelType = viewModelType;
            _currentParameter = navigationParameter;

            BeginPendingNavigation(viewModelType, navigationParameter, NavigationDirection.Forward);

            var navigated = false;
            try
            {
                navigated = _frame.Navigate(page, navigationParameter) && await _pendingNavigationTcs!.Task;
            }
            finally
            {
                if (!navigated)
                {
                    // Cancelled synchronously (e.g. by a NavigatingCancelEventHandler elsewhere), stopped, or failed -
                    // Navigated never completed for it, so unwind the pending state and the stack.
                    CompletePendingNavigation(false);
                    if (leftType != null && _backStack.Count > 0)
                    {
                        _backStack.Pop();
                    }
                    _currentViewModelType = leftType;
                    _currentParameter = leftParameter;
                }
            }

            _logger?.LogDebug("Navigation success: {b}", navigated);

            return navigated;
        }

        return false;
    }

    private void BeginPendingNavigation(Type viewModelType, object? parameter, NavigationDirection direction)
    {
        _pendingNavigationParameter = parameter;
        _pendingViewModelType = viewModelType;
        _pendingDirection = direction;
        _pendingNavigationTcs = new TaskCompletionSource<bool>();
        _navigationInFlight = true;
    }

    private void OnFrameNavigating(object sender, NavigatingCancelEventArgs e)
    {
        if (!_navigationInFlight || _frame == null)
        {
            return;
        }

        // Direction is tracked by NavigateTo/GoBack: the frame always reports NavigationMode.New now that it keeps no journal.
        _pendingFromContext = _frame.GetDataContext();
    }

    private async void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        if (!_navigationInFlight || _frame == null)
        {
            return;
        }

        // The frame only ever holds the current page; drop the journal entry for the page just left so it is not rooted.
        _frame.CleanNavigation();

        var currentContext = _frame.GetDataContext();
        var viewModelType = _pendingViewModelType ?? currentContext?.GetType() ?? typeof(object);

        try
        {
            await RaiseProgressAndAwait(
                HandlePostNav(_pendingFromContext, currentContext, _pendingNavigationParameter),
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

    // Runs the leave callbacks on the old view model, then the enter callbacks on the new one. The parameter passed
    // to OnNavigatedTo is the one the destination was originally navigated with, including after GoBack.
    private async Task HandlePostNav(object? lastContext, object? currentContext, object? navigationParameter)
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
            currentNavAware.OnNavigatedTo(navigationParameter);
        }
        if (currentContext is IAsyncNavigationAwareViewModel currentAsyncNavAware)
        {
            await currentAsyncNavAware.OnNavigatedToAsync(navigationParameter);
        }
    }
}
