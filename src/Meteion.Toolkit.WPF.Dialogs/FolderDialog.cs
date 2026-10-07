using System.ComponentModel;
using System.Windows;

namespace Meteion.Toolkit.WPF.Dialogs;

/// <summary>
/// Prompts the user to choose one or more folders.
/// </summary>
/// <remarks>
/// The public surface mirrors <c>Microsoft.Win32.OpenFolderDialog</c> (new in .NET 8). On .NET 8+ it delegates
/// to that dialog; on .NET Framework it drives a native <c>IFileOpenDialog</c> in folder-picking mode.
/// </remarks>
[DefaultProperty(nameof(FolderName))]
public sealed partial class FolderDialog
{
    private string[]? _folderNames;
    private string? _initialDirectory;
    private string? _title;
    private bool _multiselect;
    private bool _validateNames;
    private bool _dereferenceLinks;

    /// <summary>
    /// Initializes a new instance of the <see cref="FolderDialog"/> class.
    /// </summary>
    public FolderDialog()
    {
        Reset();
    }

    /// <summary>
    /// Gets or sets the folder shown in, and returned from, the dialog. Defaults to an empty string.
    /// </summary>
    [Category("Data"), DefaultValue("")]
    public string FolderName
    {
        get => _folderNames is { Length: > 0 } && !string.IsNullOrEmpty(_folderNames[0]) ? _folderNames[0] : string.Empty;
        set => _folderNames = [value];
    }

    /// <summary>
    /// Gets the full paths of all folders selected in the dialog.
    /// </summary>
    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string[] FolderNames => _folderNames is null ? [] : (string[])_folderNames.Clone();

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
    /// Gets or sets the dialog title. Defaults to an empty string, which uses the system title.
    /// </summary>
    [Category("Appearance"), DefaultValue(""), Localizable(true)]
    public string Title
    {
        get => _title ?? string.Empty;
        set => _title = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether more than one folder can be selected. Defaults to <see langword="false"/>.
    /// </summary>
    [Category("Behavior"), DefaultValue(false)]
    public bool Multiselect
    {
        get => _multiselect;
        set => _multiselect = value;
    }

    /// <summary>
    /// Gets or sets a value indicating whether only valid Win32 names are accepted. Defaults to <see langword="true"/>.
    /// </summary>
    [Category("Behavior"), DefaultValue(true)]
    public bool ValidateNames
    {
        get => _validateNames;
        set => _validateNames = value;
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
    /// Resets all properties to their default values.
    /// </summary>
    public void Reset()
    {
        _folderNames = null;
        _initialDirectory = null;
        _title = null;
        _multiselect = false;
        _validateNames = true;
        _dereferenceLinks = true;
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
}
