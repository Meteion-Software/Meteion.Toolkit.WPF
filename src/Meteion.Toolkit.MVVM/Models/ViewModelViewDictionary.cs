using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Meteion.Toolkit.MVVM.Models;

/// <summary>
/// A dictionary where the key is the type of the ViewModel, and the record contains the view information.
/// </summary>
public sealed class ViewModelViewDictionary<TUIType> : Dictionary<Type, ViewModelRecord>
{
    public void Add<T_ViewModel, T_View>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where T_ViewModel : class, INotifyPropertyChanged, new()
        where T_View : TUIType
    {
        Add(typeof(T_ViewModel), new ViewModelRecord(typeof(T_View), lifetime));
    }
}
