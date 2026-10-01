using Meteion.Toolkit.WPF.SampleApp.ViewModels;
using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.SampleApp.Views;

/// <summary>
/// Interaction logic for StatefulContainerPage.xaml
/// </summary>
public partial class StatefulContainerPage : Page
{
    public StatefulContainerPage(StatefulContainerPageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
