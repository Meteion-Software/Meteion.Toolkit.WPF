using System.Collections;

namespace Meteion.Toolkit.WPF.Converters;

/// <summary>
/// Shared "is this value null or empty" check for the VisibleIf*NullOrEmpty converters.
/// </summary>
internal static class NullOrEmpty
{
    /// <summary>
    /// Returns <see langword="true"/> for null, an empty string, or an empty collection/sequence.
    /// Any other value (including non-enumerable objects) is considered non-empty.
    /// </summary>
    public static bool Check(object? value)
    {
        switch (value)
        {
            case null:
                return true;
            case string s:
                return s.Length == 0;
            case ICollection collection:
                return collection.Count == 0;
            case IEnumerable enumerable:
                var enumerator = enumerable.GetEnumerator();
                try
                {
                    return !enumerator.MoveNext();
                }
                finally
                {
                    (enumerator as IDisposable)?.Dispose();
                }
            default:
                return false;
        }
    }
}
