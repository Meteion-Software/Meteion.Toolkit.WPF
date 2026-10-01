namespace Meteion.Toolkit.WPF;

/// <summary>
/// Implemented by a WPF <see cref="System.Windows.Application"/> that owns the app's
/// <see cref="IServiceProvider"/>. Lets toolkit libraries (e.g. localization) reach the app's
/// services through <see cref="System.Windows.Application.Current"/> without depending on how the
/// application is hosted. <c>WpfGenericHostApplication</c> implements it; an application with its
/// own composition root can implement it directly.
/// </summary>
public interface IServiceProviderApplication
{
    /// <summary>The application's service provider.</summary>
    IServiceProvider Services { get; }
}
