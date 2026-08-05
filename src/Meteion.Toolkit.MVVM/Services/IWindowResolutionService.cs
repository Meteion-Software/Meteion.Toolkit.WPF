using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;

namespace Meteion.Toolkit.WPF.MVVM.Services;

public interface IWindowResolutionService
{
    void AddWindow<T_ViewModel, T_View>(ServiceLifetime lifetime = ServiceLifetime.Transient)
        where T_ViewModel : INotifyPropertyChanged
        where T_View : Window;
    // Type GetWindowTypeFor(Type viewModelType);
    Window GetNewScopedWindowInstance(Type viewModelType);
}
