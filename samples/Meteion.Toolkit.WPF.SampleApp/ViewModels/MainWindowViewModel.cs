using Meteion.Toolkit.WPF.SampleApp.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Meteion.Toolkit.WPF.SampleApp.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string ScopeId { get; set; }

        public MainWindowViewModel(IScopeIdService scopeIdService)
        {
            ScopeId = scopeIdService.Id.ToString();
        }
    }
}
