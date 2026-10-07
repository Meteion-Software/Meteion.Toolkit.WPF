namespace Meteion.Toolkit.Dialogs;

/// <summary>
/// Options for <see cref="IFilesystemDialogService.ShowOpenFile"/> and <see cref="IFilesystemDialogService.ShowOpenFiles"/>.
/// </summary>
public record OpenFileOptions : FileDialogOptions
{
    /// <summary>Gets a value indicating whether the dialog warns when the chosen file does not exist. Defaults to <see langword="true"/>.</summary>
    public bool CheckFileExists { get; init; } = true;
}
