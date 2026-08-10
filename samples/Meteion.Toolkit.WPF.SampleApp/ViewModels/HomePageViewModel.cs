
using CommunityToolkit.Mvvm.Input;
using Meteion.Toolkit.Localization.Abstractions;
using Meteion.Toolkit.WPF.SampleApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace Meteion.Toolkit.WPF.SampleApp.ViewModels
{
    public partial class HomePageViewModel : INotifyPropertyChanged
    {
        private readonly IScopeIdService _scopeIdProvider;
        private readonly ILocalizationService _localizationService;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Title { get; set; } = "Home Page";

        public string ScopeId { get => _scopeIdProvider.Id.ToString(); }

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
}
