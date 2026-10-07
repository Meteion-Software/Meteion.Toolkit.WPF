// Polyfill so `init` accessors and records compile on netstandard2.0.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
