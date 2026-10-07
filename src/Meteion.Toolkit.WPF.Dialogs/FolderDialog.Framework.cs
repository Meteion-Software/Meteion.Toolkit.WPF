using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Meteion.Toolkit.WPF.Dialogs.Interop;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Meteion.Toolkit.WPF.Dialogs;

// .NET Framework backend: an IFileOpenDialog in folder-picking mode (FOS_PICKFOLDERS).
public sealed partial class FolderDialog
{
    private unsafe bool? ShowDialogCore(Window? owner)
    {
        var ownerHandle = owner is null ? PInvoke.GetActiveWindow() : new HWND(new WindowInteropHelper(owner).Handle);

        var dialog = (IFileOpenDialog)new FileOpenDialog();
        try
        {
            var options = FILEOPENDIALOGOPTIONS.FOS_PICKFOLDERS | FILEOPENDIALOGOPTIONS.FOS_FORCEFILESYSTEM
                | FILEOPENDIALOGOPTIONS.FOS_PATHMUSTEXIST;
            if (_multiselect)
            {
                options |= FILEOPENDIALOGOPTIONS.FOS_ALLOWMULTISELECT;
            }

            if (!_validateNames)
            {
                options |= FILEOPENDIALOGOPTIONS.FOS_NOVALIDATE;
            }

            if (!_dereferenceLinks)
            {
                options |= FILEOPENDIALOGOPTIONS.FOS_NODEREFERENCELINKS;
            }

            dialog.SetOptions(options);

            if (!string.IsNullOrEmpty(_title))
            {
                fixed (char* p = _title)
                {
                    dialog.SetTitle(new PCWSTR(p));
                }
            }

            if (!string.IsNullOrEmpty(_initialDirectory) && Directory.Exists(_initialDirectory))
            {
                dialog.SetDefaultFolder(ShellHelpers.CreateItemFromParsingName(_initialDirectory!));
            }

            // Pre-select the current folder: open its parent and put the folder's name in the name box.
            var current = FolderName;
            if (current.Length > 0)
            {
                var parent = Path.GetDirectoryName(current.TrimEnd(Path.DirectorySeparatorChar));
                if (parent is not null && Directory.Exists(parent))
                {
                    dialog.SetFolder(ShellHelpers.CreateItemFromParsingName(parent));
                    SetName(dialog, Path.GetFileName(current.TrimEnd(Path.DirectorySeparatorChar)));
                }
                else
                {
                    SetName(dialog, current);
                }
            }

            dialog.Show(ownerHandle);

            if (_multiselect)
            {
                dialog.GetResults(out var items);
                _folderNames = ShellHelpers.GetFileSystemPaths(items);
            }
            else
            {
                dialog.GetResult(out var item);
                _folderNames = [ShellHelpers.GetFileSystemPath(item)];
            }

            return true;
        }
        catch (COMException ex) when (ex.ErrorCode == ShellHelpers.ErrorCancelled)
        {
            return false;
        }
        finally
        {
            Marshal.FinalReleaseComObject(dialog);
        }
    }

    private static unsafe void SetName(IFileOpenDialog dialog, string name)
    {
        fixed (char* p = name)
        {
            dialog.SetFileName(new PCWSTR(p));
        }
    }
}
