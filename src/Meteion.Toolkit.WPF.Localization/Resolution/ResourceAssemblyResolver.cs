using Meteion.Toolkit.Localization.Abstractions;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Meteion.Toolkit.WPF.Localization.Resolution;

/// <summary>
/// Picks the resource assembly for a markup-extension usage, in priority order: an explicit
/// assembly, the assembly inferred from the XAML context, then
/// <see cref="LocalizationOptions.DefaultAssembly"/>.
/// </summary>
public sealed class ResourceAssemblyResolver : IResourceAssemblyResolver
{
    private readonly IXamlAssemblyResolver _xamlAssemblyResolver;
    private readonly LocalizationOptions _options;

    /// <summary>
    /// Creates the resolver with its XAML-context fallback and the configured defaults.
    /// </summary>
    /// <param name="xamlAssemblyResolver">Infers the assembly from the XAML being parsed.</param>
    /// <param name="options">Localization options supplying <c>DefaultAssembly</c>.</param>
    public ResourceAssemblyResolver(IXamlAssemblyResolver xamlAssemblyResolver, IOptions<LocalizationOptions> options)
    {
        _xamlAssemblyResolver = xamlAssemblyResolver;
        _options = options.Value;
    }

    /// <inheritdoc />
    /// <exception cref="LocalizationConfigurationException">No assembly could be resolved from any source.</exception>
    public Assembly Resolve(Assembly? explicitAssembly, IServiceProvider provideValueServiceProvider)
    {
        return explicitAssembly
        ?? _xamlAssemblyResolver.Resolve(provideValueServiceProvider)
        ?? _options.DefaultAssembly
        ?? throw new LocalizationConfigurationException(
               "Could not resolve a resource assembly: no explicit Assembly was set, " +
               "the XAML context couldn't be inferred, and no LocalizationOptions.DefaultAssembly is configured.");
    }
}
