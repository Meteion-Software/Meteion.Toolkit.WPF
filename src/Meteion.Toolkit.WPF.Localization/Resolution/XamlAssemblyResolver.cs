using System.Collections.Concurrent;
using System.Reflection;
using System.Windows.Markup;

namespace Meteion.Toolkit.WPF.Localization.Resolution;

internal sealed class XamlAssemblyResolver : IXamlAssemblyResolver
{
    private readonly ConcurrentDictionary<string, Assembly> _cache = new();

    public Assembly? Resolve(IServiceProvider serviceProvider)
    {
        // This is not a Microsoft.Extensions.DependencyInjection serviceprovider, be aware!!
        var uriContext = serviceProvider.GetService(typeof(IUriContext)) as IUriContext;
        var baseUri = uriContext?.BaseUri;

        if (baseUri != null)
        {
            var cacheKey = baseUri.ToString();
            if (_cache.TryGetValue(cacheKey, out var cachedAssembly))
            {
                return cachedAssembly;
            }

            var resolved = TryResolveAssemblyFromPackUri(baseUri);
            if (resolved != null)
            {
                _cache[cacheKey] = resolved;
                return resolved;
            }
        }

        // Fallback if we couldn't grab the base URI
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget targetProvider
            && targetProvider.TargetObject != null)
        {
            var asm = targetProvider.TargetObject.GetType().Assembly;
            // Filter out returning for SharedDp, which isn't a real element
            if (asm != typeof(System.Windows.FrameworkElement).Assembly)
            {
                return asm;
            }
        }

        return null;
    }

    private static Assembly? TryResolveAssemblyFromPackUri(Uri baseUri)
    {
        if (!baseUri.IsAbsoluteUri || baseUri.Scheme != "pack")
            return null;

        // AbsolutePath looks like: /MyAssembly;component/Views/MainWindow.xaml
        var path = baseUri.AbsolutePath.TrimStart('/');
        var componentIndex = path.IndexOf(";component", StringComparison.OrdinalIgnoreCase);

        if (componentIndex < 0)
            return null; // no assembly segment present — it's the app's own assembly's resx

        var assemblySegment = path[..componentIndex]; // e.g. "MyAssembly" or "MyAssembly;v4.0.0.0"
        var assemblyShortName = assemblySegment.Split(';')[0];

        if (string.IsNullOrWhiteSpace(assemblyShortName))
            return null;

        // Match against already-loaded assemblies rather than Assembly.Load —
        // if it's providing XAML being parsed right now, it's already loaded.
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => string.Equals(
                a.GetName().Name, assemblyShortName, StringComparison.OrdinalIgnoreCase));

        return loaded;
    }
}
