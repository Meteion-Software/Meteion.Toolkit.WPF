using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Meteion.Toolkit.WPF.SplashScreen;

/// <summary>
/// Failure and warning sink for the splash. Until a logger is attached, entries are buffered and written to
/// <see cref="Trace"/>; attaching a logger flushes the buffer to it and uses it from then on. Thread-safe.
/// </summary>
internal sealed class SplashLog
{
    private readonly object _gate = new();
    private readonly List<(LogLevel Level, string Message, Exception? Exception)> _buffer = [];
    private ILogger? _logger;

    public SplashLog(ILogger? logger = null) => _logger = logger;

    public void Warning(string message) => Write(LogLevel.Warning, message, null);

    public void Error(string message, Exception exception) => Write(LogLevel.Error, message, exception);

    public void Attach(ILogger logger)
    {
        lock (_gate)
        {
            _logger = logger;
            foreach (var (level, message, exception) in _buffer)
            {
                Forward(logger, level, message, exception);
            }

            _buffer.Clear();
        }
    }

    private void Write(LogLevel level, string message, Exception? exception)
    {
        lock (_gate)
        {
            if (_logger is { } logger)
            {
                Forward(logger, level, message, exception);
                return;
            }

            _buffer.Add((level, message, exception));
        }

        Trace.WriteLine($"[Meteion.SplashScreen] {level}: {message}{(exception is null ? string.Empty : Environment.NewLine + exception)}");
    }

    private static void Forward(ILogger logger, LogLevel level, string message, Exception? exception)
    {
        try
        {
            logger.Log(level, exception, "{Message}", message);
        }
        catch
        {
            // A broken logger must not take the splash down.
        }
    }
}
