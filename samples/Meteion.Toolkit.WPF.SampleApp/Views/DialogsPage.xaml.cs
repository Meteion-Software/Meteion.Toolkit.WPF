using Meteion.Toolkit.WPF.SampleApp.ViewModels;
using System.Windows.Controls;

namespace Meteion.Toolkit.WPF.SampleApp.Views;

/// <summary>
/// Interaction logic for DialogsPage.xaml
/// </summary>
public partial class DialogsPage : Page
{
    public DialogsPage(DialogsPageViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
