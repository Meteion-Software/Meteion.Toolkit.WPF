using System.Windows;

namespace Meteion.Toolkit.WPF.Dialogs;

// All targets: copy our state onto the framework's dialog, show it, and copy the results back.
public abstract partial class FileDialog
{
    private protected abstract Microsoft.Win32.FileDialog CreateDialog();

    private protected virtual void ApplyTo(Microsoft.Win32.FileDialog dialog)
    {
        dialog.AddExtension = _addExtension;
        dialog.CheckFileExists = _checkFileExists;
        dialog.CheckPathExists = _checkPathExists;
        dialog.DereferenceLinks = _dereferenceLinks;
        dialog.RestoreDirectory = _restoreDirectory;
        dialog.ValidateNames = _validateNames;
        dialog.FilterIndex = _filterIndex;

        if (_defaultExt is not null)
        {
            dialog.DefaultExt = _defaultExt;
        }

        if (!string.IsNullOrEmpty(_filter))
        {
            dialog.Filter = _filter;
        }

        if (!string.IsNullOrEmpty(_initialDirectory))
        {
            dialog.InitialDirectory = _initialDirectory;
        }

        if (!string.IsNullOrEmpty(_title))
        {
            dialog.Title = _title;
        }

        if (_fileNames is { Length: > 0 } && !string.IsNullOrEmpty(_fileNames[0]))
        {
            dialog.FileName = _fileNames[0];
        }
    }

    private protected virtual void ReadFrom(Microsoft.Win32.FileDialog dialog) => SetResult(dialog.FileNames);

    private bool? ShowDialogCore(Window? owner)
    {
        var dialog = CreateDialog();
        ApplyTo(dialog);

        // Publish the selection before raising FileOk so handlers can read FileName(s).
        dialog.FileOk += (_, e) =>
        {
            ReadFrom(dialog);
            OnFileOk(e);
        };

        var result = owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
        if (result == true)
        {
            ReadFrom(dialog);
        }

        return result;
    }
}
