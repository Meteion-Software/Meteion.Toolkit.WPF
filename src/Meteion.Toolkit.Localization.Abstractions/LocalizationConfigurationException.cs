namespace Meteion.Toolkit.Localization.Abstractions;

public class LocalizationConfigurationException : Exception
{
    public LocalizationConfigurationException(string? message) : base(message)
    {
    }

    public LocalizationConfigurationException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
