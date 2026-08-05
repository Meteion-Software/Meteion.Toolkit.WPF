namespace Meteion.Toolkit.MVVM;

public interface INavigationAwareViewModel
{
    void OnNavigatedFrom();
    void OnNavigatedTo(object? navigationParameter);
}

public interface IAsyncNavigationAwareViewModel
{
    Task OnNavigatedFromAsync();
    Task OnNavigatedToAsync(object? navigationParameter);
}
