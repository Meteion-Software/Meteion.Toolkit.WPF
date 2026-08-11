using System.Runtime.CompilerServices;

namespace Meteion.Toolkit.WPF.Localization.Tests;

internal static class ModuleSetup
{
    /// <summary>
    /// Constructing a "pack://" Uri (as XamlAssemblyResolverTests does) throws
    /// "UriFormatException: Invalid URI: Invalid port specified" for the
    /// "application:,,," authority pack URIs use, unless the "pack" scheme has
    /// already been registered with System.Uri. In a real WPF app that happens as a
    /// side effect of framework startup; in a bare test process it doesn't happen at
    /// all unless something touches PackUriHelper first. Relying on some other test
    /// incidentally doing that first is exactly the kind of test-order-dependent
    /// flakiness this module initializer exists to remove — it runs once,
    /// deterministically, before any test in this assembly.
    /// </summary>
    [ModuleInitializer]
    public static void EnsurePackUriSchemeIsRegistered()
    {
        _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
    }
}
