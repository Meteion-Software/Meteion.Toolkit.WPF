using Meteion.Toolkit.Dialogs;
using Meteion.Toolkit.WPF.Dialogs;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Meteion.Toolkit.WPF.SampleApp.Net48;

public partial class MainWindow : Window
{
    private const string TextFilter = "Text files|*.txt|All files|*.*";

    // No DI container in this sample; DialogService has no dependencies.
    private readonly IFilesystemDialogService _dialogs = new DefaultFilesystemDialogService();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OpenFile_Click(object sender, RoutedEventArgs e) =>
        Show(_dialogs.ShowOpenFile(new OpenFileOptions { Title = "Open a file", Filter = TextFilter }));

    private void OpenFiles_Click(object sender, RoutedEventArgs e) =>
        Show(_dialogs.ShowOpenFiles(new OpenFileOptions { Title = "Open files", Filter = TextFilter }));

    private void SaveFile_Click(object sender, RoutedEventArgs e) =>
        Show(_dialogs.ShowSaveFile(new SaveFileOptions
        {
            Title = "Save a file",
            Filter = TextFilter,
            DefaultExtension = "txt",
            FileName = "example.txt",
        }));

    private void PickFolder_Click(object sender, RoutedEventArgs e) =>
        Show(_dialogs.ShowFolder(new FolderOptions { Title = "Pick a folder" }));

    private void PickFolders_Click(object sender, RoutedEventArgs e) =>
        Show(_dialogs.ShowFolders(new FolderOptions { Title = "Pick folders" }));

    private void OpenFileDirect_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Open (read-only choice, rejects .exe)", ShowReadOnly = true };
        dialog.FileOk += (_, args) => args.Cancel = dialog.FileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);

        ResultBox.Text = dialog.ShowDialog(this) == true
            ? $"{dialog.FileName} (read-only chosen: {dialog.ReadOnlyChecked})"
            : "(cancelled)";
    }

    private void Show(string? path) => ResultBox.Text = path ?? "(cancelled)";

    private void Show(IReadOnlyList<string>? paths) =>
        ResultBox.Text = paths is null ? "(cancelled)" : string.Join(Environment.NewLine, paths);
}
