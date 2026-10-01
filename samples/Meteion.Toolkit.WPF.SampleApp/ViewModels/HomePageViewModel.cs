
using CommunityToolkit.Mvvm.Input;
using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.MVVM.Services;
using Meteion.Toolkit.WPF.SampleApp.Resources;
using Meteion.Toolkit.WPF.SampleApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Meteion.Toolkit.WPF.SampleApp.ViewModels;

/// <summary>
/// A single row for the DataTemplate usage example — just enough to give each row its own
/// resource key to resolve via {lx:LocalizedValue KeyBinding={Binding Key}}.
/// </summary>
public sealed record FeatureItem(string Key);

public partial class HomePageViewModel : INotifyPropertyChanged
{
    private readonly IScopeIdService _scopeIdProvider;
    private readonly ILocalizationService _localizationService;
    private readonly INavigationService _navService;

    private string _selectedKey = HomeKeys.WelcomeMessage;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title { get; set; } = "Home Page";

    public string ScopeId { get => _scopeIdProvider.Id.ToString(); }

    /// <summary>
    /// Backs the CultureAwareFormat usage example — a plain number/date, formatted
    /// according to whatever language the user has picked via SwitchLanguageCommand.
    /// </summary>
    public decimal Amount { get; } = 1234567.89m;

    public DateTime Today { get; } = DateTime.Now;

    /// <summary>
    /// Resource keys the user can pick from, to drive the KeyBinding-via-ComboBox example.
    /// Generated constants hold qualified keys, so one list can mix keys from both resx files.
    /// </summary>
    public ObservableCollection<string> AvailableKeys { get; } = new(
    [
        HomeKeys.WelcomeMessage,
        ResourcesKeys.ChangeLanguage,
        ResourcesKeys.ScopeID,
        ResourcesKeys.Feature_Alpha,
        ResourcesKeys.Feature_Beta,
        ResourcesKeys.Feature_Gamma,
    ]);

    /// <summary>
    /// Backs the DataTemplate example: each row resolves its own resource key via
    /// {lx:LocalizedValue KeyBinding={Binding Key}}, demonstrating per-item dynamic keys
    /// resolved inside an ItemsControl.ItemTemplate.
    /// </summary>
    public ObservableCollection<FeatureItem> Features { get; } = new(
        [new FeatureItem(ResourcesKeys.Feature_Alpha), new FeatureItem(ResourcesKeys.Feature_Beta), new FeatureItem(ResourcesKeys.Feature_Gamma)]);

    /// <summary>
    /// Backs the KeyPrefix example: each row supplies only the short suffix ("Alpha", "Beta",
    /// "Gamma") via {lx:LocalizedValue Source=..., KeyPrefix=Feature_, KeyBinding={Binding Key}},
    /// which combines with the shared "Feature_" prefix and the Source resx set once in XAML to
    /// resolve the same Feature_Alpha/Beta/Gamma resx keys the DataTemplate example above uses.
    /// </summary>
    public ObservableCollection<FeatureItem> FeatureSuffixes { get; } = new(
        [new FeatureItem("Alpha"), new FeatureItem("Beta"), new FeatureItem("Gamma")]);

    /// <summary>
    /// The currently selected key for the KeyBinding-via-ComboBox example. Bound
    /// two-way to a ComboBox, and read by {lx:LocalizedValue KeyBinding={Binding SelectedKey}}.
    /// </summary>
    public string SelectedKey
    {
        get => _selectedKey;
        set
        {
            if (_selectedKey == value) return;
            _selectedKey = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedKey)));
        }
    }

    public HomePageViewModel(IScopeIdService scopeIdProvider, ILocalizationService localizationService, INavigationService navService)
    {
        _scopeIdProvider = scopeIdProvider;
        _localizationService = localizationService;
        _navService = navService;
    }

    /// <summary>
    /// Navigates to SecondPage, whose OnNavigatedToAsync simulates a slow load so the navigation
    /// busy overlay (wired up in MainWindow) has something to demonstrate.
    /// </summary>
    [RelayCommand]
    public Task NavigateToSecondPage() => _navService.NavigateTo<SecondPageViewModel>();

    /// <summary>
    /// Navigates to the StatefulContainer example (loading / loaded / error states, with Retry).
    /// </summary>
    [RelayCommand]
    public Task NavigateToStatefulContainerPage() => _navService.NavigateTo<StatefulContainerPageViewModel>();

    /// <summary>
    /// Navigates to the LocalizedBinding example, a DataGrid column localized via
    /// {lx:LocalizedBinding}.
    /// </summary>
    [RelayCommand]
    public Task NavigateToLocalizedBindingPage() => _navService.NavigateTo<LocalizedBindingPageViewModel>();

    [RelayCommand]
    public void SwitchLanguage()
    {
        // Just toggle between EN and JP for now
        if (_localizationService.CurrentCulture.TwoLetterISOLanguageName == "en")
        {
            _localizationService.CurrentCulture = new System.Globalization.CultureInfo("ja-JP");
        }
        else
        {
            _localizationService.CurrentCulture = new System.Globalization.CultureInfo("en-CA");
        }
    }
}
