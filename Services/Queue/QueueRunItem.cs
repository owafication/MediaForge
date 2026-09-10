using MediaForge.Models;

namespace MediaForge.Services.Queue;

public sealed class QueueRunItem
{
    public QueueRunItem(MediaJob job, ConversionOptions options)
    {
        Job = job ?? throw new ArgumentNullException(nameof(job));
        Options = options ?? throw new ArgumentNullException(nameof(options));
        Snapshot = job.CreateRunSnapshot();
    }

    public MediaJob Job { get; }
    public MediaJob Snapshot { get; }
    public ConversionOptions Options { get; }
}
