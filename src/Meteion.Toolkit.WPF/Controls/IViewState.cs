using System.ComponentModel;

namespace Meteion.Toolkit.WPF.Controls;

/// <summary>
/// The observable state a <see cref="StatefulContainer"/> binds to. Implemented by
/// <c>Meteion.Toolkit.MVVM.ViewState</c>, but anything that raises <see cref="INotifyPropertyChanged.PropertyChanged"/>
/// when these properties change can be used.
/// </summary>
public interface IViewState : INotifyPropertyChanged
{
    /// <summary>Gets the current state.</summary>
    ViewStatus Status { get; }

    /// <summary>Gets the user-facing error message when <see cref="Status"/> is <see cref="ViewStatus.Error"/>.</summary>
    string? ErrorMessage { get; }

    /// <summary>Gets the exception behind the error, if any. Available to custom error templates; never shown by default.</summary>
    Exception? Exception { get; }
}
