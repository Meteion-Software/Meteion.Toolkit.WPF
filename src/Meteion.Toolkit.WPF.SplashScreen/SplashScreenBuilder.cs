using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Meteion.Toolkit.WPF.SplashScreen;

/// <summary>
/// Configures and shows a splash screen. Call <see cref="Show"/> as early as possible in <c>Program.Main</c>.
/// Nothing on the path from this class through <see cref="Show"/> references a WPF type, so showing the splash
/// never causes PresentationFramework to load earlier than it otherwise would.
/// </summary>
public sealed class SplashScreenBuilder
{
    private readonly List<Action<SplashScreenOptions>> _configure = [];
    private Func<Func<Stream>>? _imageResolver;
    private ILogger? _logger;
    private bool _shown;

    /// <summary>
    /// Uses a PNG embedded in the entry assembly. The resource must have the <c>EmbeddedResource</c> build action
    /// (WPF <c>Resource</c> items and pack URIs are not supported). Matches the exact manifest name first, then a
    /// <c>.{fileName}</c> suffix. The last <c>UseImageFrom…</c> call wins.
    /// </summary>
    public SplashScreenBuilder UseImageFromEmbeddedResource(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        _imageResolver = () => SplashImageSource.ResolveEmbeddedResource(fileName, null);
        return this;
    }

    /// <inheritdoc cref="UseImageFromEmbeddedResource(string)"/>
    /// <param name="fileName">The resource name, or a unique trailing part of it.</param>
    /// <param name="assembly">The assembly holding the resource.</param>
    public SplashScreenBuilder UseImageFromEmbeddedResource(string fileName, Assembly assembly)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(assembly);
        _imageResolver = () => SplashImageSource.ResolveEmbeddedResource(fileName, assembly);
        return this;
    }

    /// <summary>
    /// Uses a PNG on disk. Relative paths resolve against <see cref="AppContext.BaseDirectory"/>, never the current
    /// working directory. The last <c>UseImageFrom…</c> call wins.
    /// </summary>
    public SplashScreenBuilder UseImageFromFilesystem(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        _imageResolver = () => SplashImageSource.ResolveFilesystem(fileName);
        return this;
    }

    /// <summary>Adjusts the options. May be called more than once; delegates run in call order.</summary>
    public SplashScreenBuilder Configure(Action<SplashScreenOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        _configure.Add(configure);
        return this;
    }

    /// <summary>Supplies a logger for failures and warnings when one exists this early.</summary>
    public SplashScreenBuilder UseLogger(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        return this;
    }

    /// <summary>
    /// Validates the configuration, starts the splash thread and returns immediately. Configuration mistakes throw
    /// from here; anything that fails afterwards is logged and turns the returned handle into a silent no-op.
    /// </summary>
    /// <exception cref="InvalidOperationException">No image was configured, the image could not be resolved, or this builder has already been shown.</exception>
    /// <exception cref="FileNotFoundException">The file given to <see cref="UseImageFromFilesystem"/> does not exist.</exception>
    public ISplashScreen Show()
    {
        if (_shown)
        {
            throw new InvalidOperationException("Show() has already been called on this builder.");
        }

        if (_imageResolver is null)
        {
            throw new InvalidOperationException($"No image was configured. Call {nameof(UseImageFromEmbeddedResource)} or {nameof(UseImageFromFilesystem)} first.");
        }

        var options = new SplashScreenOptions();
        foreach (var configure in _configure)
        {
            configure(options);
        }

        options.Validate();
        var openImage = _imageResolver();

        _shown = true;
        return SplashScreenHandle.Start(options, openImage, new SplashLog(_logger));
    }
}
