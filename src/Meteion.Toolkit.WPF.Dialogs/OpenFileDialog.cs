// Portions copyright (c) Sven Groot (Ookii.org) 2006
// See LICENSE.txt for details
using System.ComponentModel;
using System.IO;

namespace Meteion.Toolkit.WPF.Dialogs;

/// <summary>
/// Prompts the user to open one or more files.
/// </summary>
/// <remarks>
/// The public surface mirrors <see cref="Microsoft.Win32.OpenFileDialog"/>.
/// </remarks>
[Description("Prompts the user to open a file.")]
public sealed partial class OpenFileDialog : FileDialog
{
    private bool _multiselect;
    private bool _showReadOnly;
    private bool _readOnlyChecked;

    /// <inheritdoc/>
    /// <value>Defaults to <see langword="true"/> for the open dialog.</value>
    [DefaultValue(true)]
    public override bool CheckFileExists
    {
        get => base.CheckFileExists;
        set => base.CheckFileExists = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether more than one file can be selected. Defaults to <see langword="false"/>.
    /// </summary>
    [Category("Behavior"), DefaultValue(false)]
    public bool Multiselect
    {
        get => _multiselect;
        set => _multiselect = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dialog offers an "Open as read-only" choice. Defaults to <see langword="false"/>.
    /// </summary>
    [Category("Behavior"), DefaultValue(false)]
    public bool ShowReadOnly
    {
        get => _showReadOnly;
        set => _showReadOnly = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the read-only choice is selected; after the dialog closes, whether the user chose it.
    /// </summary>
    [Category("Behavior"), DefaultValue(false)]
    public bool ReadOnlyChecked
    {
        get => _readOnlyChecked;
        set => _readOnlyChecked = value;
    }

    /// <inheritdoc/>
    public override void Reset()
    {
        base.Reset();
        CheckFileExists = true;
        _multiselect = false;
        _showReadOnly = false;
        _readOnlyChecked = false;
    }

    /// <summary>
    /// Opens the selected file for reading.
    /// </summary>
    /// <returns>A read-only stream over <see cref="FileDialog.FileName"/>.</returns>
    public Stream OpenFile() => new FileStream(FileName, FileMode.Open, FileAccess.Read);
}
