using Meteion.Toolkit.WPF.SampleApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Meteion.Toolkit.WPF.SampleApp.Views
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public partial class HomePage : Page
    {
        // In this example, we inject the HomePageViewModel into the HomePage constructor. This is optional, because it will ultimately be set by the PageResolutionService, but it is a good example of how to inject dependencies into a page.
        public HomePage(HomePageViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
