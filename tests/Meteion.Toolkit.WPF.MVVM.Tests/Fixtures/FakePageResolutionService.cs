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

    // Per-view-model-type overrides, for tests that navigate between more than one
    // view model and so can't rely on the single fallback values above. Checked first;
    // the singular properties remain as a fallback for existing single-target tests.
    public Dictionary<Type, Type> PageTypesByViewModelType { get; } = new();
    public Dictionary<Type, Func<Page>> PageFactoriesByViewModelType { get; } = new();
    public Dictionary<Type, object> ViewModelInstancesByViewModelType { get; } = new();

    public void AddPage<T_ViewModel, T_View>(ServiceLifetime? lifetime = null)
        where T_ViewModel : INotifyPropertyChanged
        where T_View : Page
        => throw new NotImplementedException();

    public Type GetPageFor(Type viewModelType)
        => PageTypesByViewModelType.TryGetValue(viewModelType, out var pageType)
            ? pageType
            : PageTypeToReturn ?? throw new InvalidOperationException("PageTypeToReturn not configured.");

    public Page GetPageInstance(Type viewModelType)
        => PageFactoriesByViewModelType.TryGetValue(viewModelType, out var factory)
            ? factory()
            : PageInstanceFactory?.Invoke(viewModelType) ?? throw new InvalidOperationException("PageInstanceFactory not configured.");

    public object GetViewModelInstance(Type viewModelType)
        => ViewModelInstancesByViewModelType.TryGetValue(viewModelType, out var viewModel)
            ? viewModel
            : ViewModelInstanceToReturn ?? throw new InvalidOperationException("ViewModelInstanceToReturn not configured.");
}
