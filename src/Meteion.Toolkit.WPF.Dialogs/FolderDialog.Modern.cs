using System.Windows;

namespace Meteion.Toolkit.WPF.Dialogs;

// .NET 8+ backend: delegates to Microsoft.Win32.OpenFolderDialog.
public sealed partial class FolderDialog
{
    private bool? ShowDialogCore(Window? owner)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Multiselect = _multiselect,
            ValidateNames = _validateNames,
            DereferenceLinks = _dereferenceLinks,
        };

        if (!string.IsNullOrEmpty(_initialDirectory))
        {
            dialog.InitialDirectory = _initialDirectory;
        }

        if (!string.IsNullOrEmpty(_title))
        {
            dialog.Title = _title;
        }

        if (_folderNames is { Length: > 0 } && !string.IsNullOrEmpty(_folderNames[0]))
        {
            dialog.FolderName = _folderNames[0];
        }

        var result = owner is null ? dialog.ShowDialog() : dialog.ShowDialog(owner);
        if (result == true)
        {
            _folderNames = dialog.FolderNames;
        }

        return result;
    }
}
