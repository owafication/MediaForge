using System.Globalization;
using System.IO;
using MediaForge.Models;
using MediaForge.Services.Images;
using MediaForge.Services.Runtime;

namespace MediaForge.Services;

public sealed record ConversionResult(bool Skipped, string OutputPath, string Message);

public sealed class MediaConversionService : IMediaConversionService
{
    private readonly object _destinationLock = new();
    private readonly HashSet<string> _reservedDestinations = new(StringComparer.OrdinalIgnoreCase);
    private readonly IProcessRunner _processRunner;

    public MediaConversionService(IProcessRunner? processRunner = null)
    {
        _processRunner = processRunner ?? new ProcessRunner();
    }

    public async Task<ConversionResult> ConvertAsync(
        MediaJob job,
        ConversionOptions options,
        IProgress<double>? progress,
        Action<string>? log,
        CancellationToken cancellationToken)
    {
        if (job.Kind == MediaKind.Image)
        {
            ImageSourcePreflight.ValidateForFfmpeg(job.SourcePath);
        }

        var destination = BuildDestinationPath(job, options);
        if (destination.Skip)
        {
            return new ConversionResult(true, destination.Path, "Skipped because the output exists or is already targeted by this batch.");
        }

        string? tempPath = null;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination.Path)!);
            tempPath = BuildTemporaryPath(destination.Path);
            var duration = job.Kind == MediaKind.Image
                ? 0
                : job.EditPlan is { Clips.Count: > 0 }
                    ? job.EditPlan.Clips.Sum(clip => clip.EffectiveDurationSeconds)
                    : await ProbeDurationSecondsAsync(job.SourcePath, options.FfprobePath, cancellationToken);

            var arguments = BuildArguments(job, options, tempPath);
            log?.Invoke($"> {Quote(options.FfmpegPath)} {string.Join(" ", arguments.Select(Quote))}");

            await RunFfmpegAsync(options.FfmpegPath, arguments, duration, progress, log, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            if (!File.Exists(tempPath) || new FileInfo(tempPath).Length == 0)
            {
                throw new InvalidOperationException("FFmpeg completed without producing a usable output file.");
            }

            File.Move(tempPath, destination.Path, options.CollisionPolicy == "Overwrite");

            if (options.PreserveTimestamps)
            {
                PreserveTimestamps(job.SourcePath, destination.Path);
            }

            progress?.Report(100);
            return new ConversionResult(false, destination.Path, destination.Path);
        }
        catch
        {
            if (tempPath is not null) TryDelete(tempPath);
            throw;
        }
        finally
        {
            ReleaseDestination(destination.Path);
        }
    }

    private (string Path, bool Skip) BuildDestinationPath(MediaJob job, ConversionOptions options)
    {
        var sourceDirectory = Path.GetDirectoryName(job.SourcePath) ?? Environment.CurrentDirectory;
        var outputDirectory = options.OutputBesideSource ? sourceDirectory : options.OutputFolder;

        if (!options.OutputBesideSource && options.PreserveFolderTree && !string.IsNullOrWhiteSpace(job.RootFolder))
        {
            var relative = Path.GetRelativePath(job.RootFolder!, sourceDirectory);
            if (!relative.StartsWith("..", StringComparison.Ordinal) && !Path.IsPathRooted(relative) && relative != ".")
            {
                outputDirectory = Path.Combine(outputDirectory, relative);
            }
        }

        var extension = GetOutputExtension(job, options);
        var stem = Path.GetFileNameWithoutExtension(job.SourcePath) + options.FileSuffix;
        if (string.IsNullOrWhiteSpace(stem)) stem = Path.GetFileNameWithoutExtension(job.SourcePath) + "_converted";

        var candidate = Path.Combine(outputDirectory, stem + extension);
        if (Path.GetFullPath(candidate).Equals(Path.GetFullPath(job.SourcePath), StringComparison.OrdinalIgnoreCase))
        {
            candidate = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(job.SourcePath) + "_converted" + extension);
        }

        lock (_destinationLock)
        {
            candidate = Path.GetFullPath(candidate);
            var occupied = File.Exists(candidate) || _reservedDestinations.Contains(candidate);
            if (!occupied)
            {
                _reservedDestinations.Add(candidate);
                return (candidate, false);
            }

            if (options.CollisionPolicy == "Skip")
            {
                return (candidate, true);
            }

            if (options.CollisionPolicy == "Overwrite" && !_reservedDestinations.Contains(candidate))
            {
                _reservedDestinations.Add(candidate);
                return (candidate, false);
            }

            var index = 1;
            string renamed;
            do
            {
                renamed = Path.GetFullPath(Path.Combine(outputDirectory, $"{stem} ({index++}){extension}"));
            } while (File.Exists(renamed) || _reservedDestinations.Contains(renamed));

            _reservedDestinations.Add(renamed);
            return (renamed, false);
        }
    }

    private void ReleaseDestination(string path)
    {
        lock (_destinationLock)
        {
            _reservedDestinations.Remove(Path.GetFullPath(path));
        }
    }

    private static string GetOutputExtension(MediaJob job, ConversionOptions options)
    {
        if (job.Kind == MediaKind.Video && options.ExtractAudioOnly)
        {
            return options.AudioFormat == "Keep"
                ? ".mka"
                : AudioExtension(options.AudioFormat, Path.GetExtension(job.SourcePath));
        }

        return job.Kind switch
        {
            MediaKind.Image => ImageExtension(options.ImageFormat, Path.GetExtension(job.SourcePath)),
            MediaKind.Video => VideoExtension(options.VideoContainer, Path.GetExtension(job.SourcePath)),
            MediaKind.Audio => AudioExtension(options.AudioFormat, Path.GetExtension(job.SourcePath)),
            _ => Path.GetExtension(job.SourcePath)
        };
    }

    private static string ImageExtension(string format, string original) => format switch
    {
        "Keep" => original,
        "JPEG" => ".jpg",
        "PNG" => ".png",
        "WebP" => ".webp",
        "AVIF" => ".avif",
        "TIFF" => ".tiff",
        "BMP" => ".bmp",
        "GIF" => ".gif",
        _ => original
    };

    private static string VideoExtension(string container, string original) => container switch
    {
        "Keep" => original,
        "MP4" => ".mp4",
        "MKV" => ".mkv",
        "WebM" => ".webm",
        "MOV" => ".mov",
        "AVI" => ".avi",
        _ => original
    };

    private static string AudioExtension(string format, string original) => format switch
    {
        "Keep" => original,
        "MP3" => ".mp3",
        "AAC" => ".aac",
        "M4A" => ".m4a",
        "FLAC" => ".flac",
        "WAV" => ".wav",
        "OGG" => ".ogg",
        "OPUS" => ".opus",
        _ => original
    };

    private static List<string> BuildArguments(MediaJob job, ConversionOptions options, string tempPath)
    {
        var args = new List<string> { "-hide_banner", "-nostdin", "-y" };

        if (job.Kind == MediaKind.Video && !options.ExtractAudioOnly && job.EditPlan is { Clips.Count: > 0 } editPlan)
        {
            foreach (var clip in editPlan.Clips) args.AddRange(["-i", clip.SourcePath]);
            if (options.StripMetadata) args.AddRange(["-map_metadata", "-1", "-map_chapters", "-1"]);
            AddEditedVideoArguments(args, editPlan, options, Path.GetExtension(tempPath));
        }
        else
        {
            args.AddRange(["-i", job.SourcePath]);
            if (options.StripMetadata) args.AddRange(["-map_metadata", "-1", "-map_chapters", "-1"]);

            switch (job.Kind)
            {
                case MediaKind.Image:
                    AddImageArguments(args, options, job.EditPlan, Path.GetExtension(tempPath));
                    break;
                case MediaKind.Video when options.ExtractAudioOnly:
                    AddAudioArguments(args, options, Path.GetExtension(tempPath), sourceFormat: options.AudioFormat);
                    break;
                case MediaKind.Video:
                    AddVideoArguments(args, options, Path.GetExtension(tempPath));
                    break;
                case MediaKind.Audio:
                    AddAudioArguments(args, options, Path.GetExtension(tempPath), sourceFormat: options.AudioFormat);
                    break;
                default:
                    throw new InvalidOperationException("Unsupported media type.");
            }
        }

        args.AddRange(["-progress", "pipe:1", "-nostats", tempPath]);
        return args;
    }

    private static void AddImageArguments(List<string> args, ConversionOptions options, MediaEditPlan? editPlan, string outputExtension)
    {
        args.AddRange(["-map", "0:v:0", "-frames:v", "1"]);

        var quality = Math.Clamp(options.ImageQuality, 1, 100);
        var format = options.ImageFormat == "Keep" ? ImageFormatFromExtension(outputExtension) : options.ImageFormat;
        var filters = new List<string>();
        if (editPlan is not null && !editPlan.ImageCrop.IsFullFrame) filters.Add(BuildCropFilter(editPlan.ImageCrop, requireEven: false));
        var resizeFilter = BuildImageFilter(options, editPlan);
        if (!string.IsNullOrWhiteSpace(resizeFilter)) filters.Add(resizeFilter);
        if (format == "AVIF") filters.Add("pad=ceil(iw/2)*2:ceil(ih/2)*2");
        if (filters.Count > 0) args.AddRange(["-vf", string.Join(",", filters)]);

        switch (format)
        {
            case "JPEG":
                args.AddRange(["-c:v", "mjpeg", "-q:v", QualityToQScale(quality).ToString(CultureInfo.InvariantCulture)]);
                break;
            case "PNG":
                args.AddRange(["-c:v", "png", "-compression_level", QualityToCompressionLevel(quality).ToString(CultureInfo.InvariantCulture)]);
                break;
            case "WebP":
                args.AddRange(["-c:v", "libwebp", "-quality", quality.ToString(CultureInfo.InvariantCulture)]);
                break;
            case "AVIF":
                args.AddRange(["-c:v", "libaom-av1", "-crf", QualityToAv1Crf(quality).ToString(CultureInfo.InvariantCulture), "-still-picture", "1", "-pix_fmt", "yuv420p"]);
                break;
            case "TIFF":
                args.AddRange(["-c:v", "tiff", "-compression_algo", "deflate"]);
                break;
            case "BMP":
                args.AddRange(["-c:v", "bmp"]);
                break;
            case "GIF":
                args.AddRange(["-c:v", "gif"]);
                break;
        }
    }

    private static void AddEditedVideoArguments(List<string> args, MediaEditPlan editPlan, ConversionOptions options, string outputExtension)
    {
        if (options.VideoCodec == "Copy") throw new InvalidOperationException("Cutting, cropping and stitching require video re-encoding.");
        if (options.VideoAudioCodec == "Copy") throw new InvalidOperationException("Stitched or trimmed audio cannot use stream copy.");

        var width = MakeEven(Math.Clamp(editPlan.TargetWidth, 2, 16384));
        var height = MakeEven(Math.Clamp(editPlan.TargetHeight, 2, 16384));
        var fps = ResolveFps(options) ?? editPlan.Clips.Select(clip => clip.FrameRate).FirstOrDefault(value => value > 0);
        if (fps <= 0) fps = 30;
        var includeAudio = options.VideoAudioCodec != "None";
        var filters = new List<string>();

        for (var index = 0; index < editPlan.Clips.Count; index++)
        {
            var clip = editPlan.Clips[index];
            var videoFilters = new List<string>
            {
                $"trim=start={FormatNumber(clip.TrimStartSeconds)}:end={FormatNumber(clip.TrimEndSeconds)}",
                "setpts=PTS-STARTPTS"
            };
            if (!clip.Crop.IsFullFrame) videoFilters.Add(BuildCropFilter(clip.Crop, requireEven: true));
            videoFilters.Add(BuildPlacementFilter(editPlan.ResizeMode, width, height, requireEven: true));
            videoFilters.Add($"fps={FormatNumber(fps)}");
            videoFilters.Add("format=yuv420p");
            videoFilters.Add("setsar=1");
            filters.Add($"[{index}:v:0]{string.Join(",", videoFilters)}[v{index}]");

            if (includeAudio)
            {
                if (clip.HasAudio)
                {
                    filters.Add($"[{index}:a:0]atrim=start={FormatNumber(clip.TrimStartSeconds)}:end={FormatNumber(clip.TrimEndSeconds)},asetpts=PTS-STARTPTS,aresample=48000,aformat=sample_fmts=fltp:sample_rates=48000:channel_layouts=stereo[a{index}]");
                }
                else
                {
                    filters.Add($"anullsrc=r=48000:cl=stereo,atrim=duration={FormatNumber(clip.EffectiveDurationSeconds)},asetpts=PTS-STARTPTS[a{index}]");
                }
            }
        }

        var concatInputs = string.Concat(Enumerable.Range(0, editPlan.Clips.Count)
            .Select(index => includeAudio ? $"[v{index}][a{index}]" : $"[v{index}]"));
        filters.Add(includeAudio
            ? $"{concatInputs}concat=n={editPlan.Clips.Count}:v=1:a=1[outv][outa]"
            : $"{concatInputs}concat=n={editPlan.Clips.Count}:v=1:a=0[outv]");

        args.AddRange(["-filter_complex", string.Join(";", filters), "-map", "[outv]"]);
        if (includeAudio) args.AddRange(["-map", "[outa]"]);
        else args.Add("-an");

        AddVideoEncoder(args, options);
        if (options.VideoCodec == "H.265" && IsIsoBaseMedia(outputExtension)) args.AddRange(["-tag:v", "hvc1"]);
        if (includeAudio) AddVideoAudioEncoder(args, options);
        if (IsIsoBaseMedia(outputExtension)) args.AddRange(["-movflags", "+faststart"]);
    }

    private static void AddVideoArguments(List<string> args, ConversionOptions options, string outputExtension)
    {
        args.AddRange(["-map", "0:v:0", "-map", "0:a?"]);
        var filter = BuildVideoFilter(options);
        var fps = ResolveFps(options);

        if (options.VideoCodec == "Copy")
        {
            if (!string.IsNullOrWhiteSpace(filter) || fps is not null)
            {
                throw new InvalidOperationException("Video stream copy cannot be combined with resolution or frame-rate changes.");
            }
            args.AddRange(["-c:v", "copy"]);
        }
        else
        {
            filter ??= "scale=trunc(iw/2)*2:trunc(ih/2)*2";
            args.AddRange(["-vf", filter]);
            if (fps is not null) args.AddRange(["-r", fps.Value.ToString("0.###", CultureInfo.InvariantCulture)]);
            AddVideoEncoder(args, options);
            if (options.VideoCodec == "H.265" &&
                (outputExtension.Equals(".mp4", StringComparison.OrdinalIgnoreCase) ||
                 outputExtension.Equals(".mov", StringComparison.OrdinalIgnoreCase) ||
                 outputExtension.Equals(".m4v", StringComparison.OrdinalIgnoreCase)))
            {
                args.AddRange(["-tag:v", "hvc1"]);
            }
        }

        AddVideoAudioEncoder(args, options);

        if (IsIsoBaseMedia(outputExtension))
        {
            args.AddRange(["-movflags", "+faststart"]);
        }
    }

    private static void AddVideoEncoder(List<string> args, ConversionOptions options)
    {
        var crf = Math.Clamp(options.VideoCrf, 0, 51);
        switch (options.VideoCodec)
        {
            case "H.265":
                args.AddRange(["-c:v", "libx265", "-crf", crf.ToString(CultureInfo.InvariantCulture), "-preset", options.VideoPreset, "-pix_fmt", "yuv420p"]);
                break;
            case "AV1":
                args.AddRange(["-c:v", "libaom-av1", "-crf", Math.Clamp(crf + 8, 0, 63).ToString(CultureInfo.InvariantCulture), "-b:v", "0", "-cpu-used", "6", "-pix_fmt", "yuv420p"]);
                break;
            case "VP9":
                args.AddRange(["-c:v", "libvpx-vp9", "-crf", Math.Clamp(crf + 8, 0, 63).ToString(CultureInfo.InvariantCulture), "-b:v", "0", "-row-mt", "1", "-pix_fmt", "yuv420p"]);
                break;
            case "MPEG4":
                args.AddRange(["-c:v", "mpeg4", "-q:v", QualityToQScale(100 - Math.Min(99, crf * 2)).ToString(CultureInfo.InvariantCulture)]);
                break;
            default:
                args.AddRange(["-c:v", "libx264", "-crf", crf.ToString(CultureInfo.InvariantCulture), "-preset", options.VideoPreset, "-pix_fmt", "yuv420p"]);
                break;
        }
    }

    private static void AddVideoAudioEncoder(List<string> args, ConversionOptions options)
    {
        switch (options.VideoAudioCodec)
        {
            case "None":
                args.Add("-an");
                break;
            case "Copy":
                args.AddRange(["-c:a", "copy"]);
                break;
            case "Opus":
                args.AddRange(["-c:a", "libopus", "-b:a", $"{Math.Clamp(options.VideoAudioBitrate, 16, 512)}k"]);
                break;
            case "MP3":
                args.AddRange(["-c:a", "libmp3lame", "-b:a", $"{Math.Clamp(options.VideoAudioBitrate, 32, 320)}k"]);
                break;
            default:
                args.AddRange(["-c:a", "aac", "-b:a", $"{Math.Clamp(options.VideoAudioBitrate, 32, 512)}k"]);
                break;
        }
    }

    private static void AddAudioArguments(List<string> args, ConversionOptions options, string outputExtension, string sourceFormat)
    {
        args.Add("-vn");
        var hasProcessing = options.AudioNormalize || options.AudioSampleRate != "Keep" || options.AudioChannels != "Keep";
        var format = sourceFormat == "Keep" ? FormatFromExtension(outputExtension) : sourceFormat;

        if (sourceFormat == "Keep" && !hasProcessing)
        {
            args.AddRange(["-c:a", "copy"]);
        }
        else
        {
            var bitrate = Math.Clamp(options.AudioBitrate, 16, 512);
            switch (format)
            {
                case "FLAC":
                    args.AddRange(["-c:a", "flac"]);
                    break;
                case "WAV":
                    args.AddRange(["-c:a", "pcm_s16le"]);
                    break;
                case "AAC":
                case "M4A":
                    args.AddRange(["-c:a", "aac", "-b:a", $"{bitrate}k"]);
                    break;
                case "OGG":
                    args.AddRange(["-c:a", "libvorbis", "-b:a", $"{bitrate}k"]);
                    break;
                case "OPUS":
                    args.AddRange(["-c:a", "libopus", "-b:a", $"{bitrate}k"]);
                    break;
                case "WMA":
                    args.AddRange(["-c:a", "wmav2", "-b:a", $"{bitrate}k"]);
                    break;
                case "AC3":
                    args.AddRange(["-c:a", "ac3", "-b:a", $"{bitrate}k"]);
                    break;
                case "EAC3":
                    args.AddRange(["-c:a", "eac3", "-b:a", $"{bitrate}k"]);
                    break;
                case "AIFF":
                    args.AddRange(["-c:a", "pcm_s16be"]);
                    break;
                default:
                    args.AddRange(["-c:a", "libmp3lame", "-b:a", $"{Math.Clamp(bitrate, 32, 320)}k"]);
                    break;
            }
        }

        if (options.AudioSampleRate != "Keep") args.AddRange(["-ar", options.AudioSampleRate]);
        if (options.AudioChannels != "Keep") args.AddRange(["-ac", options.AudioChannels]);
        if (options.AudioNormalize)
        {
            args.AddRange(["-af", "loudnorm=I=-14:TP=-1.0:LRA=11"]);
            if (options.AudioSampleRate == "Keep") args.AddRange(["-ar", "48000"]);
        }
    }


    private static string ImageFormatFromExtension(string extension) => extension.ToLowerInvariant() switch
    {
        ".jpg" or ".jpeg" => "JPEG",
        ".png" => "PNG",
        ".webp" => "WebP",
        ".avif" => "AVIF",
        ".tif" or ".tiff" => "TIFF",
        ".bmp" or ".dib" => "BMP",
        ".gif" => "GIF",
        _ => "Keep"
    };

    private static string FormatFromExtension(string extension) => extension.ToLowerInvariant() switch
    {
        ".flac" => "FLAC",
        ".wav" => "WAV",
        ".aac" => "AAC",
        ".m4a" => "M4A",
        ".ogg" or ".oga" => "OGG",
        ".opus" => "OPUS",
        ".wma" => "WMA",
        ".ac3" => "AC3",
        ".eac3" => "EAC3",
        ".aif" or ".aiff" => "AIFF",
        ".mka" => "FLAC",
        _ => "MP3"
    };

    private static string? BuildImageFilter(ConversionOptions options, MediaEditPlan? editPlan)
    {
        if (editPlan is not null)
        {
            return BuildPlacementFilter(editPlan.ResizeMode,
                Math.Clamp(editPlan.TargetWidth, 1, 100000),
                Math.Clamp(editPlan.TargetHeight, 1, 100000),
                requireEven: false);
        }

        var width = Math.Clamp(options.ImageWidth, 1, 100000);
        var height = Math.Clamp(options.ImageHeight, 1, 100000);
        var percent = Math.Clamp(options.ImageScalePercent, 1, 10000);
        return options.ImageResizeMode switch
        {
            "None" => null,
            "Percent" => $"scale=iw*{percent}/100:ih*{percent}/100",
            "Fill" => BuildPlacementFilter("Crop", width, height, requireEven: false),
            "Exact" => BuildPlacementFilter("Stretch", width, height, requireEven: false),
            _ => BuildPlacementFilter(options.ImageResizeMode, width, height, requireEven: false)
        };
    }

    private static string? BuildVideoFilter(ConversionOptions options)
    {
        var dimensions = options.VideoResolution switch
        {
            "720p" => (1280, 720),
            "1080p" => (1920, 1080),
            "1440p" => (2560, 1440),
            "2160p" => (3840, 2160),
            "Custom" => (Math.Clamp(options.VideoWidth, 2, 16384), Math.Clamp(options.VideoHeight, 2, 16384)),
            _ => (0, 0)
        };

        if (dimensions.Item1 == 0) return null;
        return BuildPlacementFilter(options.VideoResizeMode, MakeEven(dimensions.Item1), MakeEven(dimensions.Item2), requireEven: true);
    }

    private static string BuildPlacementFilter(string mode, int width, int height, bool requireEven)
    {
        width = requireEven ? MakeEven(width) : Math.Max(1, width);
        height = requireEven ? MakeEven(height) : Math.Max(1, height);
        return mode switch
        {
            "Center" => $"crop='min(iw,{width})':'min(ih,{height})':'max(0,(iw-ow)/2)':'max(0,(ih-oh)/2)',pad={width}:{height}:(ow-iw)/2:(oh-ih)/2",
            "Stretch" => $"scale={width}:{height}",
            "Shrink" => $"scale=w='min(iw,{width})':h='min(ih,{height})':force_original_aspect_ratio=decrease,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2",
            "Crop" or "Fill" => $"scale={width}:{height}:force_original_aspect_ratio=increase,crop={width}:{height}",
            _ => $"scale={width}:{height}:force_original_aspect_ratio=decrease,pad={width}:{height}:(ow-iw)/2:(oh-ih)/2"
        };
    }

    private static string BuildCropFilter(CropSelection crop, bool requireEven)
    {
        crop = crop.Clamp();
        var divisor = requireEven ? 2 : 1;
        var minimum = requireEven ? 2 : 1;
        var maximumWidth = requireEven ? "floor(iw/2)*2" : "iw";
        var maximumHeight = requireEven ? "floor(ih/2)*2" : "ih";
        var width = $"min({maximumWidth}\\,max({minimum}\\,floor(iw*{FormatNumber(crop.Width)}/{divisor})*{divisor}))";
        var height = $"min({maximumHeight}\\,max({minimum}\\,floor(ih*{FormatNumber(crop.Height)}/{divisor})*{divisor}))";
        var x = $"max(0\\,min(floor(iw*{FormatNumber(crop.X)}/{divisor})*{divisor}\\,iw-ow))";
        var y = $"max(0\\,min(floor(ih*{FormatNumber(crop.Y)}/{divisor})*{divisor}\\,ih-oh))";
        return $"crop={width}:{height}:{x}:{y}" + (requireEven ? string.Empty : ":0:1");
    }

    private static int MakeEven(int value) => Math.Max(2, value % 2 == 0 ? value : value - 1);
    private static string FormatNumber(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
    private static bool IsIsoBaseMedia(string extension) =>
        extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".mov", StringComparison.OrdinalIgnoreCase) ||
        extension.Equals(".m4v", StringComparison.OrdinalIgnoreCase);

    private static double? ResolveFps(ConversionOptions options)
    {
        if (options.VideoFps == "Keep") return null;
        if (options.VideoFps == "Custom") return Math.Clamp(options.VideoCustomFps, 1, 240);
        return double.TryParse(options.VideoFps, NumberStyles.Float, CultureInfo.InvariantCulture, out var fps) ? fps : null;
    }

    private async Task<double> ProbeDurationSecondsAsync(string sourcePath, string ffprobePath, CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync(
            new ProcessRunRequest
            {
                FileName = ffprobePath,
                Arguments =
                [
                    "-v", "error",
                    "-show_entries", "format=duration",
                    "-of", "default=noprint_wrappers=1:nokey=1",
                    sourcePath
                ]
            },
            cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(result.StandardError)
                ? "ffprobe could not read the source file."
                : result.StandardError.Trim());
        }

        var output = result.StandardOutput.Trim();
        return double.TryParse(output, NumberStyles.Float, CultureInfo.InvariantCulture, out var duration) &&
               double.IsFinite(duration) && duration > 0
            ? duration
            : 0;
    }

    private async Task RunFfmpegAsync(
        string ffmpegPath,
        IReadOnlyList<string> arguments,
        double durationSeconds,
        IProgress<double>? progress,
        Action<string>? log,
        CancellationToken cancellationToken)
    {
        var result = await _processRunner.RunAsync(
            new ProcessRunRequest
            {
                FileName = ffmpegPath,
                Arguments = arguments,
                StandardOutputTailLineLimit = 0,
                StandardErrorTailLineLimit = 35,
                StandardOutputLineReceived = line =>
                {
                    if (line.StartsWith("out_time_us=", StringComparison.Ordinal) && durationSeconds > 0 &&
                        long.TryParse(line.AsSpan("out_time_us=".Length), out var microseconds))
                    {
                        progress?.Report(Math.Min(99, microseconds / 1_000_000d / durationSeconds * 100d));
                    }
                    else if (line == "progress=end")
                    {
                        progress?.Report(100);
                    }
                },
                StandardErrorLineReceived = line => log?.Invoke(line)
            },
            cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                ImageSourcePreflight.DescribeFfmpegFailure(result.StandardError, result.ExitCode));
        }
    }

    private static string BuildTemporaryPath(string destinationPath)
    {
        var directory = Path.GetDirectoryName(destinationPath)!;
        var stem = Path.GetFileNameWithoutExtension(destinationPath);
        var extension = Path.GetExtension(destinationPath);
        return Path.Combine(directory, $".{stem}.{Guid.NewGuid():N}.partial{extension}");
    }

    private static int QualityToQScale(int quality) => 2 + (int)Math.Round((100 - quality) * 29d / 99d);
    private static int QualityToCompressionLevel(int quality) => (int)Math.Round(quality * 9d / 100d);
    private static int QualityToAv1Crf(int quality) => (int)Math.Round((100 - quality) * 63d / 99d);

    private static void PreserveTimestamps(string source, string destination)
    {
        try
        {
            File.SetCreationTimeUtc(destination, File.GetCreationTimeUtc(source));
            File.SetLastWriteTimeUtc(destination, File.GetLastWriteTimeUtc(source));
            File.SetLastAccessTimeUtc(destination, File.GetLastAccessTimeUtc(source));
        }
        catch
        {
            // Timestamp preservation is best-effort on filesystems that support it.
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // A failed cleanup must not hide the conversion error.
        }
    }

    private static string Quote(string value) => value.Any(char.IsWhiteSpace) ? $"\"{value.Replace("\"", "\\\"")}\"" : value;
}
