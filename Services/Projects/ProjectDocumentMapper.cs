using System.IO;
using System.Reflection;
using MediaForge.Models;
using MediaForge.Models.Projects;
using MediaForge.Models.Presets;

namespace MediaForge.Services.Projects;

public static class ProjectDocumentMapper
{
    public static ProjectDocument Capture(
        IReadOnlyList<MediaJob> jobs,
        AppSettings defaults,
        string? projectPath,
        Guid projectId,
        DateTimeOffset createdUtc,
        bool portable,
        ProjectDocument? basis = null,
        int selectedTabIndex = 0,
        Guid? selectedItemId = null,
        PresetDocument? selectedPreset = null)
    {
        ArgumentNullException.ThrowIfNull(jobs);
        ArgumentNullException.ThrowIfNull(defaults);

        var now = DateTimeOffset.UtcNow;
        var projectDirectory = string.IsNullOrWhiteSpace(projectPath)
            ? null
            : Path.GetDirectoryName(Path.GetFullPath(projectPath));
        var relativeRoot = portable && projectDirectory is not null ? "." : null;
        var pathRoot = relativeRoot is null ? null : ProjectPathPolicy.ResolveProjectRoot(projectPath, relativeRoot);

        var projectDefaults = CloneSettings(defaults);
        projectDefaults.Extensions ??= basis?.ProjectDefaults.Extensions;

        var document = new ProjectDocument
        {
            SchemaVersion = ProjectDocument.CurrentSchemaVersion,
            ApplicationVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.1.0",
            ProjectId = projectId,
            CreatedUtc = createdUtc,
            ModifiedUtc = now,
            ProjectRoot = relativeRoot,
            SelectedPresetId = selectedPreset?.Id,
            SelectedPresetVersion = selectedPreset?.Version,
            SelectedPresetName = selectedPreset?.Name,
            SelectedPresetSnapshot = selectedPreset?.Values.Clone(),
            ProjectDefaults = projectDefaults,
            OutputRules = new ProjectOutputRulesDocument
            {
                OutputBesideSource = defaults.OutputBesideSource,
                OutputFolder = defaults.OutputFolder,
                PreserveFolderTree = defaults.PreserveFolderTree,
                PreserveTimestamps = defaults.PreserveTimestamps,
                StripMetadata = defaults.StripMetadata,
                FileSuffix = defaults.FileSuffix,
                CollisionPolicy = defaults.CollisionPolicy,
                Extensions = basis?.OutputRules.Extensions
            },
            PresetReferences = basis?.PresetReferences ?? [],
            UiState = new ProjectUiStateDocument
            {
                SelectedTabIndex = selectedTabIndex,
                SelectedItemId = selectedItemId,
                Extensions = basis?.UiState.Extensions
            },
            Extensions = basis?.Extensions
        };

        document.Queue = jobs.Select((job, index) =>
        {
            var basisItem = basis?.Queue.FirstOrDefault(item => item.ItemId == job.Id);
            return new ProjectQueueItemDocument
            {
                ItemId = job.Id,
                Enabled = job.Enabled,
                Order = index,
                Priority = job.Priority,
                Source = CaptureSource(
                    job.SourcePath,
                    job.RootFolder,
                    job.Kind,
                    job.SourceBytes,
                    job.SourceLastWriteUtc,
                    pathRoot,
                    basisItem?.Source.ReferenceId ?? job.Id,
                    basisItem?.Source.Extensions),
                EditPlan = CaptureEditPlan(job.EditPlan, pathRoot, basisItem?.EditPlan),
                SelectedPresetId = job.SelectedPresetId,
                SelectedPresetVersion = job.SelectedPresetVersion,
                SelectedPresetName = job.SelectedPresetName,
                SelectedPresetSnapshot = job.SelectedPresetSnapshot?.Clone(),
                PerJobOverrides = job.PerJobOverrides?.Clone(),
                LastKnownOutputPath = string.IsNullOrWhiteSpace(job.OutputPath) ? null : job.OutputPath,
                LastKnownOutputState = job.State is JobState.Completed or JobState.CompletedWithWarnings or JobState.VerificationFailed or JobState.Failed or JobState.Cancelled or JobState.Skipped
                    ? job.State
                    : null,
                Extensions = basisItem?.Extensions
            };
        }).ToList();

        var references = new Dictionary<Guid, ProjectPresetReferenceDocument>();
        foreach (var reference in basis?.PresetReferences ?? [])
        {
            if (reference.PresetId != Guid.Empty) references[reference.PresetId] = reference;
        }
        if (document.SelectedPresetId.HasValue)
        {
            references[document.SelectedPresetId.Value] = new ProjectPresetReferenceDocument
            {
                PresetId = document.SelectedPresetId.Value,
                Version = document.SelectedPresetVersion ?? string.Empty,
                Name = document.SelectedPresetName
            };
        }
        foreach (var job in jobs.Where(job => job.SelectedPresetId.HasValue))
        {
            var id = job.SelectedPresetId!.Value;
            references[id] = new ProjectPresetReferenceDocument
            {
                PresetId = id,
                Version = job.SelectedPresetVersion ?? string.Empty,
                Name = job.SelectedPresetName
            };
        }
        document.PresetReferences = references.Values.OrderBy(reference => reference.Name, StringComparer.CurrentCultureIgnoreCase).ToList();

        return document;
    }

