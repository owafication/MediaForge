namespace MediaForge.Models;

public sealed record MediaProbeInfo(
    double DurationSeconds,
    int Width,
    int Height,
    double FrameRate,
    bool HasVideo,
    bool HasAudio);
