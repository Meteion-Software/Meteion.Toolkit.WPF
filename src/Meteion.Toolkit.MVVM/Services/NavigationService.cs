using Meteion.Toolkit.WPF;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection.Metadata;
using System.Text;
using System.Windows.Controls;

namespace Meteion.Toolkit.MVVM.Services;

/// <summary>
/// Implements a default <see cref="INavigationService"/> for frame navigation within a window. This service is responsible for managing navigation between different pages in the application, allowing for navigation to specific view models and handling back navigation.
/// </summary>
public class NavigationService(IPageResolutionService pageService, ILogger<NavigationService>? logger = null) : INavigationService
{
    private readonly IPageResolutionService _pageService = pageService;
    private readonly ILogger<NavigationService>? _logger = logger;
    private Frame? _frame;
    private object? _lastParameterUsed;

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void CleanNavigation()
    {
        _frame?.CleanNavigation();
    }

    public async Task<bool> GoBack()
    {
        if (CanGoBack && _frame != null)
        {
            var vmBeforeNavigation = _frame.GetDataContext();
            _frame.GoBack();

            await HandlePostNav(vmBeforeNavigation, _frame.GetDataContext());

            return true;
        }
        else
        {
            return false;
        }
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

        // First resolve the page for the provided view model type so we can check if we are navigating to the same page with the same parameter.
        var pageType = _pageService.GetPageFor(viewModelType);

        // Make sure we aren't navigating to the same page with the same parameter. If we are, don't navigate and just return false.
        if (_frame.Content?.GetType() != pageType || (navigationParameter != null && !navigationParameter.Equals(_lastParameterUsed)))
        {
            var page = _pageService.GetPageInstance(viewModelType);
            // page did not set datacontext in constructor; set for them. Otherwise it would have been automatically resolved by DI.
            page.DataContext ??= _pageService.GetViewModelInstance(viewModelType);

            var currentPageViewModel = _frame.GetDataContext();

            _logger?.LogDebug("Navigating to page of type {pageType} with datacontext {dataContext} and parameter {parameter}", pageType, viewModelType, navigationParameter);

            var navigated = _frame.Navigate(page, navigationParameter);
            if (navigated)
            {
                _lastParameterUsed = navigationParameter;

                await HandlePostNav(currentPageViewModel, page.DataContext);
            }

            _logger?.LogDebug("Navigation success: {b}", navigated);

            return navigated;
        }

        return false;
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
