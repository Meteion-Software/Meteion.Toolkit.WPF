using Meteion.Toolkit.MVVM.Models;
using Meteion.Toolkit.MVVM.Services;
using Meteion.Toolkit.WPF.MVVM.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.MVVM;

/// <summary>
/// Extension methods that register the page and window resolution services, and their views and view models,
/// with an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configure the default <see cref="PageResolutionService"/>. Registers all pages and view models into the application service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="viewsBuilder">Callback that declares the page and view model mappings.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="viewsBuilder">Callback that declares the page and view model mappings.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="viewsBuilder">Callback that declares the page and view model mappings.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection UseDefaultPageResolutionService(this IServiceCollection services, Action<ViewModelViewDictionaryBuilder<Page>> viewsBuilder)
    {
        var builder = new ViewModelViewDictionaryBuilder<Page>();
        viewsBuilder.Invoke(builder);
        var views = builder.Build();

        // We add a scoped as it is scoped per window.
        services.AddScoped<IPageResolutionService, PageResolutionService>((provider) =>
        {
            var p = new PageResolutionService(provider, views);
            return p;
        });

        // Now ensure all view models and views are registered in DI
        foreach (var view in views)
        {
            services.TryAdd(new ServiceDescriptor(view.Key, view.Key, view.Value.Lifetime));
            services.TryAdd(new ServiceDescriptor(view.Value.PageType, view.Value.PageType, view.Value.Lifetime));
        }

        return services;
    }

    /// <summary>
    /// Configure the default <see cref="WindowResolutionService"/>. Registers all windows and view models into the application service collection.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="viewsBuilder">Callback that declares the window and view model mappings.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="viewsBuilder">Callback that declares the window and view model mappings.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="viewsBuilder">Callback that declares the window and view model mappings.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection UseDefaultWindowResolutionService(this IServiceCollection services, Action<ViewModelViewDictionaryBuilder<Window>> viewsBuilder)
    {
        var builder = new ViewModelViewDictionaryBuilder<Window>();
        viewsBuilder.Invoke(builder);
        var views = builder.Build();
        
        // We add this one as singleton because it handles the scope internally.
        services.AddSingleton<IWindowResolutionService, WindowResolutionService>((provider) =>
        {
            var p = new WindowResolutionService(provider, views);
            return p;
        });

        // Now ensure all view models and views are registered in DI
        foreach (var view in views)
        {
            services.TryAdd(new ServiceDescriptor(view.Key, view.Key, view.Value.Lifetime));
            services.TryAdd(new ServiceDescriptor(view.Value.PageType, view.Value.PageType, view.Value.Lifetime));
        }

        return services;
    }

    /// <summary>
    /// Registers every concrete <see cref="INotifyPropertyChanged"/> type in <paramref name="assembly"/> whose namespace
    /// matches <paramref name="namespaceRegex"/> as itself. This is for view models that have no page of their own, such as
    /// those rendered through data templates or shown in dialogs. The lifetime comes from
    /// <see cref="ViewModelOptionsAttribute"/> and is Scoped when the attribute is absent. Types that are already
    /// registered are left untouched.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="namespaceRegex">Only types whose namespace matches this expression are registered.</param>
    /// <param name="mustEndInViewModel">When true, only types whose name ends in "ViewModel" are registered.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddViewModelsFoundIn(this IServiceCollection services, Assembly assembly, Regex namespaceRegex, bool mustEndInViewModel = true)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);
        ArgumentNullException.ThrowIfNull(namespaceRegex);

        var viewModelTypes = assembly.GetTypes().Where(t =>
            typeof(INotifyPropertyChanged).IsAssignableFrom(t)
            && !t.IsAbstract
            && !t.IsInterface
            && namespaceRegex.IsMatch(t.Namespace ?? string.Empty)
            && (!mustEndInViewModel || t.Name.EndsWith("ViewModel", StringComparison.Ordinal)));

        foreach (var viewModelType in viewModelTypes)
        {
            services.TryAdd(new ServiceDescriptor(viewModelType, viewModelType, ViewModelLifetime.Resolve(viewModelType, null)));
        }

        return services;
    }

}
