using Meteion.Toolkit.MVVM;
using Meteion.Toolkit.MVVM.Services;
using Meteion.Toolkit.WPF.SampleApp.Services;
using Meteion.Toolkit.WPF.SampleApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.SampleApp;

/// <summary>
/// Demos a default window for MVVM navigation.
/// </summary>
public partial class MainWindow : Window, INavigationShellWindow
{
    private readonly INavigationService _navService;
    private readonly IScopeIdService _scopeIdService;

    /// <summary>
    /// Drives the navigation loading overlay. Starts Loaded so the shell is not covered before the first navigation.
    /// </summary>
    public ViewState NavigationState { get; } = CreateLoadedState();

    // Since this window is used as the main window for the application, if we want a datacontext, we must resolve it in the constructor. This is because the window is created by the host, and not by the IWindowResolutionService.
    public MainWindow(MainWindowViewModel viewModel, INavigationService navService, IScopeIdService scopeIdService)
    {
        DataContext = viewModel;
        _navService = navService;
        _scopeIdService = scopeIdService;
        InitializeComponent();
        _navService.Initialize(ShellFrame);
        Title = $"Main Window - ScopeId: {_scopeIdService.Id}";

        // NavigationService also implements INavigationProgress; drive the container's state from it so
        // slow OnNavigatedToAsync calls (anything over NavigationIndicatorDelay) show a spinner.
        if (_navService is INavigationProgress navProgress)
        {
            navProgress.NavigationStarted += (_, _) => NavigationState.SetLoading();
            navProgress.NavigationCompleted += (_, _) => NavigationState.SetLoaded();
        }
    }

    private static ViewState CreateLoadedState()
    {
        var state = new ViewState();
        state.SetLoaded();
        return state;
    }

    public Frame GetNavigationFrame() => ShellFrame;

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // Navigate to the home page when the window is loaded.
        await _navService.NavigateTo<HomePageViewModel>();

        // Bind the window title to the shell frame title
        
    }
}
