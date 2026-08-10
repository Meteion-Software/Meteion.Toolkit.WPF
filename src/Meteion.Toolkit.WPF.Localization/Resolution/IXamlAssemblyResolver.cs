using System.Reflection;

namespace Meteion.Toolkit.WPF.Localization.Resolution;

/// <summary>
/// Attempts to resolve the assembly based on a XAML IServiceProvider.
/// </summary>
public interface IXamlAssemblyResolver
{
    Assembly? Resolve(IServiceProvider serviceProvider);
}
