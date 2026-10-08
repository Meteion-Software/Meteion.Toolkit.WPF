using System.Windows;
using Meteion.Toolkit.Dialogs;

namespace Meteion.Toolkit.WPF.Dialogs;

/// <summary>
/// The WPF implementation of <see cref="IFilesystemDialogService"/>. Dialogs are owned by the active window, falling back to
/// the application's main window, and must be shown from the UI thread.
/// </summary>
public sealed class DefaultFilesystemDialogService : IFilesystemDialogService
{
    /// <inheritdoc/>
    public string? ShowOpenFile(OpenFileOptions? options = null)
        => ShowOpen(options, multiselect: false)?[0];

    /// <inheritdoc/>
    public IReadOnlyList<string>? ShowOpenFiles(OpenFileOptions? options = null)
        => ShowOpen(options, multiselect: true);

    /// <inheritdoc/>
    public string? ShowSaveFile(SaveFileOptions? options = null)
    {
        options ??= new SaveFileOptions();
        var dialog = new SaveFileDialog { OverwritePrompt = options.OverwritePrompt };
        Apply(dialog, options);
        return dialog.ShowDialog(GetOwner()) == true ? dialog.FileName : null;
    }

    /// <inheritdoc/>
    public string? ShowFolder(FolderOptions? options = null)
        => ShowFolderCore(options, multiselect: false)?[0];

    /// <inheritdoc/>
    public IReadOnlyList<string>? ShowFolders(FolderOptions? options = null)
        => ShowFolderCore(options, multiselect: true);

    private static string[]? ShowOpen(OpenFileOptions? options, bool multiselect)
    {
        options ??= new OpenFileOptions();
        var dialog = new OpenFileDialog { CheckFileExists = options.CheckFileExists, Multiselect = multiselect };
        Apply(dialog, options);
        return dialog.ShowDialog(GetOwner()) == true ? dialog.FileNames : null;
    }

    private static string[]? ShowFolderCore(FolderOptions? options, bool multiselect)
    {
        options ??= new FolderOptions();
        var dialog = new FolderDialog { Multiselect = multiselect };

        if (options.Title is not null)
        {
            dialog.Title = options.Title;
        }

        if (options.InitialDirectory is not null)
        {
            dialog.InitialDirectory = options.InitialDirectory;
        }

        if (options.FolderName is not null)
        {
            dialog.FolderName = options.FolderName;
        }

        return dialog.ShowDialog(GetOwner()) == true ? dialog.FolderNames : null;
    }

    private static void Apply(FileDialog dialog, FileDialogOptions options)
    {
        if (options.Title is not null)
        {
            dialog.Title = options.Title;
        }

        if (options.InitialDirectory is not null)
        {
            dialog.InitialDirectory = options.InitialDirectory;
        }

        if (options.FileName is not null)
        {
            dialog.FileName = options.FileName;
        }

        if (options.Filter is not null)
        {
            dialog.Filter = options.Filter.ToString();
        }

        if (options.DefaultExtension is not null)
        {
            dialog.DefaultExt = options.DefaultExtension;
        }

        dialog.FilterIndex = options.FilterIndex;
    }

    private static Window? GetOwner()
    {
        var app = Application.Current;
        if (app is null)
        {
            return null;
        }

        foreach (Window window in app.Windows)
        {
            if (window.IsActive)
            {
                return window;
            }
        }

        return app.MainWindow;
    }
}
