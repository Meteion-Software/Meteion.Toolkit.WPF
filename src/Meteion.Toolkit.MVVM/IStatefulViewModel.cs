namespace Meteion.Toolkit.MVVM;

/// <summary>
/// A view model that exposes a <see cref="ViewState"/> for a <c>StatefulContainer</c> to bind to
/// (<c>State="{Binding State}"</c>).
/// </summary>
public interface IStatefulViewModel
{
    /// <summary>Gets the loading/loaded/error state of this view model.</summary>
    ViewState State { get; }
}
