using Microsoft.Extensions.Logging;

namespace Meteion.Toolkit.WPF.SplashScreen.Tests;

/// <summary>Collects formatted log messages.</summary>
internal sealed class ListLogger(List<string> messages) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        lock (messages)
        {
            messages.Add(formatter(state, exception));
        }
    }
}
