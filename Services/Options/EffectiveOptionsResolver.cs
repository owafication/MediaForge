using System.Globalization;
using System.IO;
using MediaForge.Models;
using MediaForge.Services;

namespace MediaForge.Services.Options;

public sealed class EffectiveOptionsResolver : IEffectiveOptionsResolver
{
    public EffectiveOptionsResult Resolve(EffectiveOptionsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Input);
        ArgumentNullException.ThrowIfNull(request.CandidateJobs);

        var lockedFields = new HashSet<string>(request.Preset?.LockedFields ?? [], StringComparer.Ordinal);
        var overrideKeys = request.JobOverrides?.GetDefinedKeys() ?? new HashSet<string>(StringComparer.Ordinal);
        var lockedConflicts = overrideKeys.Where(lockedFields.Contains).OrderBy(key => key, StringComparer.Ordinal).ToList();
        if (lockedConflicts.Count > 0)
        {
            return EffectiveOptionsResult.Failure(
                $"The selected preset locks these fields: {string.Join(", ", lockedConflicts)}. Duplicate or unlock the preset before applying job overrides.");
        }

        if (request.Preset is { MediaKinds.Count: > 0 } preset &&
            request.CandidateJobs.Any(job => !preset.MediaKinds.Contains(job.Kind)))
        {
            return EffectiveOptionsResult.Failure($"Preset '{preset.Name}' does not apply to every selected media type.");
        }

        var sources = CreateGlobalSources();
        var input = request.Input;
        if (request.Preset is not null)
        {
            input = request.Preset.Values.ApplyTo(input);
            foreach (var key in request.Preset.Values.GetDefinedKeys()) sources[key] = EffectiveOptionSource.Preset;
        }
        if (request.JobOverrides is not null)
        {
            input = request.JobOverrides.ApplyTo(input);
            foreach (var key in request.JobOverrides.GetDefinedKeys()) sources[key] = EffectiveOptionSource.JobOverride;
        }

        var ffmpegPath = input.FfmpegPath.Trim();
        if (!File.Exists(ffmpegPath)) return EffectiveOptionsResult.Failure("Choose a valid ffmpeg.exe path.");
        var ffprobePath = FfmpegLocator.FindFfprobe(ffmpegPath);
        if (ffprobePath is null) return EffectiveOptionsResult.Failure("ffprobe.exe must be in the same folder as ffmpeg.exe.");

        var outputFolder = input.OutputFolder.Trim();
        if (!input.OutputBesideSource)
        {
            if (string.IsNullOrWhiteSpace(outputFolder))
            {
                return EffectiveOptionsResult.Failure("Choose an output folder or enable saving beside each source.");
            }

            try
            {
                Directory.CreateDirectory(outputFolder);
            }
            catch (Exception ex)
            {
                return EffectiveOptionsResult.Failure($"The output folder cannot be used: {ex.Message}");
            }
        }

        if (!TryParsePositiveInt(input.ImageWidth, out var imageWidth) ||
            !TryParsePositiveInt(input.ImageHeight, out var imageHeight) ||
            !TryParsePositiveInt(input.ImageScalePercent, out var imagePercent))
        {
            return EffectiveOptionsResult.Failure("Image width, height and scale percentage must be positive whole numbers.");
        }

        if (!TryParsePositiveInt(input.VideoWidth, out var videoWidth) ||
            !TryParsePositiveInt(input.VideoHeight, out var videoHeight) ||
            !TryParsePositiveDouble(input.VideoCustomFps, out var videoFps))
        {
            return EffectiveOptionsResult.Failure("Video dimensions must be positive whole numbers and custom frame rate must be a positive number.");
        }

        if (!TryParsePositiveInt(input.VideoAudioBitrate, out var videoAudioBitrate) ||
            !TryParsePositiveInt(input.AudioBitrate, out var audioBitrate) ||
            videoAudioBitrate > 512 || audioBitrate > 512)
        {
            return EffectiveOptionsResult.Failure("Audio bitrate values must be whole numbers from 1 to 512 kbps.");
        }

        if (imagePercent > 10000 || videoFps > 240)
        {
            return EffectiveOptionsResult.Failure("Image scale cannot exceed 10000% and custom video frame rate cannot exceed 240 FPS.");
        }

        if (videoWidth > 16384 || videoHeight > 16384 || imageWidth > 100000 || imageHeight > 100000)
        {
            return EffectiveOptionsResult.Failure("Requested dimensions exceed this version's supported limits.");
        }

        if (input.ImageAspectRatio == "Custom" &&
            (!TryParsePositiveDouble(input.ImageCustomAspectWidth, out var imageAspectWidth) ||
             !TryParsePositiveDouble(input.ImageCustomAspectHeight, out var imageAspectHeight) ||
             !IsFiniteRatio(imageAspectWidth, imageAspectHeight)))
        {
            return EffectiveOptionsResult.Failure("Custom image aspect ratio values must be positive numbers.");
        }

        if (input.VideoAspectRatio == "Custom" &&
            (!TryParsePositiveDouble(input.VideoCustomAspectWidth, out var videoAspectWidth) ||
             !TryParsePositiveDouble(input.VideoCustomAspectHeight, out var videoAspectHeight) ||
             !IsFiniteRatio(videoAspectWidth, videoAspectHeight)))
        {
            return EffectiveOptionsResult.Failure("Custom video aspect ratio values must be positive numbers.");
        }

