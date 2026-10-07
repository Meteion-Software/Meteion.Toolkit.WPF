namespace Meteion.Toolkit.Dialogs;

/// <summary>
/// Shows file and folder pickers on behalf of view models. Every method returns <see langword="null"/>
/// when the user cancels, and the full path(s) otherwise.
/// </summary>
public interface IFilesystemDialogService
{
    /// <summary>Prompts for a single file to open.</summary>
    /// <param name="options">Dialog options, or <see langword="null"/> for defaults.</param>
    /// <returns>The chosen path, or <see langword="null"/> if cancelled.</returns>
    string? ShowOpenFile(OpenFileOptions? options = null);

    /// <summary>Prompts for one or more files to open.</summary>
    /// <param name="options">Dialog options, or <see langword="null"/> for defaults.</param>
    /// <returns>The chosen paths, or <see langword="null"/> if cancelled.</returns>
    IReadOnlyList<string>? ShowOpenFiles(OpenFileOptions? options = null);

    /// <summary>Prompts for a location to save a file.</summary>
    /// <param name="options">Dialog options, or <see langword="null"/> for defaults.</param>
    /// <returns>The chosen path, or <see langword="null"/> if cancelled.</returns>
    string? ShowSaveFile(SaveFileOptions? options = null);

    /// <summary>Prompts for a single folder.</summary>
    /// <param name="options">Dialog options, or <see langword="null"/> for defaults.</param>
    /// <returns>The chosen path, or <see langword="null"/> if cancelled.</returns>
    string? ShowFolder(FolderOptions? options = null);

    /// <summary>Prompts for one or more folders.</summary>
    /// <param name="options">Dialog options, or <see langword="null"/> for defaults.</param>
    /// <returns>The chosen paths, or <see langword="null"/> if cancelled.</returns>
    IReadOnlyList<string>? ShowFolders(FolderOptions? options = null);
}
