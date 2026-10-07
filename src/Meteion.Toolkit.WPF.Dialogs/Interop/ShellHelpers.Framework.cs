using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Meteion.Toolkit.WPF.Dialogs.Interop;

/// <summary>
/// Small helpers over the CsWin32-generated shell interfaces used by the .NET Framework dialog backend.
/// </summary>
internal static class ShellHelpers
{
    /// <summary>HRESULT returned by <c>IFileDialog::Show</c> when the user cancels (HRESULT_FROM_WIN32(ERROR_CANCELLED)).</summary>
    internal const int ErrorCancelled = unchecked((int)0x800704C7);

    /// <summary>Creates an <see cref="IShellItem"/> for a file system path.</summary>
    internal static unsafe IShellItem CreateItemFromParsingName(string path)
    {
        var iid = typeof(IShellItem).GUID;
        fixed (char* pPath = path)
        {
            PInvoke.SHCreateItemFromParsingName(new PCWSTR(pPath), null, &iid, out var item).ThrowOnFailure();
            return (IShellItem)item;
        }
    }

    /// <summary>Gets the file system path of a shell item.</summary>
    internal static unsafe string GetFileSystemPath(IShellItem item)
    {
        PWSTR name = default;
        item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, &name);
        try
        {
            return name.ToString();
        }
        finally
        {
            Marshal.FreeCoTaskMem((IntPtr)name.Value);
        }
    }

    /// <summary>Gets the file system paths of every item in a shell item array.</summary>
    internal static string[] GetFileSystemPaths(IShellItemArray items)
    {
        items.GetCount(out var count);
        var paths = new string[count];
        for (uint i = 0; i < count; i++)
        {
            items.GetItemAt(i, out var item);
            paths[i] = GetFileSystemPath(item);
        }

        return paths;
    }
}
