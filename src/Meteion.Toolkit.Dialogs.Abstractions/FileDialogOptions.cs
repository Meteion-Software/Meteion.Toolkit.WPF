namespace Meteion.Toolkit.Dialogs;

/// <summary>
/// Options shared by the open and save file dialogs.
/// </summary>
public record FileDialogOptions
{
    /// <summary>Gets the dialog title, or <see langword="null"/> for the system default.</summary>
    public string? Title { get; init; }

    /// <summary>Gets the directory the dialog starts in, or <see langword="null"/> for the system default.</summary>
    public string? InitialDirectory { get; init; }

    /// <summary>Gets the file name pre-filled in the dialog, or <see langword="null"/> for none.</summary>
    public string? FileName { get; init; }

    /// <summary>
    /// Gets the file type filter in the classic <c>"Description|*.ext;*.ext2|Description|*.*"</c> format
    /// (an even number of <c>|</c>-separated segments), or <see langword="null"/> for no filter.
    /// </summary>
    public string? Filter { get; init; }

    /// <summary>Gets the 1-based index of the initially selected filter.</summary>
    public int FilterIndex { get; init; } = 1;

    /// <summary>Gets the extension (without the leading period) appended when the user omits one.</summary>
    public string? DefaultExtension { get; init; }
}
