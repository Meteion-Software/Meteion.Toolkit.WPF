namespace Meteion.Toolkit.Dialogs;

/// <summary>
/// Options for <see cref="IFilesystemDialogService.ShowSaveFile"/>.
/// </summary>
public record SaveFileOptions : FileDialogOptions
{
    /// <summary>Gets a value indicating whether the dialog warns before overwriting an existing file. Defaults to <see langword="true"/>.</summary>
    public bool OverwritePrompt { get; init; } = true;
}
