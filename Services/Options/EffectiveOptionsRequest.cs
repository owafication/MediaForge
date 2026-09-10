using MediaForge.Models;
using MediaForge.Models.Presets;

namespace MediaForge.Services.Options;

public sealed record EffectiveOptionsRequest(
    ConversionOptionInput Input,
    IReadOnlyCollection<MediaJob> CandidateJobs,
    PresetDocument? Preset = null,
    ConversionOptionOverrides? JobOverrides = null);
