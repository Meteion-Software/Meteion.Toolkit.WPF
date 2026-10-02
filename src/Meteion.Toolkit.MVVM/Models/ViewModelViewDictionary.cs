using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Meteion.Toolkit.MVVM.Models;

/// <summary>
/// A dictionary where the key is the type of the ViewModel, and the record contains the view information.
/// </summary>
/// <typeparam name="TUIType">The base type of the views stored, such as Page or Window.</typeparam>
/// <typeparam name="TUIType">The base type of the views stored, such as Page or Window.</typeparam>
/// <typeparam name="TUIType">The base type of the views stored, such as Page or Window.</typeparam>
/// <typeparam name="TUIType">The base type of the views stored, such as Page or Window.</typeparam>
public sealed class ViewModelViewDictionary<TUIType> : Dictionary<Type, ViewModelRecord>
{
    /// <summary>
    /// Maps a view model type to the view that displays it.
    /// </summary>
    /// <typeparam name="T_ViewModel">The view model type, used as the dictionary key.</typeparam>
    /// <typeparam name="T_View">The view type that displays the view model.</typeparam>
    /// <param name="lifetime">The service lifetime to register the view model and view with.</param>
    /// <summary>
    /// Maps a view model type to the view that displays it.
    /// </summary>
    /// <typeparam name="T_ViewModel">The view model type, used as the dictionary key.</typeparam>
    /// <typeparam name="T_View">The view type that displays the view model.</typeparam>
    /// <param name="lifetime">The service lifetime to register the view model and view with.</param>
    /// <summary>
    /// Maps a view model type to the view that displays it.
    /// </summary>
    /// <typeparam name="T_ViewModel">The view model type, used as the dictionary key.</typeparam>
    /// <typeparam name="T_View">The view type that displays the view model.</typeparam>
    /// <param name="lifetime">The service lifetime to register the view model and view with.</param>
    /// <summary>
    /// Maps a view model type to the view that displays it.
    /// </summary>
    /// <typeparam name="T_ViewModel">The view model type, used as the dictionary key.</typeparam>
    /// <typeparam name="T_View">The view type that displays the view model.</typeparam>
    /// <param name="lifetime">The service lifetime to register the view model and view with.</param>
    public void Add<T_ViewModel, T_View>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where T_ViewModel : class, INotifyPropertyChanged, new()
        where T_View : TUIType
    {
        Add(typeof(T_ViewModel), new ViewModelRecord(typeof(T_View), lifetime));
    }
}
