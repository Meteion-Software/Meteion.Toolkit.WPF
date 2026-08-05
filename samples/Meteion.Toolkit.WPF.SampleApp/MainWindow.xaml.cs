using Meteion.Toolkit.MVVM;
using Meteion.Toolkit.MVVM.Services;
using Meteion.Toolkit.WPF.SampleApp.Services;
using Meteion.Toolkit.WPF.SampleApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.SampleApp
{
    /// <summary>
    /// Demos a default window for MVVM navigation.
    /// </summary>
    public partial class MainWindow : Window, INavigationShellWindow
    {
        private readonly INavigationService _navService;
        private readonly IScopeIdService _scopeIdService;

        // Since this window is used as the main window for the application, if we want a datacontext, we must resolve it in the constructor. This is because the window is created by the host, and not by the IWindowResolutionService.
        public MainWindow(MainWindowViewModel viewModel, INavigationService navService, IScopeIdService scopeIdService)
        {
            DataContext = viewModel;
            _navService = navService;
            _scopeIdService = scopeIdService;
            InitializeComponent();
            _navService.Initialize(ShellFrame);
            Title = $"Main Window - ScopeId: {_scopeIdService.Id}";
        }

        public Frame GetNavigationFrame() => ShellFrame;

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Navigate to the home page when the window is loaded.
            await _navService.NavigateTo<HomePageViewModel>();
        }
    }
}