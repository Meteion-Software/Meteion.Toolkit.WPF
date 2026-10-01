using Meteion.Toolkit.WPF.SampleApp.ViewModels;
using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.SampleApp.Views;

/// <summary>
/// Interaction logic for PlaceholderContainerPage.xaml
/// </summary>
public partial class PlaceholderContainerPage : Page
{
    public PlaceholderContainerPage(PlaceholderContainerPageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
