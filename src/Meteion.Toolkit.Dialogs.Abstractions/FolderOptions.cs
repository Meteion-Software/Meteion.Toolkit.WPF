namespace Meteion.Toolkit.Dialogs;

/// <summary>
/// Options for <see cref="IFilesystemDialogService.ShowFolder"/> and <see cref="IFilesystemDialogService.ShowFolders"/>.
/// </summary>
public record FolderOptions
{
    /// <summary>Gets the dialog title, or <see langword="null"/> for the system default.</summary>
    public string? Title { get; init; }

    /// <summary>Gets the directory the dialog starts in, or <see langword="null"/> for the system default.</summary>
    public string? InitialDirectory { get; init; }

    /// <summary>Gets the folder pre-selected in the dialog, or <see langword="null"/> for none.</summary>
    public string? FolderName { get; init; }
}
