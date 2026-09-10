using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MediaForge.Models;

namespace MediaForge.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private double _overallProgress;
    private string _summaryText = "No files queued";
    private string _totalsText = string.Empty;
    private bool _isRunning;
    private string? _statusOverride;

    public MainViewModel(ReadOnlyObservableCollection<MediaJob> jobs)
    {
        Jobs = jobs ?? throw new ArgumentNullException(nameof(jobs));
        RefreshSummary();
    }

    public ReadOnlyObservableCollection<MediaJob> Jobs { get; }

    public double OverallProgress
    {
        get => _overallProgress;
        private set => SetField(ref _overallProgress, value);
    }

    public string SummaryText
    {
        get => _summaryText;
        private set => SetField(ref _summaryText, value);
    }

    public string TotalsText
    {
        get => _totalsText;
        private set => SetField(ref _totalsText, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set => SetField(ref _isRunning, value);
    }

    public void SetRunning(bool running)
    {
        IsRunning = running;
        if (!running) _statusOverride = null;
        RefreshSummary();
    }

    public void SetStatusOverride(string? message)
    {
        _statusOverride = string.IsNullOrWhiteSpace(message) ? null : message;
        RefreshSummary();
    }

    public void RefreshSummary()
    {
        var total = Jobs.Count;
        var enabled = Jobs.Count(job => job.Enabled);
        var disabled = total - enabled;
        var completed = Jobs.Count(job => job.State is JobState.Completed or JobState.CompletedWithWarnings);
        var running = Jobs.Count(job => job.State == JobState.Running);
        var failed = Jobs.Count(job => job.State is JobState.Failed or JobState.VerificationFailed);
        var skipped = Jobs.Count(job => job.State == JobState.Skipped);
        var cancelled = Jobs.Count(job => job.State == JobState.Cancelled);
        var progress = enabled == 0 ? 0 : Jobs.Where(job => job.Enabled).Sum(job => job.Progress) / enabled;

        OverallProgress = progress;
        SummaryText = _statusOverride ?? (total == 0
            ? "No files queued"
            : $"{completed} completed, {running} running, {failed} failed, {skipped} skipped, {cancelled} cancelled, {disabled} disabled");
        TotalsText = total == 0 ? string.Empty : $"{total} file(s), {enabled} enabled — {progress:0}%";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