    public static AppSettings CreateEffectiveSettings(ProjectDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var settings = CloneSettings(document.ProjectDefaults);
        settings.OutputBesideSource = document.OutputRules.OutputBesideSource;
        settings.OutputFolder = document.OutputRules.OutputFolder;
        settings.PreserveFolderTree = document.OutputRules.PreserveFolderTree;
        settings.PreserveTimestamps = document.OutputRules.PreserveTimestamps;
        settings.StripMetadata = document.OutputRules.StripMetadata;
        settings.FileSuffix = document.OutputRules.FileSuffix;
        settings.CollisionPolicy = document.OutputRules.CollisionPolicy;
        return settings;
    }

    public static (IReadOnlyList<MediaJob> Jobs, IReadOnlyList<ProjectSourceIssue> Issues) Materialize(
        ProjectDocument document,
        string? projectPath)
    {
        ArgumentNullException.ThrowIfNull(document);
        var issues = new List<ProjectSourceIssue>();
        var root = ResolveRoot(document, projectPath, issues);
        var jobs = document.Queue
            .OrderBy(item => item.Order)
            .Select(item => MaterializeJob(item, root, issues))
            .ToList();
        return (jobs, issues);
    }

    public static ProjectSourceReferenceDocument CaptureSource(
        string sourcePath,
        string? rootFolder,
        MediaKind kind,
        long knownSize,
        DateTimeOffset knownLastWriteUtc,
        string? pathRoot,
        Guid? referenceId = null,
        Dictionary<string, System.Text.Json.JsonElement>? extensions = null)
    {
        var fullPath = Path.GetFullPath(sourcePath);
        var info = SafeFileInfo(fullPath);
        ProjectPathPolicy.TryMakeRelativeInsideRoot(fullPath, pathRoot, out var relativePath);

        string? rootFolderRelative = null;
        string? rootFolderAbsolute = null;
        if (!string.IsNullOrWhiteSpace(rootFolder))
        {
            rootFolderAbsolute = Path.GetFullPath(rootFolder);
            ProjectPathPolicy.TryMakeRelativeInsideRoot(rootFolderAbsolute, pathRoot, out rootFolderRelative);
        }

        return new ProjectSourceReferenceDocument
        {
            ReferenceId = referenceId.GetValueOrDefault(Guid.NewGuid()),
            FileName = Path.GetFileName(fullPath),
            RelativePath = relativePath,
            AbsolutePath = fullPath,
            RootFolderRelativePath = rootFolderRelative,
            RootFolderAbsolutePath = rootFolderAbsolute,
            Kind = kind,
            SizeBytes = info?.Length ?? knownSize,
            LastWriteUtc = info is null
                ? knownLastWriteUtc
                : new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero),
            Extensions = extensions
        };
    }

    private static MediaJob MaterializeJob(
        ProjectQueueItemDocument item,
        string? root,
        List<ProjectSourceIssue> issues)
    {
        var sourcePath = ResolveSource(item.Source, root, issues);
        var rootFolder = ResolveOptionalRootFolder(item.Source, root);
        var job = new MediaJob
        {
            Id = item.ItemId == Guid.Empty ? Guid.NewGuid() : item.ItemId,
            SourcePath = sourcePath,
            RootFolder = rootFolder,
            Kind = item.Source.Kind,
            SourceBytes = item.Source.SizeBytes,
            SourceLastWriteUtc = item.Source.LastWriteUtc,
            EditPlan = MaterializeEditPlan(item.EditPlan, root, issues),
            Enabled = item.Enabled,
            Priority = item.Priority,
            SelectedPresetId = item.SelectedPresetId,
            SelectedPresetVersion = item.SelectedPresetVersion,
            SelectedPresetName = item.SelectedPresetId.HasValue
                ? item.SelectedPresetName ?? "Missing preset"
                : "Global settings",
            SelectedPresetSnapshot = item.SelectedPresetSnapshot?.Clone(),
            PerJobOverrides = item.PerJobOverrides?.Clone()
        };

        if (!string.IsNullOrWhiteSpace(item.LastKnownOutputPath)) job.OutputPath = item.LastKnownOutputPath;
        if (item.LastKnownOutputState is JobState.Completed or JobState.CompletedWithWarnings or JobState.VerificationFailed or JobState.Failed or JobState.Cancelled or JobState.Skipped)
        {
            job.State = item.LastKnownOutputState.Value;
        }
        return job;
    }

    private static string? ResolveRoot(
        ProjectDocument document,
        string? projectPath,
        List<ProjectSourceIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(document.ProjectRoot)) return null;

        var root = ProjectPathPolicy.ResolveProjectRoot(projectPath, document.ProjectRoot);
        if (root is null)
        {
            issues.Add(new ProjectSourceIssue(
                Guid.Empty,
                "Project root",
                document.ProjectRoot,
                ProjectSourceIssueKind.UnsafeRelativePath,
                "The portable project root is absolute or escapes the project directory. Relative paths were not used."));
        }
        return root;
    }

    private static string ResolveSource(
        ProjectSourceReferenceDocument source,
        string? root,
        List<ProjectSourceIssue> issues)
    {
        string? relativeCandidate = null;
        if (root is not null && !string.IsNullOrWhiteSpace(source.RelativePath))
        {
            if (!ProjectPathPolicy.TryResolveRelativeInsideRoot(root, source.RelativePath, out relativeCandidate))
            {
                issues.Add(new ProjectSourceIssue(
                    source.ReferenceId,
                    source.FileName,
                    source.RelativePath,
                    ProjectSourceIssueKind.UnsafeRelativePath,
                    "The stored relative path escapes the project root and was ignored."));
            }
            else if (File.Exists(relativeCandidate))
            {
                AddFingerprintIssueIfChanged(source, relativeCandidate, issues);
                return relativeCandidate;
            }
        }

        if (!string.IsNullOrWhiteSpace(source.AbsolutePath))
        {
            try
            {
                var absoluteCandidate = Path.GetFullPath(source.AbsolutePath);
                if (File.Exists(absoluteCandidate))
                {
                    AddFingerprintIssueIfChanged(source, absoluteCandidate, issues);
                    return absoluteCandidate;
                }
            }
            catch
            {
                // The missing-source issue below retains the recorded value.
            }
        }

        var recorded = relativeCandidate ?? source.AbsolutePath ?? source.RelativePath ?? source.FileName;
        issues.Add(new ProjectSourceIssue(
            source.ReferenceId,
            source.FileName,
            recorded,
            ProjectSourceIssueKind.Missing,
            "The source file is missing. Use Relink missing to choose a search folder."));
        return recorded;
    }

    private static string? ResolveOptionalRootFolder(ProjectSourceReferenceDocument source, string? root)
    {
        if (root is not null && !string.IsNullOrWhiteSpace(source.RootFolderRelativePath) &&
            ProjectPathPolicy.TryResolveRelativeInsideRoot(root, source.RootFolderRelativePath, out var relative))
        {
            return relative;
        }

        if (string.IsNullOrWhiteSpace(source.RootFolderAbsolutePath)) return null;
        try { return Path.GetFullPath(source.RootFolderAbsolutePath); }
        catch { return null; }
    }

    private static void AddFingerprintIssueIfChanged(
        ProjectSourceReferenceDocument source,
        string candidate,
        List<ProjectSourceIssue> issues)
    {
        var info = SafeFileInfo(candidate);
        if (info is null) return;
        var lastWrite = new DateTimeOffset(info.LastWriteTimeUtc, TimeSpan.Zero);
        var sizeChanged = source.SizeBytes >= 0 && info.Length != source.SizeBytes;
        var timeChanged = source.LastWriteUtc != default &&
                          Math.Abs((lastWrite - source.LastWriteUtc).TotalSeconds) > 1;
        if (!sizeChanged && !timeChanged) return;

        issues.Add(new ProjectSourceIssue(
            source.ReferenceId,
            source.FileName,
            candidate,
            ProjectSourceIssueKind.FingerprintChanged,
            $"Recorded size/time: {source.SizeBytes} bytes, {source.LastWriteUtc:u}; current: {info.Length} bytes, {lastWrite:u}."));
    }

    private static ProjectEditPlanDocument? CaptureEditPlan(MediaEditPlan? plan, string? pathRoot, ProjectEditPlanDocument? basis)
    {
        if (plan is null) return null;
        var document = new ProjectEditPlanDocument
        {
            ResizeMode = plan.ResizeMode,
            TargetWidth = plan.TargetWidth,
            TargetHeight = plan.TargetHeight,
            AspectRatio = plan.AspectRatio,
            CustomAspectWidth = plan.CustomAspectWidth,
            CustomAspectHeight = plan.CustomAspectHeight,
            LockSelectionAspect = plan.LockSelectionAspect,
            ImageCrop = CaptureCrop(plan.ImageCrop),
            Extensions = basis?.Extensions
        };
        document.Clips = plan.Clips.Select((clip, index) =>
        {
            var basisClip = basis is not null && index < basis.Clips.Count ? basis.Clips[index] : null;
            return new ProjectClipEditDocument
            {
                Source = CaptureSource(
                    clip.SourcePath,
                    null,
                    MediaKind.Video,
                    SafeFileInfo(clip.SourcePath)?.Length ?? 0,
                    default,
                    pathRoot,
                    basisClip?.Source.ReferenceId,
                    basisClip?.Source.Extensions),
                DurationSeconds = clip.DurationSeconds,
                Width = clip.Width,
                Height = clip.Height,
                FrameRate = clip.FrameRate,
                HasAudio = clip.HasAudio,
                TrimStartSeconds = clip.TrimStartSeconds,
                TrimEndSeconds = clip.TrimEndSeconds,
                Crop = CaptureCrop(clip.Crop),
                Extensions = basisClip?.Extensions
            };
        }).ToList();
        return document;
    }

    private static MediaEditPlan? MaterializeEditPlan(
        ProjectEditPlanDocument? document,
        string? root,
        List<ProjectSourceIssue> issues)
    {
        if (document is null) return null;
        var plan = new MediaEditPlan
        {
            ResizeMode = document.ResizeMode,
            TargetWidth = document.TargetWidth,
            TargetHeight = document.TargetHeight,
            AspectRatio = document.AspectRatio,
            CustomAspectWidth = document.CustomAspectWidth,
            CustomAspectHeight = document.CustomAspectHeight,
            LockSelectionAspect = document.LockSelectionAspect,
            ImageCrop = MaterializeCrop(document.ImageCrop)
        };
        foreach (var clip in document.Clips)
        {
            var materialized = new MediaClipEdit
            {
                SourcePath = ResolveSource(clip.Source, root, issues),
                DurationSeconds = clip.DurationSeconds,
                Width = clip.Width,
                Height = clip.Height,
                FrameRate = clip.FrameRate,
                HasAudio = clip.HasAudio,
                Crop = MaterializeCrop(clip.Crop)
            };
            materialized.SetTrimRange(clip.TrimStartSeconds, clip.TrimEndSeconds);
            plan.Clips.Add(materialized);
        }
        return plan;
    }

    private static ProjectCropSelectionDocument CaptureCrop(CropSelection crop) =>
        new(crop.X, crop.Y, crop.Width, crop.Height);

    private static CropSelection MaterializeCrop(ProjectCropSelectionDocument? crop) =>
        crop is null ? CropSelection.Full : new CropSelection(crop.X, crop.Y, crop.Width, crop.Height).Clamp();

    private static FileInfo? SafeFileInfo(string path)
    {
        try { return File.Exists(path) ? new FileInfo(path) : null; }
        catch { return null; }
    }

    private static AppSettings CloneSettings(AppSettings source) => new()
    {
        FfmpegPath = source.FfmpegPath,
        OutputBesideSource = source.OutputBesideSource,
        OutputFolder = source.OutputFolder,
        PreserveFolderTree = source.PreserveFolderTree,
        PreserveTimestamps = source.PreserveTimestamps,
        StripMetadata = source.StripMetadata,
        FileSuffix = source.FileSuffix,
        CollisionPolicy = source.CollisionPolicy,
        ParallelJobs = source.ParallelJobs,
        ImageFormat = source.ImageFormat,
        ImageQuality = source.ImageQuality,
        ImageResizeMode = source.ImageResizeMode,
        ImageWidth = source.ImageWidth,
        ImageHeight = source.ImageHeight,
        ImageScalePercent = source.ImageScalePercent,
        ImageAspectRatio = source.ImageAspectRatio,
        ImageCustomAspectWidth = source.ImageCustomAspectWidth,
        ImageCustomAspectHeight = source.ImageCustomAspectHeight,
        VideoContainer = source.VideoContainer,
        VideoCodec = source.VideoCodec,
        VideoCrf = source.VideoCrf,
        VideoPreset = source.VideoPreset,
        VideoResolution = source.VideoResolution,
        VideoWidth = source.VideoWidth,
        VideoHeight = source.VideoHeight,
        VideoResizeMode = source.VideoResizeMode,
        VideoAspectRatio = source.VideoAspectRatio,
        VideoCustomAspectWidth = source.VideoCustomAspectWidth,
        VideoCustomAspectHeight = source.VideoCustomAspectHeight,
        VideoFps = source.VideoFps,
        VideoCustomFps = source.VideoCustomFps,
        VideoAudioCodec = source.VideoAudioCodec,
        VideoAudioBitrate = source.VideoAudioBitrate,
        ExtractAudioOnly = source.ExtractAudioOnly,
        AudioFormat = source.AudioFormat,
        AudioBitrate = source.AudioBitrate,
        AudioSampleRate = source.AudioSampleRate,
        AudioChannels = source.AudioChannels,
        AudioNormalize = source.AudioNormalize,
        Extensions = source.Extensions
    };
}
