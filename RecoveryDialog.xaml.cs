using System.Windows;
using System.Windows.Controls;
using System.IO;
using MediaForge.Models.Projects;
using MediaForge.Services.Runtime;

namespace MediaForge;

public enum RecoveryDialogAction
{
    Later,
    Recover,
    OpenReadOnly,
    Discard
}

public partial class RecoveryDialog : Window
{
    public RecoveryDialog(IReadOnlyList<RecoverySnapshotInfo> snapshots)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        InitializeComponent();
        SnapshotsList.ItemsSource = snapshots;
        SnapshotsList.SelectionChanged += SnapshotsList_SelectionChanged;
        if (snapshots.Count > 0) SnapshotsList.SelectedIndex = 0;
    }

    public RecoveryDialogAction Action { get; private set; } = RecoveryDialogAction.Later;
    public RecoverySnapshotInfo? SelectedSnapshot => SnapshotsList.SelectedItem as RecoverySnapshotInfo;

    private void SnapshotsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = SelectedSnapshot;
        DetailsText.Text = selected is null
            ? "Select a recovery snapshot."
            : $"Snapshot: {selected.SnapshotPath}{Environment.NewLine}" +
              $"Canonical project: {selected.Snapshot.CanonicalProjectPath ?? "Unsaved project"}{Environment.NewLine}" +
              $"Queue items: {selected.Snapshot.Document.Queue.Count}";
    }

    private void Recover_Click(object sender, RoutedEventArgs e) => Complete(RecoveryDialogAction.Recover);
    private void OpenReadOnly_Click(object sender, RoutedEventArgs e) => Complete(RecoveryDialogAction.OpenReadOnly);
    private void Discard_Click(object sender, RoutedEventArgs e) => Complete(RecoveryDialogAction.Discard);

    private void Later_Click(object sender, RoutedEventArgs e)
    {
        Action = RecoveryDialogAction.Later;
        DialogResult = false;
    }

    private async void Inspect_Click(object sender, RoutedEventArgs e)
    {
        var selected = SelectedSnapshot;
        if (selected is null) return;
        try
        {
            var result = await new ProcessRunner().RunAsync(new ProcessRunRequest
            {
                FileName = "explorer.exe",
                Arguments = [Path.GetDirectoryName(selected.SnapshotPath)!],
                StandardOutputTailLineLimit = 5,
                StandardErrorTailLineLimit = 5
            });
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.StandardError)
                    ? $"Explorer exited with code {result.ExitCode}."
                    : result.StandardError);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open recovery folder", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Complete(RecoveryDialogAction action)
    {
        if (SelectedSnapshot is null) return;
        Action = action;
        DialogResult = true;
    }
}
