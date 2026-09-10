using MediaForge.Models;

namespace MediaForge.Services.Options;

public sealed record EffectiveOptionsResult(
    ConversionOptions? Options,
    int ParallelJobs,
    string Error)
{
    public bool Success => Options is not null;
    public IReadOnlyDictionary<string, EffectiveOptionSource> Sources { get; init; } =
        new Dictionary<string, EffectiveOptionSource>(StringComparer.Ordinal);
    public IReadOnlySet<string> LockedFields { get; init; } = new HashSet<string>(StringComparer.Ordinal);

    public static EffectiveOptionsResult Failure(string error) => new(null, 0, error);

    public static EffectiveOptionsResult Resolved(
        ConversionOptions options,
        int parallelJobs,
        IReadOnlyDictionary<string, EffectiveOptionSource>? sources = null,
        IReadOnlySet<string>? lockedFields = null) =>
        new(options, parallelJobs, string.Empty)
        {
            Sources = sources ?? new Dictionary<string, EffectiveOptionSource>(StringComparer.Ordinal),
            LockedFields = lockedFields ?? new HashSet<string>(StringComparer.Ordinal)
        };
}
