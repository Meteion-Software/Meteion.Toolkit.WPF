// Portions copyright (c) Sven Groot (Ookii.org) 2006
// See LICENSE.txt for details
using System.ComponentModel;
using System.Windows;

namespace Meteion.Toolkit.WPF.Dialogs;

/// <summary>
/// Base class for the open and save file dialogs.
/// </summary>
/// <remarks>
/// The public surface mirrors <see cref="Microsoft.Win32.FileDialog"/>. State is held on this object and applied to
/// the framework's own dialog (which is Vista-style on every supported target) when <see cref="ShowDialog()"/> is called.
/// </remarks>
[DefaultEvent(nameof(FileOk)), DefaultProperty(nameof(FileName))]
public abstract partial class FileDialog
{
    private const string InvalidFilterMessage =
        "Filter must be pairs of a description and a pattern separated by \"|\", e.g. \"Text files|*.txt|All files|*.*\".";

    private string[]? _fileNames;
    private string? _filter;
    private string? _defaultExt;
    private string? _initialDirectory;
    private string? _title;
    private bool _addExtension;
    private bool _checkFileExists;
    private bool _checkPathExists;
    private bool _dereferenceLinks;
    private bool _restoreDirectory;
    private bool _validateNames;
    private int _filterIndex;

    /// <summary>
    /// Raised when the user clicks Open or Save. Set <see cref="CancelEventArgs.Cancel"/> to keep the dialog open.
    /// </summary>
    [Description("Event raised when the user clicks on the Open or Save button on a file dialog box."), Category("Action")]
    public event CancelEventHandler? FileOk;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDialog"/> class.
    /// </summary>
    private protected FileDialog()
    {
        Reset();
    }

    /// <summary>
    /// Gets or sets a value indicating whether an extension is added when the user omits one. Defaults to <see langword="true"/>.
    /// </summary>
    [Category("Behavior"), DefaultValue(true)]
    public bool AddExtension
    {
        get => _addExtension;
        set => _addExtension = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dialog warns when the chosen file does not exist.
    /// </summary>
    [Category("Behavior"), DefaultValue(false)]
    public virtual bool CheckFileExists
    {
        get => _checkFileExists;
        set => _checkFileExists = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the dialog warns when the chosen path does not exist. Defaults to <see langword="true"/>.
    /// </summary>
    [Category("Behavior"), DefaultValue(true)]
    public bool CheckPathExists
    {
        get => _checkPathExists;
        set => _checkPathExists = value;
    }

    /// <summary>
    /// Gets or sets the default file name extension, without the leading period. Defaults to an empty string.
    /// </summary>
    [Category("Behavior"), DefaultValue("")]
    public string DefaultExt
    {
        get => _defaultExt ?? string.Empty;
        set
        {
            if (value is { Length: > 0 } && value[0] == '.')
            {
                value = value.Substring(1);
            }

            _defaultExt = string.IsNullOrEmpty(value) ? null : value;
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether shortcuts resolve to their targets. Defaults to <see langword="true"/>.
    /// </summary>
    [Category("Behavior"), DefaultValue(true)]
    public bool DereferenceLinks
    {
        get => _dereferenceLinks;
        set => _dereferenceLinks = value;
    }

    /// <summary>
    /// Gets or sets the file name shown in, and returned from, the dialog. Defaults to an empty string.
    /// </summary>
    [Category("Data"), DefaultValue("")]
    public string FileName
    {
        get => _fileNames is { Length: > 0 } && !string.IsNullOrEmpty(_fileNames[0]) ? _fileNames[0] : string.Empty;
        set => _fileNames = [value];
    }

    /// <summary>
    /// Gets the full paths of all files selected in the dialog.
    /// </summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string[] FileNames => _fileNames is null ? [] : (string[])_fileNames.Clone();

    /// <summary>
    /// Gets or sets the file type filter, as <c>"Description|*.ext|Description|*.*"</c>.
    /// </summary>
    /// <exception cref="ArgumentException">The filter does not have an even number of <c>|</c>-separated segments.</exception>
    [Category("Behavior"), Localizable(true), DefaultValue("")]
    public string Filter
    {
        get => _filter ?? string.Empty;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                _filter = null;
                return;
            }

            if (value.Split('|').Length % 2 != 0)
            {
                throw new ArgumentException(InvalidFilterMessage, nameof(value));
            }

            _filter = value;
        }
    }

    /// <summary>
    /// Gets or sets the 1-based index of the selected filter. Defaults to 1.
    /// </summary>
    [Category("Behavior"), DefaultValue(1)]
    public int FilterIndex
    {
        get => _filterIndex;
        set => _filterIndex = value;
    }

    /// <summary>
    /// Gets or sets the directory the dialog starts in. Defaults to an empty string.
    /// </summary>
    [Category("Data"), DefaultValue("")]
    public string InitialDirectory
    {
        get => _initialDirectory ?? string.Empty;
        set => _initialDirectory = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the current directory is restored after the dialog closes.
    /// </summary>
    [Category("Behavior"), DefaultValue(false)]
    public bool RestoreDirectory
    {
        get => _restoreDirectory;
        set => _restoreDirectory = value;
    }

    /// <summary>
    /// Gets or sets the dialog title. Defaults to an empty string, which uses the system title.
    /// </summary>
    [Category("Appearance"), DefaultValue(""), Localizable(true)]
    public string Title
    {
        get => _title ?? string.Empty;
        set => _title = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether only valid Win32 file names are accepted. Defaults to <see langword="true"/>.
    /// </summary>
    [Category("Behavior"), DefaultValue(true)]
    public bool ValidateNames
    {
        get => _validateNames;
        set => _validateNames = value;
    }

    /// <summary>
    /// Resets all properties to their default values.
    /// </summary>
    public virtual void Reset()
    {
        _fileNames = null;
        _filter = null;
        _defaultExt = null;
        _initialDirectory = null;
        _title = null;
        _filterIndex = 1;
        _addExtension = true;
        _checkFileExists = false;
        _checkPathExists = true;
        _dereferenceLinks = true;
        _restoreDirectory = false;
        _validateNames = true;
    }

    /// <summary>
    /// Shows the dialog without an explicit owner.
    /// </summary>
    /// <returns><see langword="true"/> if the user accepted the dialog; otherwise <see langword="false"/>.</returns>
    public bool? ShowDialog() => ShowDialog(null);

    /// <summary>
    /// Shows the dialog.
    /// </summary>
    /// <param name="owner">The window that owns the dialog, or <see langword="null"/> to use the active window.</param>
    /// <returns><see langword="true"/> if the user accepted the dialog; otherwise <see langword="false"/>.</returns>
    public bool? ShowDialog(Window? owner) => ShowDialogCore(owner);

    /// <summary>
    /// Raises the <see cref="FileOk"/> event.
    /// </summary>
    /// <param name="e">The event data; set <see cref="CancelEventArgs.Cancel"/> to keep the dialog open.</param>
    protected virtual void OnFileOk(CancelEventArgs e) => FileOk?.Invoke(this, e);

    /// <summary>Stores the paths chosen by the user.</summary>
    private protected void SetResult(string[]? fileNames) => _fileNames = fileNames;
}
