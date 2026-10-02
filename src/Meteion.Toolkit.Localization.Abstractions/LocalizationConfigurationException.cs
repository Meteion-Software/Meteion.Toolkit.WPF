namespace Meteion.Toolkit.Localization.Abstractions;

/// <summary>
/// Thrown when localization is used incorrectly or configured with invalid values, such as a malformed
/// resx source or a key that is already qualified.
/// </summary>
public class LocalizationConfigurationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizationConfigurationException"/> class.
    /// </summary>
    /// <param name="message">A description of the configuration problem.</param>
    public LocalizationConfigurationException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizationConfigurationException"/> class.
    /// </summary>
    /// <param name="message">A description of the configuration problem.</param>
    /// <param name="innerException">The exception that caused this one, if any.</param>
    public LocalizationConfigurationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
