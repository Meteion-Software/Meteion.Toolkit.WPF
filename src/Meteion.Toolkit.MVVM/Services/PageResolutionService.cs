using Meteion.Toolkit.MVVM.Models;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows.Controls;

namespace Meteion.Toolkit.MVVM.Services;
/// <summary>
/// Implements a default <see cref="IPageResolutionService"/>.
/// </summary>
/// <param name="serviceProvider">The provider used to resolve pages and view models.</param>
/// <param name="serviceProvider">The provider used to resolve pages and view models.</param>
/// <param name="serviceProvider">The provider used to resolve pages and view models.</param>
public class PageResolutionService(IServiceProvider serviceProvider) : IPageResolutionService
{
    private readonly ViewModelViewDictionary<Page> _pages = [];
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    /// <summary>
    /// Creates the service over a pre-built set of page mappings.
    /// </summary>
    /// <param name="serviceProvider">The provider used to resolve pages and view models.</param>
    /// <param name="pages">The existing view model to page mappings.</param>
    /// <summary>
    /// Creates the service over a pre-built set of page mappings.
    /// </summary>
    /// <param name="serviceProvider">The provider used to resolve pages and view models.</param>
    /// <param name="pages">The existing view model to page mappings.</param>
    /// <summary>
    /// Creates the service over a pre-built set of page mappings.
    /// </summary>
    /// <param name="serviceProvider">The provider used to resolve pages and view models.</param>
    /// <param name="pages">The existing view model to page mappings.</param>
    internal PageResolutionService(IServiceProvider serviceProvider, ViewModelViewDictionary<Page> pages)
        : this(serviceProvider)
    {
        _pages = pages;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">The view model or the page is already registered.</exception>
    /// <inheritdoc />
    /// <exception cref="ArgumentException">The view model or the page is already registered.</exception>
    /// <inheritdoc />
    /// <exception cref="ArgumentException">The view model or the page is already registered.</exception>
    public void AddPage<T_ViewModel, T_View>(ServiceLifetime lifetime = ServiceLifetime.Transient)
        where T_ViewModel : INotifyPropertyChanged
        where T_View : Page
    {
        lock (_pages)
        {
            var key = typeof(T_ViewModel);
            if (_pages.ContainsKey(key))
            {
                throw new ArgumentException($"The key {key} is already configured in {nameof(PageResolutionService)}.", nameof(key));
            }

            var type = typeof(T_View);
            if (_pages.Any(p => p.Value.PageType == type))
            {
                throw new ArgumentException($"This type is already configured with key {_pages.First(p => p.Value.PageType == type).Key}", nameof(type));
            }

            _pages.Add(key, new ViewModelRecord(type, lifetime));
        }
    }

    /// <inheritdoc />
    public Type GetPageFor(Type viewModelType)
    {
        ViewModelRecord? pageType;
        lock (_pages)
        {
            if (!_pages.TryGetValue(viewModelType, out pageType))
            {
                throw new ArgumentException($"Page not found: {viewModelType}. Did you forget to call PageService.Configure?", nameof(viewModelType));
            }
        }

        return pageType.PageType;
    }

    /// <inheritdoc />
    /// <inheritdoc />
    /// <inheritdoc />
    public Page GetPageInstance(Type viewModelType)
    {
        var pageType = GetPageFor(viewModelType);
        return _serviceProvider.GetService(pageType) as Page ?? throw new Exception($"Could not create instance of {pageType}.");
    }

    /// <inheritdoc />
    /// <inheritdoc />
    /// <inheritdoc />
    public object GetViewModelInstance(Type viewModelType)
    {
        lock (_pages)
        {
            if (_pages.ContainsKey(viewModelType))
            {
                return _serviceProvider.GetService(viewModelType) ?? throw new Exception($"Could not create instance of {viewModelType}.");
            }
        }

        throw new ArgumentException($"ViewModel not found: {viewModelType}. Did you forget to call PageService.Configure?", nameof(viewModelType));
    }
}
