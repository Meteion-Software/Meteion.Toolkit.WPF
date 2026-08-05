
using Meteion.Toolkit.WPF.SampleApp.Services;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;

namespace Meteion.Toolkit.WPF.SampleApp.ViewModels
{
    public class HomePageViewModel : INotifyPropertyChanged
    {
        private readonly IScopeIdService _scopeIdProvider;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Title { get; set; } = "Home Page";

        public string ScopeId { get => _scopeIdProvider.Id.ToString(); }

        public HomePageViewModel(IScopeIdService scopeIdProvider)
        {
            _scopeIdProvider = scopeIdProvider;
        }
    }
}
