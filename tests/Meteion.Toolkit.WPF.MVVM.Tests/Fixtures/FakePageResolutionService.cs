using Meteion.Toolkit.MVVM.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.MVVM.Tests.Fixtures;

/// <summary>
/// Fake IPageResolutionService so NavigationService's own logic (guard clauses,
/// short-circuiting on an already-current page, HandlePostNav) can be tested in
/// isolation without needing a real DI-backed PageResolutionService.
/// </summary>
public class FakePageResolutionService : IPageResolutionService
{
    public Type? PageTypeToReturn { get; set; }
    public Func<Type, Page>? PageInstanceFactory { get; set; }
    public object? ViewModelInstanceToReturn { get; set; }

    public void AddPage<T_ViewModel, T_View>(ServiceLifetime lifetime = ServiceLifetime.Transient)
        where T_ViewModel : INotifyPropertyChanged
        where T_View : Page
        => throw new NotImplementedException();

    public Type GetPageFor(Type viewModelType)
        => PageTypeToReturn ?? throw new InvalidOperationException("PageTypeToReturn not configured.");

    public Page GetPageInstance(Type viewModelType)
        => PageInstanceFactory?.Invoke(viewModelType) ?? throw new InvalidOperationException("PageInstanceFactory not configured.");

    public object GetViewModelInstance(Type viewModelType)
        => ViewModelInstanceToReturn ?? throw new InvalidOperationException("ViewModelInstanceToReturn not configured.");
}
