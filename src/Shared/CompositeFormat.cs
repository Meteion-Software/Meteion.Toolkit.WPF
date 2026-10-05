using System.Globalization;

namespace Meteion.Toolkit.Localization;

/// <summary>
/// Reads the placeholders of a .NET composite format string (<c>"{0} has {1:N0} items"</c>),
/// shared, as a linked source file, by Meteion.Toolkit.Localization.KeysGenerator (which emits
/// a typed helper per formatted resx entry) and Meteion.Toolkit.Localization.Check.Core (which
/// compares placeholders across cultures and against XAML usages) - one copy, so the two agree
/// on what a placeholder is. Must stay netstandard2.0-compatible for the generator.
/// </summary>
internal static class CompositeFormat
{
    /// <summary>
    /// Finds the argument indices a composite format string uses, following the rules of
    /// <see cref="string.Format(string, object[])"/>: <c>{{</c> and <c>}}</c> are literal braces,
    /// and a placeholder is <c>{index[,alignment][:format]}</c>.
    /// </summary>
    /// <param name="value">The resx value to read.</param>
    /// <param name="indices">The distinct argument indices used, ascending. Empty when invalid.</param>
    /// <param name="error">Why the string isn't a valid format string, or <see langword="null"/> when it is.</param>
    /// <returns><see langword="true"/> when the string is a valid composite format string.</returns>
    public static bool TryAnalyze(string value, out List<int> indices, out string? error)
    {
        indices = new List<int>();
        error = null;

        var found = new SortedSet<int>();
        var i = 0;
        while (i < value.Length)
        {
            var c = value[i];

            if (c == '}')
            {
                if (i + 1 < value.Length && value[i + 1] == '}')
                {
                    i += 2;
                    continue;
                }

                error = $"unmatched '}}' at position {i}";
                return false;
            }

            if (c != '{')
            {
                i++;
                continue;
            }

            if (i + 1 < value.Length && value[i + 1] == '{')
            {
                i += 2;
                continue;
            }

            var start = i;
            i++;

            if (!TryReadNumber(value, ref i, allowSign: false, out var index))
            {
                error = $"placeholder at position {start} has no valid argument index";
                return false;
            }

            SkipSpaces(value, ref i);
            if (i < value.Length && value[i] == ',')
            {
                i++;
                SkipSpaces(value, ref i);
                if (!TryReadNumber(value, ref i, allowSign: true, out _))
                {
                    error = $"placeholder at position {start} has an invalid alignment";
                    return false;
                }

                SkipSpaces(value, ref i);
            }

            if (i < value.Length && value[i] == ':')
            {
                i++;
                while (i < value.Length && value[i] != '}')
                {
                    if (value[i] == '{')
                    {
                        error = $"placeholder at position {start} has an unescaped '{{' in its format";
                        return false;
                    }

                    i++;
                }
            }

            if (i >= value.Length || value[i] != '}')
            {
                error = $"placeholder at position {start} is not closed with '}}'";
                return false;
            }

            i++;
            found.Add(index);
        }

        indices = new List<int>(found);
        return true;
    }

    /// <summary>
    /// How many arguments a string needs: the highest index it uses plus one, or 0 when it has none.
    /// </summary>
    /// <param name="indices">The ascending indices from <see cref="TryAnalyze"/>.</param>
    public static int RequiredArgumentCount(IReadOnlyList<int> indices) =>
        indices.Count == 0 ? 0 : indices[indices.Count - 1] + 1;

    /// <summary>
    /// The indices below the highest one that the string never uses (e.g. <c>{0}</c> and <c>{2}</c>
    /// leave 1) - almost always a typo, since a skipped argument still has to be supplied.
    /// </summary>
    /// <param name="indices">The ascending indices from <see cref="TryAnalyze"/>.</param>
    public static List<int> FindGaps(IReadOnlyList<int> indices)
    {
        var gaps = new List<int>();
        var used = new HashSet<int>(indices);
        for (var i = 0; i < RequiredArgumentCount(indices); i++)
        {
            if (!used.Contains(i))
            {
                gaps.Add(i);
            }
        }

        return gaps;
    }

    /// <summary>Formats indices as <c>{0}, {1}</c> for messages; <c>none</c> when empty.</summary>
    /// <param name="indices">The indices to describe.</param>
    public static string Describe(IEnumerable<int> indices)
    {
        var parts = new List<string>();
        foreach (var index in indices)
        {
            parts.Add("{" + index.ToString(CultureInfo.InvariantCulture) + "}");
        }

        return parts.Count == 0 ? "none" : string.Join(", ", parts);
    }

    private static void SkipSpaces(string value, ref int i)
    {
        while (i < value.Length && value[i] == ' ')
        {
            i++;
        }
    }

    // Reads an ASCII integer (optionally negative) at i; false when there are no digits or it overflows.
    private static bool TryReadNumber(string value, ref int i, bool allowSign, out int number)
    {
        var start = i;
        if (allowSign && i < value.Length && value[i] == '-')
        {
            i++;
        }

        var digitsStart = i;
        while (i < value.Length && value[i] >= '0' && value[i] <= '9')
        {
            i++;
        }

        if (i == digitsStart)
        {
            number = 0;
            return false;
        }

        return int.TryParse(value.Substring(start, i - start), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out number);
    }
}
