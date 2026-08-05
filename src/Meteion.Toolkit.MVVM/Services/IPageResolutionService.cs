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
    void AddPage<T_ViewModel, T_View>(ServiceLifetime lifetime = ServiceLifetime.Transient)
        where T_ViewModel : INotifyPropertyChanged
        where T_View : Page;
    Type GetPageFor(Type viewModelType);
    Page GetPageInstance(Type viewModelType);
    object GetViewModelInstance(Type viewModelType);
}
