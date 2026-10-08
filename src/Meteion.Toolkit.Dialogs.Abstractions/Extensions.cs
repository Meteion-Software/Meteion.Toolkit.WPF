using System.Text;

namespace Meteion.Toolkit.Dialogs.Abstractions;

public static class Extensions
{
#if !NET8_0_OR_GREATER
    internal static StringBuilder AppendJoin(this StringBuilder sb, string separator, params string[] items)
    {
        bool first = true;
        for (int i = 0; i < items.Length; i++)
        {
            if (!first)
                sb.Append(separator);
            sb.Append(items[i]); // null appends nothing, matching string.Join
            first = false;
        }
        return sb;
    }
#endif
}
