// Portions copyright (c) Sven Groot (Ookii.org) 2009
// See LICENSE.txt for details
using System.ComponentModel;
using System.IO;

namespace Meteion.Toolkit.WPF.Dialogs;

/// <summary>
/// Prompts the user to choose a location to save a file.
/// </summary>
/// <remarks>
/// The public surface mirrors <see cref="Microsoft.Win32.SaveFileDialog"/>.
/// </remarks>
[Description("Prompts the user to save a file.")]
public sealed partial class SaveFileDialog : FileDialog
{
    private bool _createPrompt;
    private bool _overwritePrompt;

    /// <summary>
    /// Gets or sets a value indicating whether the dialog asks before creating a file that does not exist. Defaults to <see langword="false"/>.
    /// </summary>
    [Category("Behavior"), DefaultValue(false)]
    public bool CreatePrompt
    {
        get => _createPrompt;
        set => _createPrompt = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dialog asks before overwriting an existing file. Defaults to <see langword="true"/>.
    /// </summary>
    [Category("Behavior"), DefaultValue(true)]
    public bool OverwritePrompt
    {
        get => _overwritePrompt;
        set => _overwritePrompt = value;
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        _createPrompt = false;
        _overwritePrompt = true;
    }

    /// <summary>
    /// Opens the selected file for reading and writing, creating or truncating it.
    /// </summary>
    /// <returns>A read/write stream over <see cref="FileDialog.FileName"/>.</returns>
    public Stream OpenFile() => new FileStream(FileName, FileMode.Create, FileAccess.ReadWrite);
}
