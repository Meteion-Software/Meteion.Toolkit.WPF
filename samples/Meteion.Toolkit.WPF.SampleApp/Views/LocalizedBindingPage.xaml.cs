using Meteion.Toolkit.WPF.SampleApp.ViewModels;
using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.SampleApp.Views;

/// <summary>
/// Interaction logic for LocalizedBindingPage.xaml
/// </summary>
public partial class LocalizedBindingPage : Page
{
    public LocalizedBindingPage(LocalizedBindingPageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
