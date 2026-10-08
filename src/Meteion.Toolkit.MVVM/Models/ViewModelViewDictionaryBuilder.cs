using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Meteion.Toolkit.MVVM.Models;

/// <summary>
/// Collects view model to view mappings, either explicitly or by scanning an assembly, and builds them into a
/// <see cref="ViewModelViewDictionary{TUIType}"/>.
/// </summary>
/// <typeparam name="TUIType">The base type of the views being mapped, such as Page or Window.</typeparam>
/// <summary>
/// Collects view model to view mappings, either explicitly or by scanning an assembly, and builds them into a
/// <see cref="ViewModelViewDictionary{TUIType}"/>.
/// </summary>
/// <typeparam name="TUIType">The base type of the views being mapped, such as Page or Window.</typeparam>
/// <summary>
/// Collects view model to view mappings, either explicitly or by scanning an assembly, and builds them into a
/// <see cref="ViewModelViewDictionary{TUIType}"/>.
/// </summary>
/// <typeparam name="TUIType">The base type of the views being mapped, such as Page or Window.</typeparam>
/// <summary>
/// Collects view model to view mappings, either explicitly or by scanning an assembly, and builds them into a
/// <see cref="ViewModelViewDictionary{TUIType}"/>.
/// </summary>
/// <typeparam name="TUIType">The base type of the views being mapped, such as Page or Window.</typeparam>
public class ViewModelViewDictionaryBuilder<TUIType>
{
    private readonly ViewModelViewDictionary<TUIType> _views = [];
    private readonly ILogger? _logger;

    /// <summary>
    /// Creates a builder that logs the results of assembly scans.
    /// </summary>
    /// <param name="logger">The logger that receives scan diagnostics.</param>
    /// <summary>
    /// Creates a builder that logs the results of assembly scans.
    /// </summary>
    /// <param name="logger">The logger that receives scan diagnostics.</param>
    /// <summary>
    /// Creates a builder that logs the results of assembly scans.
    /// </summary>
    /// <param name="logger">The logger that receives scan diagnostics.</param>
    /// <summary>
    /// Creates a builder that logs the results of assembly scans.
    /// </summary>
    /// <param name="logger">The logger that receives scan diagnostics.</param>
    public ViewModelViewDictionaryBuilder(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates a builder that does not log.
    /// </summary>
    /// <summary>
    /// Creates a builder that does not log.
    /// </summary>
    /// <summary>
    /// Creates a builder that does not log.
    /// </summary>
    /// <summary>
    /// Creates a builder that does not log.
    /// </summary>
    public ViewModelViewDictionaryBuilder()
    { }

    /// <summary>
    /// Maps a view model type to the view that displays it.
    /// </summary>
    /// <typeparam name="T_ViewModel">The view model type.</typeparam>
    /// <typeparam name="T_View">The view type that displays the view model.</typeparam>
    /// <param name="lifetime">The service lifetime to register the view model and view with.</param>
    /// <summary>
    /// Maps a view model type to the view that displays it.
    /// </summary>
    /// <typeparam name="T_ViewModel">The view model type.</typeparam>
    /// <typeparam name="T_View">The view type that displays the view model.</typeparam>
    /// <param name="lifetime">The service lifetime to register the view model and view with.</param>
    /// <summary>
    /// Maps a view model type to the view that displays it.
    /// </summary>
    /// <typeparam name="T_ViewModel">The view model type.</typeparam>
    /// <typeparam name="T_View">The view type that displays the view model.</typeparam>
    /// <param name="lifetime">The service lifetime to register the view model and view with.</param>
    /// <summary>
    /// Maps a view model type to the view that displays it.
    /// </summary>
    /// <typeparam name="T_ViewModel">The view model type.</typeparam>
    /// <typeparam name="T_View">The view type that displays the view model.</typeparam>
    /// <param name="lifetime">The service lifetime to register the view model and view with.</param>
    public void Add<T_ViewModel, T_View>(ServiceLifetime? lifetime = null)
        where T_ViewModel : class, INotifyPropertyChanged
        where T_View : TUIType
    {
        _views.Add(typeof(T_ViewModel), new ViewModelRecord(typeof(T_View), ViewModelLifetime.Resolve(typeof(T_ViewModel), lifetime)));
    }

    /// <summary>
    /// Scan the assembly for all concrete TUIType views that have a corresponding ViewModel and add them to the
    /// dictionary with the lifetime from the view model's <see cref="ViewModelOptionsAttribute"/>, or scoped when the
    /// attribute is absent. A ViewModel matches a view by name:
    /// - {ViewType}ViewModel for any view
    /// - {ViewType}PageViewModel when TUIType is Page
    /// - {ViewType}WindowViewModel when TUIType is Window
    /// The first match wins. Views without a match are skipped and logged as a warning.
    /// </summary>
    /// <param name="assembly">The assembly to scan for views and view models.</param>
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
                _views.Add(viewModelType, new ViewModelRecord(viewType, ViewModelLifetime.Resolve(viewModelType, null)));
                _logger?.LogInformation("Added ViewModel {ViewModelType} for view type {ViewType}", viewModelType.FullName, viewType.FullName);
            }
            else
            {
                _logger?.LogWarning("No matching ViewModel found for view type {ViewType}", viewType.FullName);
            }
        }
    }

    /// <summary>
    /// Gets the mappings collected so far.
    /// </summary>
    /// <returns>The dictionary of view model types to view records.</returns>
    /// <summary>
    /// Gets the mappings collected so far.
    /// </summary>
    /// <returns>The dictionary of view model types to view records.</returns>
    /// <summary>
    /// Gets the mappings collected so far.
    /// </summary>
    /// <returns>The dictionary of view model types to view records.</returns>
    public ViewModelViewDictionary<TUIType> Build()
    {
        return _views;
    }
}
