using System.Windows;

namespace Meteion.Toolkit.WPF.Hosting
{
    /// <summary>
    /// Base class of a WPF application that is hosted by a generic host.
    /// </summary>
    public abstract class WpfGenericHostApplication : Application, IServiceProviderApplication
    {
        /// <summary>
        /// The host that owns this application. Assigned by <see cref="WpfApplicationHost"/> before
        /// <see cref="PerformInitializeComponent"/> is called.
        /// </summary>
        public WpfApplicationHost Host { get; internal set; }

        /// <inheritdoc />
        IServiceProvider IServiceProviderApplication.Services => Host.Services;

        /// <summary>
        /// Performs the initialization of the WPF application. This method should call the <see cref="InitializeComponent"/> method to load the XAML resources.
        /// </summary>
        public abstract void PerformInitializeComponent();
    }
}
