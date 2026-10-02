using Meteion.Toolkit.MVVM.Models;
using Meteion.Toolkit.MVVM.Services;
using Meteion.Toolkit.WPF.MVVM.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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
            services.Add(new ServiceDescriptor(view.Key, view.Key, view.Value.Lifetime));
            services.Add(new ServiceDescriptor(view.Value.PageType, view.Value.PageType, view.Value.Lifetime));
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
            services.Add(new ServiceDescriptor(view.Key, view.Key, view.Value.Lifetime));
            services.Add(new ServiceDescriptor(view.Value.PageType, view.Value.PageType, view.Value.Lifetime));
        }

        return services;
    }

}
