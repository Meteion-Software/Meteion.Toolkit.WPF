namespace Meteion.Toolkit.WPF.Dialogs;

public sealed partial class SaveFileDialog
{
    private protected override Microsoft.Win32.FileDialog CreateDialog() => new Microsoft.Win32.SaveFileDialog();

    private protected override void ApplyTo(Microsoft.Win32.FileDialog dialog)
    {
        base.ApplyTo(dialog);
        var save = (Microsoft.Win32.SaveFileDialog)dialog;
        save.CreatePrompt = _createPrompt;
        save.OverwritePrompt = _overwritePrompt;
    }
}
