namespace Meteion.Toolkit.MVVM.Services;

public sealed class NavigationProgressEventArgs(Type viewModelType, object? navigationParameter, NavigationDirection direction) : EventArgs
{
    public Type ViewModelType { get; } = viewModelType;

    public object? NavigationParameter { get; } = navigationParameter;

    public NavigationDirection Direction { get; } = direction;
}
