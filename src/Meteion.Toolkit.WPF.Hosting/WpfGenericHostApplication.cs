using System.Windows;

namespace Meteion.Toolkit.WPF.Hosting
{
    /// <summary>
    /// Base class of a WPF application that is hosted by a generic host.
    /// </summary>
    public abstract class WpfGenericHostApplication : Application
    {
        public WpfApplicationHost Host { get; internal set; }

        /// <summary>
        /// Performs the initialization of the WPF application. This method should call the <see cref="InitializeComponent"/> method to load the XAML resources.
        /// </summary>
        public abstract void PerformInitializeComponent();
    }
}
