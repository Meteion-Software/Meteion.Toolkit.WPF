using Meteion.Toolkit.Localization.Abstractions;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Meteion.Toolkit.WPF.Localization.Resolution;

public sealed class ResourceAssemblyResolver : IResourceAssemblyResolver
{
    private readonly IXamlAssemblyResolver _xamlAssemblyResolver;
    private readonly LocalizationOptions _options;

    public ResourceAssemblyResolver(IXamlAssemblyResolver xamlAssemblyResolver, IOptions<LocalizationOptions> options)
    {
        _xamlAssemblyResolver = xamlAssemblyResolver;
        _options = options.Value;
    }

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
