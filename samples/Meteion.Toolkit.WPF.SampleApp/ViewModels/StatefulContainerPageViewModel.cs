using CommunityToolkit.Mvvm.Input;
using Meteion.Toolkit.MVVM;
using Meteion.Toolkit.MVVM.Services;
using System.ComponentModel;

namespace Meteion.Toolkit.WPF.SampleApp.ViewModels;

/// <summary>
/// Backs the StatefulContainer example page: one <see cref="ViewState"/> shared by a Replace-mode and an
/// Overlay-mode container, with buttons to load successfully, load and fail, and retry from the error panel.
/// </summary>
public partial class StatefulContainerPageViewModel : INotifyPropertyChanged, IStatefulViewModel, INavigationAwareViewModel
{
    private readonly INavigationService _navService;
    private bool _failNextLoad;
    private string _result = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public StatefulContainerPageViewModel(INavigationService navService)
    {
        _navService = navService;
    }

    public ViewState State { get; } = new();

    public string Result
    {
        get => _result;
        private set
        {
            _result = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Result)));
        }
    }

    /// <summary>
    /// Loads successfully. Also bound as the containers' RetryCommand, so Retry after a failure succeeds.
    /// </summary>
    [RelayCommand]
    public Task Load() => State.RunAsync(
        async ct =>
        {
            await Task.Delay(TimeSpan.FromSeconds(1.5), ct);

            if (_failNextLoad)
            {
                _failNextLoad = false;
                throw new InvalidOperationException("The (simulated) server did not respond.");
            }

            Result = $"Loaded at {DateTime.Now:T}";
        },
        ex => "Couldn't load the data. Check your connection and try again."); // A real app would log ex here.

    [RelayCommand]
    public Task LoadAndFail()
    {
        _failNextLoad = true;
        return Load();
    }

    [RelayCommand]
    public Task GoBack() => _navService.GoBack();

    // RunAsync never throws for work failures, so starting the first load without awaiting it is safe.
    public void OnNavigatedTo(object? navigationParameter) => _ = Load();

    public void OnNavigatedFrom() => State.Cancel();
}
