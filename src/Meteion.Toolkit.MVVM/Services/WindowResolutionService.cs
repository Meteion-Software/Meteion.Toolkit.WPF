using Meteion.Toolkit.MVVM.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.MVVM.Services;

public class WindowResolutionService(IServiceProvider serviceProvider) : IWindowResolutionService
{
    private readonly ViewModelViewDictionary<Window> _windows = [];
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    internal WindowResolutionService(IServiceProvider serviceProvider, ViewModelViewDictionary<Window> windows)
        : this(serviceProvider)
    {
        _windows = windows;
    }

    public void AddWindow<T_ViewModel, T_View>(ServiceLifetime lifetime = ServiceLifetime.Transient)
        where T_ViewModel : INotifyPropertyChanged
        where T_View : Window
    {
        lock (_windows)
        {
            var key = typeof(T_ViewModel);
            if (_windows.ContainsKey(key))
            {
                throw new ArgumentException($"The key {key} is already configured in {nameof(WindowResolutionService)}.", nameof(key));
            }

            var type = typeof(T_View);
            if (_windows.Any(p => p.Value.PageType == type))
            {
                throw new ArgumentException($"This type is already configured with key {_windows.First(p => p.Value.PageType == type).Key}", nameof(type));
            }

            _windows.Add(key, new ViewModelRecord(type, lifetime));
        }
    }

    private Type GetWindowTypeFor(Type viewModelType)
    {
        ViewModelRecord? windowType;
        lock (_windows)
        {
            if (!_windows.TryGetValue(viewModelType, out windowType))
            {
                throw new ArgumentException($"Window not found: {viewModelType}. Did you forget to call WindowService.Configure?", nameof(viewModelType));
            }
        }
        return windowType.PageType;
    }

    /// <summary>
    /// Creates a new scope for the service provider, and fetches a window.
    /// </summary>
    public Window GetNewScopedWindowInstance(Type viewModelType)
    {
        var windowType = GetWindowTypeFor(viewModelType);
        var scope = _serviceProvider.CreateScope();
        var window = (Window)scope.ServiceProvider.GetRequiredService(windowType);

        // Dispose the scope when the window is closed to ensure scoped services live for the window's lifetime.
        void DisposeWindowScopeHandler(object? sender, EventArgs e)
        {
            window.Closed -= DisposeWindowScopeHandler;
            scope.Dispose();
        }

        window.Closed += DisposeWindowScopeHandler;

        return window;
    }
}
