using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Meteion.Toolkit.Localization.Abstractions;

public interface IResourceAssemblyResolver
{
    /// <summary>
    /// Resolve an assembly with the following presidence: Explicit => XAML file assembly => configured default
    /// </summary>
    Assembly Resolve(Assembly? explicitAssembly, IServiceProvider provideValueServiceProvider);
}
