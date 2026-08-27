using System.ComponentModel;
using System.Windows.Controls;

namespace Meteion.Toolkit.MVVM.Services;

public interface INavigationService
{
    bool IsNavigationLocked { get; set; }
    bool CanGoBack { get; }

    void Initialize(Frame shellFrame);

    Task<bool> NavigateTo<TViewModel>(object? navigationParameter = null)
        where TViewModel : INotifyPropertyChanged;

    Task<bool> NavigateTo(Type viewModel, object? navigationParameter = null);

    Task<bool> GoBack();

    void CleanNavigation();
}
