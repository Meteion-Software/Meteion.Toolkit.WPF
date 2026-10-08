using System.Text;

namespace Meteion.Toolkit.Dialogs.Abstractions;

public class FileDialogFilter : List<FileDialogFilterEntry>
{
    public bool IncludeFilterInTitle { get; set; } = true;

    /// <returns>
    /// Returns a string in the traditional dialog filter style.
    /// </returns>
    public override string ToString()
    {
        var sb = new StringBuilder();
        bool first = true;

        foreach (var filter in this)
        {
            if (!first)
                sb.Append('|');
            filter.AppendTo(sb, IncludeFilterInTitle);
            first = false;
        }

        return sb.ToString();
    }
}

public record struct FileDialogFilterEntry(string Title, string[] Filter)
{
    public static FileDialogFilterEntry Create(string Title, params string[] Filter) => new(Title, Filter);

    public override readonly string ToString() => ToString(true);

    public readonly string ToString(bool includeFilterInTitle = true)
    {
        var sb = new StringBuilder(Title + 64); // probably shouldn't exceed this
        AppendTo(sb, includeFilterInTitle);
        return sb.ToString();
    }

    internal readonly void AppendTo(StringBuilder sb, bool includeFilterInTitle)
    {
        sb.Append(Title);

        if (includeFilterInTitle)
        {
            sb.Append(" (")
              .AppendJoin(", ", Filter)
              .Append(')');
        }

        sb.Append('|')
          .AppendJoin(";", Filter);
    }
}