        var options = new ConversionOptions
        {
            FfmpegPath = ffmpegPath,
            FfprobePath = ffprobePath,
            OutputBesideSource = input.OutputBesideSource,
            OutputFolder = outputFolder,
            PreserveFolderTree = input.PreserveFolderTree,
            PreserveTimestamps = input.PreserveTimestamps,
            StripMetadata = input.StripMetadata,
            FileSuffix = SanitizeSuffix(input.FileSuffix),
            CollisionPolicy = input.CollisionPolicy,

            ImageFormat = input.ImageFormat,
            ImageQuality = input.ImageQuality,
            ImageResizeMode = input.ImageResizeMode,
            ImageWidth = imageWidth,
            ImageHeight = imageHeight,
            ImageScalePercent = imagePercent,
            ImageAspectRatio = input.ImageAspectRatio,
            ImageCustomAspectWidth = ParseDouble(input.ImageCustomAspectWidth, 16),
            ImageCustomAspectHeight = ParseDouble(input.ImageCustomAspectHeight, 9),

            VideoContainer = input.VideoContainer,
            VideoCodec = input.VideoCodec,
            VideoCrf = input.VideoCrf,
            VideoPreset = input.VideoPreset,
            VideoResolution = input.VideoResolution,
            VideoWidth = videoWidth,
            VideoHeight = videoHeight,
            VideoResizeMode = input.VideoResizeMode,
            VideoAspectRatio = input.VideoAspectRatio,
            VideoCustomAspectWidth = ParseDouble(input.VideoCustomAspectWidth, 16),
            VideoCustomAspectHeight = ParseDouble(input.VideoCustomAspectHeight, 9),
            VideoFps = input.VideoFps,
            VideoCustomFps = videoFps,
            VideoAudioCodec = input.VideoAudioCodec,
            VideoAudioBitrate = videoAudioBitrate,
            ExtractAudioOnly = input.ExtractAudioOnly,

            AudioFormat = input.AudioFormat,
            AudioBitrate = audioBitrate,
            AudioSampleRate = input.AudioSampleRate,
            AudioChannels = input.AudioChannels,
            AudioNormalize = input.AudioNormalize
        };

        var editedVideoCandidates = request.CandidateJobs.Where(job =>
            job.Kind == MediaKind.Video && job.EditPlan is not null).ToList();

        if (editedVideoCandidates.Count > 0 && options.ExtractAudioOnly)
        {
            return EffectiveOptionsResult.Failure(
                "Extract audio only does not apply crop, cut or stitch plans. Remove the edit plans or disable audio-only extraction.");
        }

        if (editedVideoCandidates.Count > 0 && options.VideoAudioCodec == "Copy")
        {
            return EffectiveOptionsResult.Failure(
                "Edited video audio cannot use stream copy. Choose AAC, Opus, MP3 or remove audio.");
        }

        if (request.CandidateJobs.Any(job => job.Kind == MediaKind.Video) && !options.ExtractAudioOnly)
        {
            if (options.VideoCodec == "Copy" &&
                (options.VideoResolution != "Keep" || options.VideoFps != "Keep" || editedVideoCandidates.Count > 0))
            {
                return EffectiveOptionsResult.Failure(
                    "Video stream copy cannot be combined with resizing, cropping, cutting or stitching.");
            }

            if (options.VideoContainer == "WebM" && options.VideoCodec is not ("VP9" or "AV1" or "Copy"))
            {
                return EffectiveOptionsResult.Failure(
                    "WebM output requires VP9, AV1 or stream-copy video in this version.");
            }

            if (options.VideoContainer == "WebM" && options.VideoAudioCodec is not ("Opus" or "None" or "Copy"))
            {
                return EffectiveOptionsResult.Failure(
                    "WebM output requires Opus, no audio or a compatible copied audio stream.");
            }
        }

        if (editedVideoCandidates.Count > 0)
        {
            sources[nameof(AppSettings.VideoCodec)] = EffectiveOptionSource.EditRequirement;
            sources[nameof(AppSettings.VideoAudioCodec)] = EffectiveOptionSource.EditRequirement;
        }

        var parallelJobs = Math.Clamp(ParseInt(input.ParallelJobs, 2), 1, 8);
        return EffectiveOptionsResult.Resolved(options, parallelJobs, sources, lockedFields);
    }

    private static Dictionary<string, EffectiveOptionSource> CreateGlobalSources() =>
        MediaForge.Services.Presets.PresetService.AllowedFieldKeys.ToDictionary(
            key => key,
            _ => EffectiveOptionSource.Global,
            StringComparer.Ordinal);

    private static bool IsFiniteRatio(double width, double height)
    {
        var ratio = width / height;
        return ratio > 0 && double.IsFinite(ratio);
    }

    private static bool TryParsePositiveInt(string? text, out int value) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value > 0;

    private static bool TryParsePositiveDouble(string? text, out double value) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
        double.IsFinite(value) && value > 0;

    private static int ParseInt(string? text, int fallback) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;

    private static double ParseDouble(string? text, double fallback) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value)
            ? value
            : fallback;

    private static string SanitizeSuffix(string suffix)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return new string((suffix ?? string.Empty).Where(character => !invalid.Contains(character)).ToArray()).Trim();
    }
}
