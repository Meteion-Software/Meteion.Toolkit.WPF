
using CommunityToolkit.Mvvm.Input;
using Meteion.Toolkit.Localization.Abstractions;
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

    private string _selectedKey = "HomePage_WelcomeMessage";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title { get; set; } = "Home Page";

    public string ScopeId { get => _scopeIdProvider.Id.ToString(); }

    /// <summary>
    /// Resource keys the user can pick from, to drive the KeyBinding-via-ComboBox example.
    /// </summary>
    public ObservableCollection<string> AvailableKeys { get; } = new(
        ["HomePage_WelcomeMessage", "ChangeLanguage", "ScopeID", "Feature_Alpha", "Feature_Beta", "Feature_Gamma"]);

    /// <summary>
    /// Backs the DataTemplate example: each row resolves its own resource key via
    /// {lx:LocalizedValue KeyBinding={Binding Key}}, demonstrating per-item dynamic keys
    /// resolved inside an ItemsControl.ItemTemplate.
    /// </summary>
    public ObservableCollection<FeatureItem> Features { get; } = new(
        [new FeatureItem("Feature_Alpha"), new FeatureItem("Feature_Beta"), new FeatureItem("Feature_Gamma")]);

    /// <summary>
    /// Backs the KeyPrefix example: each row supplies only the short suffix ("Alpha", "Beta",
    /// "Gamma") via {lx:LocalizedValue KeyPrefix=Feature_, KeyBinding={Binding Key}}, which
    /// combines with the shared "Feature_" prefix set once in XAML to resolve the same
    /// Feature_Alpha/Beta/Gamma resx keys the DataTemplate example above uses directly.
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

    public HomePageViewModel(IScopeIdService scopeIdProvider, ILocalizationService localizationService)
    {
        _scopeIdProvider = scopeIdProvider;
        _localizationService = localizationService;
    }

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
