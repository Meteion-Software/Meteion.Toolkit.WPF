using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Meteion.Toolkit.MVVM.Models;

public class ViewModelViewDictionaryBuilder<TUIType>
{
    private readonly ViewModelViewDictionary<TUIType> _views = [];
    private readonly ILogger? _logger;

    public ViewModelViewDictionaryBuilder(ILogger logger)
    {
        _logger = logger;
    }

    public ViewModelViewDictionaryBuilder()
    { }

    public void Add<T_ViewModel, T_View>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where T_ViewModel : class, INotifyPropertyChanged
        where T_View : TUIType
    {
        _views.Add(typeof(T_ViewModel), new ViewModelRecord(typeof(T_View), lifetime));
    }

    /// <summary>
    /// Scan the assembly for all types of TUIType that have a corresponding ViewModel type and add them to the dictionary. 
    /// We assume the following naming convention: 
    /// - The ViewModel type is named {ViewName}ViewModel
    /// - The View type is named {ViewName}
    /// - If TUIType is Page, then the ViewModel type is named {ViewName}ViewModel and the View type is named {ViewName}Page
    /// - If TUIType is Window, then the ViewModel type is named {ViewName}ViewModel and the View type is named {ViewName}Window
    /// </summary>
    /// <param name="assembly"></param>
    public void AddFromAssembly(Assembly assembly)
    {
        // Build a list of all valid view types
        var viewTypes = assembly.GetTypes().Where(t => typeof(TUIType).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

        _logger?.LogInformation("Found {ViewTypeCount} view types in assembly {AssemblyName}", viewTypes.Count(), assembly.FullName);

        // Now build a list of all valid viewmodel types. We will then match them up based on the naming convention.
        var viewModelTypes = assembly.GetTypes().Where(t => typeof(INotifyPropertyChanged).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

        _logger?.LogInformation("Found {ViewModelTypeCount} viewmodel types in assembly {AssemblyName}", viewModelTypes.Count(), assembly.FullName);

        // Now match up the viewmodel types with the view types based on the naming convention.
        var ruleFunc = new Func<Type, Type, bool>((viewModelType, viewType) =>
        {
            var validNames = new List<string> { $"{viewType.Name}ViewModel" };
            if (typeof(TUIType).IsAssignableTo(typeof(Page)))
            {
                validNames.Add($"{viewType.Name}PageViewModel");

            }
            else if (typeof(TUIType).IsAssignableTo(typeof(Window)))
            {
                validNames.Add($"{viewType.Name}WindowViewModel");
            }

            return validNames.Contains(viewModelType.Name);
        });

        foreach (var viewType in viewTypes)
        {
            var viewModelType = viewModelTypes.FirstOrDefault(x => ruleFunc(x, viewType));
            if (viewModelType != null)
            {
                _views.Add(viewModelType, new ViewModelRecord(viewType, ServiceLifetime.Scoped));
                _logger?.LogInformation("Added ViewModel {ViewModelType} for view type {ViewType}", viewModelType.FullName, viewType.FullName);
            }
            else
            {
                _logger?.LogWarning("No matching ViewModel found for view type {ViewType}", viewType.FullName);
            }
        }
    }

    public ViewModelViewDictionary<TUIType> Build()
    {
        return _views;
    }
}
