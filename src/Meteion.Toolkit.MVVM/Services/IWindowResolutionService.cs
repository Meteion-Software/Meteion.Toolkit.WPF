using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;

namespace Meteion.Toolkit.WPF.MVVM.Services;

/// <summary>
/// Resolves windows from their view model types, giving each window its own service scope.
/// </summary>
public interface IWindowResolutionService
{
    /// <summary>
    /// Registers a window for a view model.
    /// </summary>
    /// <typeparam name="T_ViewModel">The view model type, used as the lookup key.</typeparam>
    /// <typeparam name="T_View">The window that displays the view model.</typeparam>
    /// <param name="lifetime">
    /// The service lifetime of the window and view model. When null, the view model's
    /// <see cref="Meteion.Toolkit.MVVM.ViewModelOptionsAttribute"/> lifetime is used if present, otherwise transient.
    /// </param>
    void AddWindow<T_ViewModel, T_View>(ServiceLifetime? lifetime = null)
        where T_ViewModel : INotifyPropertyChanged
        where T_View : Window;
    // Type GetWindowTypeFor(Type viewModelType);

    /// <summary>
    /// Creates a new service scope and resolves the window registered for a view model from it.
    /// The scope is disposed when the window closes.
    /// </summary>
    /// <param name="viewModelType">The view model type whose window is required.</param>
    /// <returns>A new window instance.</returns>

    /// <summary>
    /// Creates a new service scope and resolves the window registered for a view model from it.
    /// The scope is disposed when the window closes.
    /// </summary>
    /// <param name="viewModelType">The view model type whose window is required.</param>
    /// <returns>A new window instance.</returns>

    /// <summary>
    /// Creates a new service scope and resolves the window registered for a view model from it.
    /// The scope is disposed when the window closes.
    /// </summary>
    /// <param name="viewModelType">The view model type whose window is required.</param>
    /// <returns>A new window instance.</returns>
    Window GetNewScopedWindowInstance(Type viewModelType);
}
