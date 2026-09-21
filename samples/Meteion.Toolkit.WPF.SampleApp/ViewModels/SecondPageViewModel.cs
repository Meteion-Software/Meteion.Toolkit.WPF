using CommunityToolkit.Mvvm.Input;
using Meteion.Toolkit.MVVM;
using Meteion.Toolkit.MVVM.Services;
using System.ComponentModel;

namespace Meteion.Toolkit.WPF.SampleApp.ViewModels
{
    public partial class SecondPageViewModel : INotifyPropertyChanged, IAsyncNavigationAwareViewModel
    {
        private readonly INavigationService _navService;

        public event PropertyChangedEventHandler? PropertyChanged;

        public SecondPageViewModel(INavigationService navService)
        {
            _navService = navService;
        }

        [RelayCommand]
        public Task GoBack() => _navService.GoBack();

        public void OnNavigatedFrom()
        {
        }

        public void OnNavigatedTo(object? navigationParameter)
        {
        }

        public Task OnNavigatedFromAsync() => Task.CompletedTask;

        /// <summary>
        /// Simulates a heavy load (e.g. a slow API/database call) so the navigation busy overlay has
        /// something to demonstrate.
        /// </summary>
        public Task OnNavigatedToAsync(object? navigationParameter) => Task.Delay(TimeSpan.FromSeconds(1.5));
    }
}
