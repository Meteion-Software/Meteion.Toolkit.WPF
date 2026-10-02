using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows.Controls;

namespace Meteion.Toolkit.MVVM.Services;

/// <summary>
/// This service is responsible for resolving page viewmodel types to their corresponding page view types. 
/// It provides a mechanism to map viewmodels to views, allowing for dynamic resolution of pages based on the viewmodel type.
/// </summary>
public interface IPageResolutionService
{
    /// <summary>
    /// Registers a page for a view model.
    /// </summary>
    /// <typeparam name="T_ViewModel">The view model type, used as the lookup key.</typeparam>
    /// <typeparam name="T_View">The page that displays the view model.</typeparam>
    /// <param name="lifetime">The service lifetime of the page and view model.</param>
    /// <summary>
    /// Registers a page for a view model.
    /// </summary>
    /// <typeparam name="T_ViewModel">The view model type, used as the lookup key.</typeparam>
    /// <typeparam name="T_View">The page that displays the view model.</typeparam>
    /// <param name="lifetime">The service lifetime of the page and view model.</param>
    /// <summary>
    /// Registers a page for a view model.
    /// </summary>
    /// <typeparam name="T_ViewModel">The view model type, used as the lookup key.</typeparam>
    /// <typeparam name="T_View">The page that displays the view model.</typeparam>
    /// <param name="lifetime">The service lifetime of the page and view model.</param>
    void AddPage<T_ViewModel, T_View>(ServiceLifetime lifetime = ServiceLifetime.Transient)
        where T_ViewModel : INotifyPropertyChanged
        where T_View : Page;

    /// <summary>
    /// Gets the page type registered for a view model.
    /// </summary>
    /// <param name="viewModelType">The view model type to look up.</param>
    /// <returns>The page type that displays the view model.</returns>
    Type GetPageFor(Type viewModelType);

    /// <summary>
    /// Resolves a page instance from the service provider for a view model.
    /// </summary>
    /// <param name="viewModelType">The view model type whose page is required.</param>
    /// <returns>The resolved page.</returns>
    Page GetPageInstance(Type viewModelType);

    /// <summary>
    /// Resolves a view model instance from the service provider.
    /// </summary>
    /// <param name="viewModelType">The registered view model type to resolve.</param>
    /// <returns>The resolved view model.</returns>
    object GetViewModelInstance(Type viewModelType);
}
