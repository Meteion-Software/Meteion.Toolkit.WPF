namespace Meteion.Toolkit.WPF.Dialogs;

public sealed partial class OpenFileDialog
{
    private protected override Microsoft.Win32.FileDialog CreateDialog() => new Microsoft.Win32.OpenFileDialog();

    private protected override void ApplyTo(Microsoft.Win32.FileDialog dialog)
    {
        base.ApplyTo(dialog);
        var open = (Microsoft.Win32.OpenFileDialog)dialog;
        open.Multiselect = _multiselect;
        open.ShowReadOnly = _showReadOnly;
        open.ReadOnlyChecked = _readOnlyChecked;
    }

    private protected override void ReadFrom(Microsoft.Win32.FileDialog dialog)
    {
        base.ReadFrom(dialog);
        _readOnlyChecked = ((Microsoft.Win32.OpenFileDialog)dialog).ReadOnlyChecked;
    }
}
