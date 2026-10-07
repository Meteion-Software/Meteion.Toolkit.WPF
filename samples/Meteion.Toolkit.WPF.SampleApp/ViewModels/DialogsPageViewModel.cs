using CommunityToolkit.Mvvm.Input;
using Meteion.Toolkit.Dialogs;
using Meteion.Toolkit.MVVM.Services;
using Meteion.Toolkit.WPF.Dialogs;
using System.ComponentModel;

namespace Meteion.Toolkit.WPF.SampleApp.ViewModels;

/// <summary>
/// Exercises every <see cref="IFilesystemDialogService"/> method, plus the dialog classes directly for the
/// options the service doesn't expose (read-only checkbox, FileOk veto).
/// </summary>
public partial class DialogsPageViewModel : INotifyPropertyChanged
{
    private const string TextFilter = "Text files|*.txt|All files|*.*";

    private readonly IFilesystemDialogService _dialogs;
    private readonly INavigationService _navService;

    private string _result = "(no dialog shown yet)";

    public event PropertyChangedEventHandler? PropertyChanged;

    public DialogsPageViewModel(IFilesystemDialogService dialogs, INavigationService navService)
    {
        _dialogs = dialogs;
        _navService = navService;
    }

    /// <summary>
    /// What the last dialog returned, shown in the page.
    /// </summary>
    public string Result
    {
        get => _result;
        private set
        {
            _result = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Result)));
        }
    }

    [RelayCommand]
    public void OpenFile() =>
        Result = Describe(_dialogs.ShowOpenFile(new OpenFileOptions { Title = "Open a file", Filter = TextFilter }));

    [RelayCommand]
    public void OpenFiles() =>
        Result = Describe(_dialogs.ShowOpenFiles(new OpenFileOptions { Title = "Open files", Filter = TextFilter }));

    [RelayCommand]
    public void SaveFile() =>
        Result = Describe(_dialogs.ShowSaveFile(new SaveFileOptions
        {
            Title = "Save a file",
            Filter = TextFilter,
            DefaultExtension = "txt",
            FileName = "example.txt",
        }));

    [RelayCommand]
    public void PickFolder() =>
        Result = Describe(_dialogs.ShowFolder(new FolderOptions { Title = "Pick a folder" }));

    [RelayCommand]
    public void PickFolders() =>
        Result = Describe(_dialogs.ShowFolders(new FolderOptions { Title = "Pick folders" }));

    /// <summary>
    /// Uses <see cref="OpenFileDialog"/> directly: shows the read-only choice and rejects .exe files via FileOk.
    /// </summary>
    [RelayCommand]
    public void OpenFileDirect()
    {
        var dialog = new OpenFileDialog { Title = "Open (read-only choice, rejects .exe)", ShowReadOnly = true };
        dialog.FileOk += (_, e) => e.Cancel = dialog.FileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);

        Result = dialog.ShowDialog() == true
            ? $"{dialog.FileName} (read-only chosen: {dialog.ReadOnlyChecked})"
            : "(cancelled)";
    }

    [RelayCommand]
    public Task GoBack() => _navService.GoBack();

    private static string Describe(string? path) => path ?? "(cancelled)";

    private static string Describe(IReadOnlyList<string>? paths) =>
        paths is null ? "(cancelled)" : string.Join(Environment.NewLine, paths);
}
