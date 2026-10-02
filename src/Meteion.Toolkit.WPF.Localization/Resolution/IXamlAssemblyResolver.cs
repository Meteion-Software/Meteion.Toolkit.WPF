using System.Reflection;

namespace Meteion.Toolkit.WPF.Localization.Resolution;

/// <summary>
/// Attempts to resolve the assembly based on a XAML IServiceProvider.
/// </summary>
public interface IXamlAssemblyResolver
{
    /// <summary>
    /// Infers the assembly that owns the XAML currently being parsed.
    /// </summary>
    /// <param name="serviceProvider">
    /// The XAML <see cref="IServiceProvider"/> passed to <c>MarkupExtension.ProvideValue</c>.
    /// </param>
    /// <returns>The owning assembly, or <see langword="null"/> when it cannot be determined.</returns>
    Assembly? Resolve(IServiceProvider serviceProvider);
}
