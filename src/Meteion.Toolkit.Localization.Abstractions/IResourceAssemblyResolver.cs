using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Meteion.Toolkit.Localization.Abstractions;

/// <summary>
/// Determines which assembly's resources a localization lookup should read from.
/// </summary>
public interface IResourceAssemblyResolver
{
    /// <summary>
    /// Resolve an assembly with the following precedence: Explicit => XAML file assembly => configured default
    /// </summary>
    /// <param name="explicitAssembly">An assembly the caller specified directly, or null if none was given.</param>
    /// <param name="provideValueServiceProvider">
    /// The service provider passed to a markup extension's <c>ProvideValue</c>, used to find the assembly of the
    /// XAML file being processed.
    /// </param>
    /// <returns>The assembly whose resources should be searched.</returns>
    Assembly Resolve(Assembly? explicitAssembly, IServiceProvider provideValueServiceProvider);
}
