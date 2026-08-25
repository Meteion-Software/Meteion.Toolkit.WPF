
using CommunityToolkit.Mvvm.Input;
using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.SampleApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Meteion.Toolkit.WPF.SampleApp.ViewModels;

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
