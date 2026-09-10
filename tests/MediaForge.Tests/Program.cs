using System.Buffers.Binary;
using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.IO;
using MediaForge.Models;
using MediaForge.Models.Projects;
using MediaForge.Models.Presets;
using MediaForge.Models.Queue;
using MediaForge.Services;
using MediaForge.Services.Images;
using MediaForge.Services.Options;
using MediaForge.Services.Projects;
using MediaForge.Services.Presets;
using MediaForge.Services.Queue;
using MediaForge.Services.Runtime;
using MediaForge.Services.Session;

namespace MediaForge.Tests;

internal static class Program
{
    private static readonly (string Name, Func<Task> Run)[] Tests =
    [
        ("ProcessRunner preserves arguments", ProcessRunnerPreservesArgumentsAsync),
        ("ProcessRunner captures bounded tails", ProcessRunnerCapturesBoundedTailsAsync),
        ("ProcessRunner returns non-zero exit codes", ProcessRunnerReturnsNonZeroExitCodeAsync),
        ("ProcessRunner cancellation kills the process tree", ProcessRunnerCancellationKillsTreeAsync),
        ("MediaClassifier is case-insensitive", MediaClassifierIsCaseInsensitiveAsync),
        ("CropSelection clamps invalid values", CropSelectionClampsInvalidValuesAsync),
        ("MediaClipEdit normalises trim ranges", MediaClipEditNormalisesTrimRangesAsync),
        ("EffectiveOptionsResolver resolves a valid snapshot", EffectiveOptionsResolverResolvesValidSnapshotAsync),
        ("EffectiveOptionsResolver rejects edited audio extraction", EffectiveOptionsResolverRejectsEditedAudioExtractionAsync),
        ("EffectiveOptionsResolver rejects incompatible WebM", EffectiveOptionsResolverRejectsIncompatibleWebMAsync),
        ("ProjectSession owns jobs and dirty revision", ProjectSessionOwnsJobsAndDirtyRevisionAsync),
        ("Project projects round-trip typed queue state", ProjectRoundTripPreservesTypedStateAsync),
        ("Project replacement preserves duplicate queue items", ProjectReplacementPreservesDuplicateQueueItemsAsync),
        ("Project output rules override legacy defaults", ProjectOutputRulesOverrideLegacyDefaultsAsync),
        ("Project newer schema opens read-only", ProjectNewerSchemaOpensReadOnlyAsync),
        ("Project non-portable saves absolute references", ProjectNonPortableSavesAbsoluteReferencesAsync),
        ("Project portable paths reject traversal", ProjectPortablePathsRejectTraversalAsync),
        ("Project recovery remains separate and bounded", ProjectRecoveryRemainsSeparateAndBoundedAsync),
        ("Project relink distinguishes exact changed and ambiguous", ProjectRelinkDistinguishesCandidatesAsync),
        ("Project recent list is bounded and deduplicated", ProjectRecentListIsBoundedAndDeduplicatedAsync),
        ("Project autosave debounces recovery snapshots", ProjectAutosaveDebouncesSnapshotsAsync),
        ("Preset catalogue enforces ownership and CRUD", PresetCatalogueEnforcesOwnershipAsync),
        ("Preset catalogue falls back when user storage is unavailable", PresetCatalogueFallsBackWhenStorageUnavailableAsync),
        ("Application data overrides isolate persistent stores", ApplicationDataOverridesIsolatePersistentStoresAsync),
        ("Preset import rejects command injection fields", PresetImportRejectsUnsafeFieldsAsync),
        ("Effective options apply preset and job precedence", EffectiveOptionsApplyPresetAndJobPrecedenceAsync),
        ("Project preserves preset snapshots and queue metadata", ProjectPreservesPresetSnapshotsAsync),
        ("ProjectSession supports professional queue mutations", ProjectSessionSupportsQueueMutationsAsync),
        ("Queue document round-trips independent workflow state", QueueDocumentRoundTripsAsync),
        ("Queue run item freezes source and edit plan", QueueRunItemFreezesSourceAndEditPlanAsync),
        ("Queue estimates label size and time", QueueEstimatesLabelSizeAndTimeAsync),
        ("QueueCoordinator pause stops new dispatch", QueueCoordinatorPauseStopsDispatchAsync),
        ("QueueCoordinator owns parallel execution", QueueCoordinatorOwnsParallelExecutionAsync),
        ("QueueCoordinator cancellation transitions jobs", QueueCoordinatorCancellationTransitionsJobsAsync),
        ("Image preflight rejects oversized decoded frames", ImagePreflightRejectsOversizedDecodedFramesAsync),
        ("Conversion preserves source and commits atomically", ConversionPreservesSourceAndCommitsAtomicallyAsync),
        ("Conversion failure cleans temporary output", ConversionFailureCleansTemporaryOutputAsync)
    ];

    public static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--child")
        {
            return await RunChildAsync(args.Skip(1).ToArray());
        }

        var failures = 0;
        foreach (var test in Tests)
        {
            try
            {
                await test.Run();
                Console.WriteLine($"[PASS] {test.Name}");
            }
            catch (Exception ex)
            {
                failures++;
                Console.Error.WriteLine($"[FAIL] {test.Name}: {ex}");
            }
        }

        Console.WriteLine($"Characterisation tests: {Tests.Length - failures} passed, {failures} failed.");
        return failures == 0 ? 0 : 1;
    }

    private static async Task ProcessRunnerPreservesArgumentsAsync()
    {
        var expected = new[] { "plain", "two words", "quote\"value", "unicode-Δ-文件", string.Empty };
        var invocation = CreateSelfInvocation("echo-arguments", expected);
        var result = await new ProcessRunner().RunAsync(
            new ProcessRunRequest
            {
                FileName = invocation.FileName,
                Arguments = invocation.Arguments
            });

        Equal(0, result.ExitCode, "child exit code");
        var actual = JsonSerializer.Deserialize<string[]>(result.StandardOutput)
            ?? throw new InvalidOperationException("The child argument payload was empty.");
        SequenceEqual(expected, actual, "argument list");
    }

    private static async Task ProcessRunnerCapturesBoundedTailsAsync()
    {
        var outputLines = new List<string>();
        var errorLines = new List<string>();
        var invocation = CreateSelfInvocation("write-lines", ["5"]);
        var result = await new ProcessRunner().RunAsync(
            new ProcessRunRequest
            {
                FileName = invocation.FileName,
                Arguments = invocation.Arguments,
                StandardOutputTailLineLimit = 2,
                StandardErrorTailLineLimit = 3,
                StandardOutputLineReceived = outputLines.Add,
                StandardErrorLineReceived = errorLines.Add
            });

        Equal(0, result.ExitCode, "child exit code");
        SequenceEqual(["out-3", "out-4"], SplitLines(result.StandardOutput), "stdout tail");
        SequenceEqual(["err-2", "err-3", "err-4"], SplitLines(result.StandardError), "stderr tail");
        SequenceEqual(["out-0", "out-1", "out-2", "out-3", "out-4"], outputLines, "stdout callbacks");
        SequenceEqual(["err-0", "err-1", "err-2", "err-3", "err-4"], errorLines, "stderr callbacks");
    }

    private static async Task ProcessRunnerReturnsNonZeroExitCodeAsync()
    {
        var invocation = CreateSelfInvocation("exit", ["17"]);
        var result = await new ProcessRunner().RunAsync(
            new ProcessRunRequest
            {
                FileName = invocation.FileName,
                Arguments = invocation.Arguments
            });

        Equal(17, result.ExitCode, "non-zero exit code");
    }

    private static async Task ProcessRunnerCancellationKillsTreeAsync()
    {
        var sentinel = Path.Combine(Path.GetTempPath(), $"mediaforge-process-tree-{Guid.NewGuid():N}.txt");
        try
        {
            var invocation = CreateSelfInvocation("spawn-grandchild", [sentinel]);
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(350));
            var stopwatch = Stopwatch.StartNew();
            await ThrowsAsync<OperationCanceledException>(() => new ProcessRunner().RunAsync(
                new ProcessRunRequest
                {
                    FileName = invocation.FileName,
                    Arguments = invocation.Arguments,
                    StandardOutputTailLineLimit = 10,
                    StandardErrorTailLineLimit = 10
                },
                cancellation.Token));
            stopwatch.Stop();

            True(stopwatch.Elapsed < TimeSpan.FromSeconds(8), "cancellation should return promptly");
            await Task.Delay(TimeSpan.FromSeconds(2.5));
            True(!File.Exists(sentinel), "a cancelled descendant must not survive to write its sentinel");
        }
        finally
        {
            TryDelete(sentinel);
        }
    }

    private static Task MediaClassifierIsCaseInsensitiveAsync()
    {
        Equal(MediaKind.Image, MediaClassifier.Classify("photo.JPEG"), "JPEG classification");
        Equal(MediaKind.Video, MediaClassifier.Classify("clip.MkV"), "MKV classification");
        Equal(MediaKind.Audio, MediaClassifier.Classify("sound.FlAc"), "FLAC classification");
        Equal(MediaKind.Unsupported, MediaClassifier.Classify("notes.txt"), "unsupported classification");
        return Task.CompletedTask;
    }

    private static Task CropSelectionClampsInvalidValuesAsync()
    {
        var clamped = new CropSelection(double.NaN, -3, 9, double.PositiveInfinity).Clamp();
        Equal(0d, clamped.X, "crop X");
        Equal(0d, clamped.Y, "crop Y");
        Equal(1d, clamped.Width, "crop width");
        Equal(1d, clamped.Height, "crop height");

        var edge = new CropSelection(0.9999, 0.9999, 1, 1).Clamp();
        True(edge.X <= 0.999 && edge.Y <= 0.999, "crop origin maximum");
        True(edge.Width <= 1 - edge.X && edge.Height <= 1 - edge.Y, "crop extent containment");
        return Task.CompletedTask;
    }

    private static Task MediaClipEditNormalisesTrimRangesAsync()
    {
        var clip = new MediaClipEdit
        {
            SourcePath = "fixture.mp4",
            DurationSeconds = 10,
            Width = 1920,
            Height = 1080,
            FrameRate = 30,
            HasAudio = true
        };

        clip.SetTrimRange(-5, 50);
        Equal(0d, clip.TrimStartSeconds, "normalised trim start");
        Equal(10d, clip.TrimEndSeconds, "normalised trim end");

        clip.SetTrimRange(9.9999, 9.9999);
        True(clip.TrimStartSeconds <= 9.999, "trim start leaves a positive interval");
        True(clip.TrimEndSeconds > clip.TrimStartSeconds, "trim end follows trim start");
        return Task.CompletedTask;
    }

    private static Task EffectiveOptionsResolverResolvesValidSnapshotAsync()
    {
        using var fixture = new TemporaryDirectory();
        var ffmpeg = fixture.CreateFile("tools/ffmpeg.exe");
        fixture.CreateFile("tools/ffprobe.exe");
        var output = Path.Combine(fixture.Path, "output");
        var input = CreateValidOptionInput(ffmpeg, output) with
        {
            FileSuffix = "_bad/name",
            ParallelJobs = "99"
        };

        var result = new EffectiveOptionsResolver().Resolve(new EffectiveOptionsRequest(input, []));
        True(result.Success, result.Error);
        Equal("_badname", result.Options!.FileSuffix, "sanitised suffix");
        Equal(8, result.ParallelJobs, "parallel clamp");
        True(Directory.Exists(output), "output directory created");
        Equal(Path.GetFullPath(ffmpeg), result.Options.FfmpegPath, "FFmpeg path snapshot");
        return Task.CompletedTask;
    }

    private static Task EffectiveOptionsResolverRejectsEditedAudioExtractionAsync()
    {
        using var fixture = new TemporaryDirectory();
        var ffmpeg = fixture.CreateFile("tools/ffmpeg.exe");
        fixture.CreateFile("tools/ffprobe.exe");
        var job = NewJob(Path.Combine(fixture.Path, "clip.mp4"), MediaKind.Video);
        job.EditPlan = new MediaEditPlan();
        var input = CreateValidOptionInput(ffmpeg, Path.Combine(fixture.Path, "output")) with
        {
            ExtractAudioOnly = true
        };

        var result = new EffectiveOptionsResolver().Resolve(new EffectiveOptionsRequest(input, [job]));
        True(!result.Success, "edited audio-only extraction should fail");
        True(result.Error.Contains("does not apply crop", StringComparison.Ordinal), "actionable edited-video message");
        return Task.CompletedTask;
    }

    private static Task EffectiveOptionsResolverRejectsIncompatibleWebMAsync()
    {
        using var fixture = new TemporaryDirectory();
        var ffmpeg = fixture.CreateFile("tools/ffmpeg.exe");
        fixture.CreateFile("tools/ffprobe.exe");
        var job = NewJob(Path.Combine(fixture.Path, "clip.mp4"), MediaKind.Video);
        var input = CreateValidOptionInput(ffmpeg, Path.Combine(fixture.Path, "output")) with
        {
            VideoContainer = "WebM",
            VideoCodec = "H.264",
            VideoAudioCodec = "AAC"
        };

        var result = new EffectiveOptionsResolver().Resolve(new EffectiveOptionsRequest(input, [job]));
        True(!result.Success, "incompatible WebM should fail");
        True(result.Error.Contains("WebM output requires VP9", StringComparison.Ordinal), "WebM video reason");
        return Task.CompletedTask;
    }

    private static Task ProjectSessionOwnsJobsAndDirtyRevisionAsync()
    {
        using var fixture = new TemporaryDirectory();
        var source = Path.Combine(fixture.Path, "source.png");
        var session = new ProjectSession();
        var first = NewJob(source, MediaKind.Image);
        var duplicate = NewJob(source, MediaKind.Image);

        Equal(1, session.AddJobs([first, duplicate]), "deduplicated job count");
        Equal(1, session.Jobs.Count, "session job count");
        True(session.IsDirty && session.Revision == 1, "add marks dirty revision");

        session.MarkClean();
        True(!session.IsDirty, "mark clean");
        first.State = JobState.Running;
        True(!session.IsDirty, "runtime state does not dirty the project");

        first.EditPlan = new MediaEditPlan();
        True(session.IsDirty && session.Revision == 2, "edit marks dirty revision");
        Equal(1, session.RemoveJobs([first]), "removed job count");
        Equal(0, session.Jobs.Count, "empty session");
        Equal(3L, session.Revision, "remove revision");
        return Task.CompletedTask;
    }

    private static Task ProjectRoundTripPreservesTypedStateAsync()
    {
        using var fixture = new TemporaryDirectory();
        var sourceA = fixture.CreateFile("portable/source-a.png");
        var sourceB = fixture.CreateFile("portable/source-b.mp4");
        var projectPath = Path.Combine(fixture.Path, "portable", "roundtrip.mediaforge");
        var editPlan = new MediaEditPlan
        {
            ResizeMode = "Crop",
            TargetWidth = 1280,
            TargetHeight = 720,
            ImageCrop = new CropSelection(0.1, 0.2, 0.7, 0.6)
        };
        editPlan.Clips.Add(new MediaClipEdit
        {
            SourcePath = sourceB,
            DurationSeconds = 12,
            Width = 1920,
            Height = 1080,
            FrameRate = 30,
            HasAudio = true,
            Crop = new CropSelection(0.05, 0.05, 0.9, 0.9)
        });
        editPlan.Clips[0].SetTrimRange(1.5, 9.25);

        var first = NewJob(sourceA, MediaKind.Image);
        first.EditPlan = editPlan;
        first.OutputPath = Path.Combine(fixture.Path, "output-a.jpg");
        first.State = JobState.Completed;
        var second = NewJob(sourceB, MediaKind.Video);
        var settings = new AppSettings { ImageQuality = 93, OutputFolder = Path.Combine(fixture.Path, "output") };
        var document = ProjectDocumentMapper.Capture(
            [first, second], settings, projectPath, Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(-5), portable: true);
        using var extensionValue = JsonDocument.Parse("{\"kept\":true}");
        document.Extensions = new Dictionary<string, JsonElement>
        {
            ["futureTopLevel"] = extensionValue.RootElement.Clone()
        };
        document.Queue[0].Extensions = new Dictionary<string, JsonElement>
        {
            ["futureQueueField"] = extensionValue.RootElement.Clone()
        };
        document.ProjectDefaults.Extensions = new Dictionary<string, JsonElement>
        {
            ["futureDefaultField"] = extensionValue.RootElement.Clone()
        };

        var service = new ProjectService();
        service.Save(document, projectPath);
        var loaded = service.Load(projectPath);

        True(!loaded.IsReadOnly, "schema v1 remains writable");
        Equal(0, loaded.SourceIssues.Count, "portable sources resolve without issues");
        Equal(2, loaded.Jobs.Count, "queue count");
        Equal(first.Id, loaded.Jobs[0].Id, "stable item ID");
        Equal(second.Id, loaded.Jobs[1].Id, "queue order");
        Equal(93, loaded.Document.ProjectDefaults.ImageQuality, "project defaults");
        Equal("Crop", loaded.Jobs[0].EditPlan!.ResizeMode, "edit resize mode");
        Equal(1, loaded.Jobs[0].EditPlan!.Clips.Count, "clip count");
        Equal(1.5, loaded.Jobs[0].EditPlan!.Clips[0].TrimStartSeconds, "trim start");
        Equal(JobState.Completed, loaded.Jobs[0].State, "last output state");
        True(loaded.Document.Extensions?.ContainsKey("futureTopLevel") == true, "top-level extension retained");
        True(loaded.Document.Queue[0].Extensions?.ContainsKey("futureQueueField") == true, "queue extension retained");
        True(loaded.Document.ProjectDefaults.Extensions?.ContainsKey("futureDefaultField") == true, "defaults extension retained");

        var recaptured = ProjectDocumentMapper.Capture(
            loaded.Jobs, loaded.Document.ProjectDefaults, projectPath, loaded.Document.ProjectId,
            loaded.Document.CreatedUtc, portable: true, loaded.Document);
        var json = service.Serialize(recaptured);
        True(json.Contains("futureTopLevel", StringComparison.Ordinal), "top-level unknown field survives supported round-trip");
        True(json.Contains("futureQueueField", StringComparison.Ordinal), "queue unknown field survives supported round-trip");
        True(json.Contains("futureDefaultField", StringComparison.Ordinal), "settings unknown field survives supported round-trip");
        return Task.CompletedTask;
    }

    private static Task ProjectReplacementPreservesDuplicateQueueItemsAsync()
    {
        using var fixture = new TemporaryDirectory();
        var source = fixture.CreateFile("same-source.png");
        var first = NewJob(source, MediaKind.Image);
        var second = NewJob(source, MediaKind.Image);
        var session = new ProjectSession();

        session.ReplaceJobs([first, second], markDirty: false);

        Equal(2, session.Jobs.Count, "loaded duplicate queue items are preserved");
        Equal(1, session.RemoveJobs([first]), "one duplicate can be removed independently");
        Equal(1, session.Jobs.Count, "second duplicate remains");
        Equal(0, session.AddJobs([NewJob(source, MediaKind.Image)]), "interactive import still prevents an additional duplicate");
        return Task.CompletedTask;
    }

    private static Task ProjectOutputRulesOverrideLegacyDefaultsAsync()
    {
        var document = NewProjectDocument([]);
        document.ProjectDefaults.OutputFolder = "legacy-default";
        document.ProjectDefaults.FileSuffix = "_legacy";
        document.OutputRules.OutputFolder = "canonical-output";
        document.OutputRules.FileSuffix = "_canonical";
        document.OutputRules.OutputBesideSource = true;

        var settings = ProjectDocumentMapper.CreateEffectiveSettings(document);

        Equal("canonical-output", settings.OutputFolder, "output rules folder");
        Equal("_canonical", settings.FileSuffix, "output rules suffix");
        True(settings.OutputBesideSource, "output rules beside-source flag");
        return Task.CompletedTask;
    }

    private static Task ProjectNewerSchemaOpensReadOnlyAsync()
    {
        var document = NewProjectDocument([]);
        document.SchemaVersion = ProjectDocument.CurrentSchemaVersion + 1;
        var result = new ProjectService().LoadJson(new ProjectService().Serialize(document));
        True(result.IsReadOnly, "newer schema opens read-only");
        True(result.ReadOnlyReason?.Contains("schema", StringComparison.OrdinalIgnoreCase) == true, "read-only reason names schema");
        return Task.CompletedTask;
    }

    private static Task ProjectNonPortableSavesAbsoluteReferencesAsync()
    {
        using var fixture = new TemporaryDirectory();
        var projectDirectory = Path.Combine(fixture.Path, "project");
        Directory.CreateDirectory(projectDirectory);
        var sourcePath = fixture.CreateFile("project/source.png");
        var projectPath = Path.Combine(projectDirectory, "absolute.mediaforge");

        var document = ProjectDocumentMapper.Capture(
            [NewJob(sourcePath, MediaKind.Image)],
            new AppSettings(),
            projectPath,
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            portable: false);

        True(document.ProjectRoot is null, "non-portable project has no portable root");
        True(document.Queue[0].Source.RelativePath is null, "non-portable project does not persist a relative source");
        Equal(Path.GetFullPath(sourcePath), document.Queue[0].Source.AbsolutePath, "absolute source fallback");
        return Task.CompletedTask;
    }

    private static Task ProjectPortablePathsRejectTraversalAsync()
    {
        using var fixture = new TemporaryDirectory();
        var projectPath = Path.Combine(fixture.Path, "portable", "unsafe.mediaforge");
        Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);
        var source = new ProjectSourceReferenceDocument
        {
            ReferenceId = Guid.NewGuid(),
            FileName = "outside.png",
            RelativePath = "../outside.png",
            Kind = MediaKind.Image,
            SizeBytes = 1,
            LastWriteUtc = DateTimeOffset.UtcNow
        };
        var document = NewProjectDocument([
            new ProjectQueueItemDocument { ItemId = Guid.NewGuid(), Order = 0, Source = source }
        ]);
        document.ProjectRoot = ".";

        var result = new ProjectService().Materialize(document, projectPath);
        True(result.SourceIssues.Any(issue => issue.Kind == ProjectSourceIssueKind.UnsafeRelativePath), "traversal is reported");
        True(result.SourceIssues.Any(issue => issue.Kind == ProjectSourceIssueKind.Missing), "unsafe path is not treated as resolved");
        True(!Path.IsPathRooted(result.Jobs[0].SourcePath), "unsafe relative value is retained only as unresolved text");
        return Task.CompletedTask;
    }

    private static Task ProjectRecoveryRemainsSeparateAndBoundedAsync()
    {
        using var fixture = new TemporaryDirectory();
        var recoveryDirectory = Path.Combine(fixture.Path, "recovery");
        var canonicalPath = Path.Combine(fixture.Path, "canonical.mediaforge");
        File.WriteAllText(canonicalPath, "canonical-sentinel");
        File.SetLastWriteTimeUtc(canonicalPath, DateTime.UtcNow.AddHours(-2));
        var document = NewProjectDocument([]);
        var store = new ProjectRecoveryStore(recoveryDirectory);

        for (var index = 0; index < 7; index++)
        {
            document.ModifiedUtc = DateTimeOffset.UtcNow.AddSeconds(index);
            store.Save(document, canonicalPath);
            Thread.Sleep(2);
        }

        Equal("canonical-sentinel", File.ReadAllText(canonicalPath), "autosave never overwrites canonical save");
        var snapshots = store.GetRecoverableSnapshots();
        True(snapshots.Count is >= 1 and <= 5, "snapshot retention is bounded");
        True(snapshots.All(snapshot => snapshot.SnapshotPath.EndsWith(".mediaforge-recovery", StringComparison.OrdinalIgnoreCase)), "recovery extension");

        var invalidPath = Path.Combine(recoveryDirectory, $"{document.ProjectId:N}-invalid.mediaforge-recovery");
        File.WriteAllText(invalidPath, "{\"schemaVersion\":1,\"projectId\":\"" + document.ProjectId + "\",\"document\":{}}");
        True(store.TryRead(invalidPath) is null, "structurally invalid recovery snapshot is ignored");
        return Task.CompletedTask;
    }

    private static Task ProjectRelinkDistinguishesCandidatesAsync()
    {
        using var fixture = new TemporaryDirectory();
        var search = Path.Combine(fixture.Path, "search");
        Directory.CreateDirectory(search);
        var exactPath = Path.Combine(search, "exact.png");
        File.WriteAllBytes(exactPath, [1, 2, 3]);
        var exactTime = new DateTimeOffset(File.GetLastWriteTimeUtc(exactPath), TimeSpan.Zero);
        var changedPath = Path.Combine(search, "changed.png");
        File.WriteAllBytes(changedPath, [1, 2, 3, 4]);
        var ambiguousA = Path.Combine(search, "a", "ambiguous.png");
        var ambiguousB = Path.Combine(search, "b", "ambiguous.png");
        Directory.CreateDirectory(Path.GetDirectoryName(ambiguousA)!);
        Directory.CreateDirectory(Path.GetDirectoryName(ambiguousB)!);
        File.WriteAllBytes(ambiguousA, [9]);
        File.WriteAllBytes(ambiguousB, [9]);
        var ambiguousTime = new DateTimeOffset(File.GetLastWriteTimeUtc(ambiguousA), TimeSpan.Zero);
        File.SetLastWriteTimeUtc(ambiguousB, ambiguousTime.UtcDateTime);

        var exact = MissingQueueItem("exact.png", 3, exactTime);
        var changed = MissingQueueItem("changed.png", 99, DateTimeOffset.UtcNow.AddDays(-1));
        var ambiguous = MissingQueueItem("ambiguous.png", 1, ambiguousTime);
        var document = NewProjectDocument([exact, changed, ambiguous]);
        var service = new ProjectRelinkService();
        var plan = service.Plan(document, search);

        Equal(1, plan.ExactCount, "unique exact match count");
        Equal(1, plan.ChangedCount, "unique changed match count");
        Equal(1, plan.AmbiguousCount, "ambiguous match count");
        var applied = service.Apply(document, plan, applyExactMatches: true, includeChangedFingerprint: true);
        Equal(1, applied.ExactApplied, "exact applied");
        Equal(1, applied.ChangedApplied, "changed applied only after explicit opt-in");
        True(string.Equals(exact.Source.AbsolutePath, exactPath, StringComparison.OrdinalIgnoreCase), "exact source updated");
        True(string.Equals(changed.Source.AbsolutePath, changedPath, StringComparison.OrdinalIgnoreCase), "changed source updated");
        True(string.IsNullOrWhiteSpace(ambiguous.Source.AbsolutePath), "ambiguous source remains unresolved");
        return Task.CompletedTask;
    }

    private static Task ProjectRecentListIsBoundedAndDeduplicatedAsync()
    {
        using var fixture = new TemporaryDirectory();
        var store = new RecentProjectStore(Path.Combine(fixture.Path, "recent.json"));
        for (var index = 0; index < 12; index++)
        {
            store.Add(Path.Combine(fixture.Path, $"project-{index}.mediaforge"), Guid.NewGuid());
        }
        var duplicatePath = Path.Combine(fixture.Path, "project-5.mediaforge");
        var duplicateId = Guid.NewGuid();
        store.Add(duplicatePath, duplicateId);
        var entries = store.Load();
        Equal(10, entries.Count, "recent project maximum");
        Equal(1, entries.Count(entry => string.Equals(entry.Path, duplicatePath, StringComparison.OrdinalIgnoreCase)), "recent path deduplicated");
        Equal(duplicateId, entries[0].ProjectId, "latest duplicate moves to front");
        return Task.CompletedTask;
    }

    private static async Task ProjectAutosaveDebouncesSnapshotsAsync()
    {
        using var fixture = new TemporaryDirectory();
        var store = new ProjectRecoveryStore(Path.Combine(fixture.Path, "recovery"));
        using var coordinator = new ProjectAutosaveCoordinator(store, TimeSpan.FromMilliseconds(60));
        var saved = 0;
        coordinator.SnapshotSaved += (_, _) => Interlocked.Increment(ref saved);
        var first = NewProjectDocument([]);
        var second = NewProjectDocument([]);
        second.ProjectId = first.ProjectId;
        coordinator.Schedule(first, null);
        await Task.Delay(20);
        coordinator.Schedule(second, null);
        await Task.Delay(250);

        Equal(1, Volatile.Read(ref saved), "debounced save count");
        Equal(1, store.GetRecoverableSnapshots().Count, "one discoverable recovery snapshot");
    }

    private static ProjectDocument NewProjectDocument(IReadOnlyList<ProjectQueueItemDocument> queue) => new()
    {
        SchemaVersion = ProjectDocument.CurrentSchemaVersion,
        ApplicationVersion = "test",
        ProjectId = Guid.NewGuid(),
        CreatedUtc = DateTimeOffset.UtcNow,
        ModifiedUtc = DateTimeOffset.UtcNow,
        Queue = queue.ToList(),
        ProjectDefaults = new AppSettings(),
        OutputRules = new ProjectOutputRulesDocument()
    };

    private static ProjectQueueItemDocument MissingQueueItem(string fileName, long size, DateTimeOffset lastWriteUtc) => new()
    {
        ItemId = Guid.NewGuid(),
        Order = 0,
        Source = new ProjectSourceReferenceDocument
        {
            ReferenceId = Guid.NewGuid(),
            FileName = fileName,
            Kind = MediaKind.Image,
            SizeBytes = size,
            LastWriteUtc = lastWriteUtc
        }
    };

    private static Task PresetCatalogueEnforcesOwnershipAsync()
    {
        using var fixture = new TemporaryDirectory();
        var service = new PresetService(Path.Combine(fixture.Path, "presets"));
        var catalogue = service.LoadCatalogue();
        True(catalogue.Any(preset => preset.IsBuiltIn), "built-in catalogue exists");
        var builtIn = catalogue.First(preset => preset.IsBuiltIn);
        Throws<InvalidOperationException>(() => service.Delete(builtIn));

        var created = service.CreateFromSettings(new AppSettings { ImageQuality = 91 }, "Fixture", "Tests", "fixture preset", [nameof(AppSettings.ImageQuality)]);
        Equal(created.Id, service.LoadCatalogue().Single(preset => preset.Id == created.Id).Id, "created preset persisted");
        var updated = service.UpdateUserPreset(created, "Renamed", "Moved", "updated", []);
        Equal("Renamed", updated.Name, "rename");
        Equal("Moved", updated.Group, "move group");
        var duplicate = service.Duplicate(updated, "Duplicate");
        True(duplicate.Id != updated.Id, "duplicate has independent ID");
        service.SetDefault(duplicate.Id);
        Equal(duplicate.Id, service.LoadState().DefaultPresetId, "default selection");
        service.Delete(updated);
        True(service.LoadCatalogue().All(preset => preset.Id != updated.Id), "user preset deleted");
        return Task.CompletedTask;
    }

    private static Task PresetCatalogueFallsBackWhenStorageUnavailableAsync()
    {
        using var fixture = new TemporaryDirectory();
        var blockedPath = fixture.CreateFile("not-a-directory");
        var service = new PresetService(blockedPath);
        var catalogue = service.LoadCatalogue();
        True(catalogue.Count > 0 && catalogue.All(preset => preset.IsBuiltIn),
            "built-ins remain usable when the user catalogue path cannot be created");
        return Task.CompletedTask;
    }

    private static Task ApplicationDataOverridesIsolatePersistentStoresAsync()
    {
        using var fixture = new TemporaryDirectory();
        var roamingRoot = Path.Combine(fixture.Path, "roaming");
        var localRoot = Path.Combine(fixture.Path, "local");
        var previousRoaming = Environment.GetEnvironmentVariable(ApplicationDataPaths.RoamingRootEnvironmentVariable);
        var previousLocal = Environment.GetEnvironmentVariable(ApplicationDataPaths.LocalRootEnvironmentVariable);
        try
        {
            Environment.SetEnvironmentVariable(ApplicationDataPaths.RoamingRootEnvironmentVariable, roamingRoot);
            Environment.SetEnvironmentVariable(ApplicationDataPaths.LocalRootEnvironmentVariable, localRoot);

            var settings = new SettingsService();
            settings.Save(new AppSettings { ParallelJobs = 3 });
            True(File.Exists(Path.Combine(roamingRoot, "MediaForge", "settings.json")), "settings use isolated roaming root");

            var recent = new RecentProjectStore();
            recent.Add(Path.Combine(fixture.Path, "project.mediaforge"), Guid.NewGuid());
            True(File.Exists(Path.Combine(roamingRoot, "MediaForge", "recent-projects.json")), "recent projects use isolated roaming root");

            var presets = new PresetService();
            presets.CreateFromSettings(new AppSettings(), "Isolated", "Tests", "isolated storage");
            True(Directory.EnumerateFiles(Path.Combine(roamingRoot, "MediaForge", "Presets"), "*.mediaforge-preset").Any(),
                "presets use isolated roaming root");

            var recovery = new ProjectRecoveryStore();
            var snapshot = recovery.Save(NewProjectDocument([]), null);
            True(snapshot.SnapshotPath.StartsWith(Path.Combine(localRoot, "MediaForge", "Recovery"), StringComparison.OrdinalIgnoreCase),
                "recovery uses isolated local root");
        }
        finally
        {
            Environment.SetEnvironmentVariable(ApplicationDataPaths.RoamingRootEnvironmentVariable, previousRoaming);
            Environment.SetEnvironmentVariable(ApplicationDataPaths.LocalRootEnvironmentVariable, previousLocal);
        }
        return Task.CompletedTask;
    }

    private static Task PresetImportRejectsUnsafeFieldsAsync()
    {
        using var fixture = new TemporaryDirectory();
        var service = new PresetService(Path.Combine(fixture.Path, "presets"));
        var path = Path.Combine(fixture.Path, "unsafe.mediaforge-preset");
        File.WriteAllText(path, """
        {
          "schemaVersion": 1,
          "id": "9bd7152e-da39-4919-a60b-9409a5ed2730",
          "version": "1.0",
          "name": "Unsafe",
          "group": "Tests",
          "mediaKinds": [0],
          "values": { "rawArguments": ["-y", "malicious"] },
          "lockedFields": []
        }
        """);
        Throws<InvalidDataException>(() => service.Import(path));

        var compatiblePath = Path.Combine(fixture.Path, "compatible.mediaforge-preset");
        File.WriteAllText(compatiblePath, """
        {
          "schemaVersion": 1,
          "id": "18b77389-bfdb-4674-b4cb-e34056ca7427",
          "version": "1.0",
          "name": "Compatible future fields",
          "group": "Tests",
          "description": "safe unknown fields are retained",
          "mediaKinds": [0],
          "values": { "imageQuality": 88, "futureQualityHint": "balanced" },
          "lockedFields": [],
          "futureLabel": "retained"
        }
        """);
        var imported = service.Import(compatiblePath);
        True(imported.Extensions?.ContainsKey("futureLabel") == true, "safe document extension preserved");
        True(imported.Values.Extensions?.ContainsKey("futureQualityHint") == true, "safe values extension preserved");
        return Task.CompletedTask;
    }

    private static Task EffectiveOptionsApplyPresetAndJobPrecedenceAsync()
    {
        using var fixture = new TemporaryDirectory();
        var ffmpeg = fixture.CreateFile("tools/ffmpeg.exe");
        fixture.CreateFile("tools/ffprobe.exe");
        var job = NewJob(fixture.CreateFile("image.png"), MediaKind.Image);
        var preset = new PresetDocument
        {
            Id = Guid.NewGuid(),
            Name = "Preset",
            Group = "Tests",
            MediaKinds = [MediaKind.Image],
            Values = new ConversionOptionOverrides { ImageQuality = 70, FileSuffix = "_preset" }
        };
        var overrides = new ConversionOptionOverrides { ImageQuality = 95 };
        var result = new EffectiveOptionsResolver().Resolve(new EffectiveOptionsRequest(
            CreateValidOptionInput(ffmpeg, Path.Combine(fixture.Path, "output")), [job], preset, overrides));
        True(result.Success, result.Error);
        Equal(95, result.Options!.ImageQuality, "job override wins");
        Equal("_preset", result.Options.FileSuffix, "preset wins over global");
        Equal(EffectiveOptionSource.JobOverride, result.Sources[nameof(AppSettings.ImageQuality)], "source map job override");
        Equal(EffectiveOptionSource.Preset, result.Sources[nameof(AppSettings.FileSuffix)], "source map preset");

        preset.LockedFields = [nameof(AppSettings.ImageQuality)];
        var locked = new EffectiveOptionsResolver().Resolve(new EffectiveOptionsRequest(
            CreateValidOptionInput(ffmpeg, Path.Combine(fixture.Path, "output2")), [job], preset, overrides));
        True(!locked.Success && locked.Error.Contains("locks", StringComparison.OrdinalIgnoreCase), "locked field rejects override");
        return Task.CompletedTask;
    }

    private static Task ProjectPreservesPresetSnapshotsAsync()
    {
        using var fixture = new TemporaryDirectory();
        var source = fixture.CreateFile("source.png");
        var presetId = Guid.NewGuid();
        var job = NewJob(source, MediaKind.Image);
        job.Enabled = false;
        job.Priority = 7;
        job.SelectedPresetId = presetId;
        job.SelectedPresetVersion = "1.0";
        job.SelectedPresetName = "Deleted later";
        job.SelectedPresetSnapshot = new ConversionOptionOverrides { ImageQuality = 72 };
        job.PerJobOverrides = new ConversionOptionOverrides { FileSuffix = "_job" };
        var globalPreset = new PresetDocument
        {
            Id = Guid.NewGuid(), Name = "Global", Group = "Tests", Version = "1.0",
            Values = new ConversionOptionOverrides { ImageFormat = "PNG" },
            MediaKinds = [MediaKind.Image]
        };
        var projectPath = Path.Combine(fixture.Path, "workflow.mediaforge");
        var document = ProjectDocumentMapper.Capture(
            [job], new AppSettings(), projectPath, Guid.NewGuid(), DateTimeOffset.UtcNow, false,
            selectedPreset: globalPreset);
        var service = new ProjectService();
        service.Save(document, projectPath);
        var loaded = service.Load(projectPath);
        Equal(globalPreset.Id, loaded.Document.SelectedPresetId, "global preset reference");
        Equal("PNG", loaded.Document.SelectedPresetSnapshot!.ImageFormat, "global snapshot");
        True(!loaded.Jobs[0].Enabled, "enabled state");
        Equal(7, loaded.Jobs[0].Priority, "priority");
        Equal(presetId, loaded.Jobs[0].SelectedPresetId, "job preset reference");
        Equal(72, loaded.Jobs[0].SelectedPresetSnapshot!.ImageQuality, "job preset snapshot");
        Equal("_job", loaded.Jobs[0].PerJobOverrides!.FileSuffix, "job overrides");
        return Task.CompletedTask;
    }

    private static Task ProjectSessionSupportsQueueMutationsAsync()
    {
        var first = NewJob("first.png", MediaKind.Image);
        var second = NewJob("second.png", MediaKind.Image);
        var third = NewJob("third.png", MediaKind.Image);
        var session = new ProjectSession();
        session.ReplaceJobs([first, second, third]);
        True(session.MoveJobs([third], -1), "move up");
        Equal(third.Id, session.Jobs[1].Id, "reordered identity");
        session.SetEnabled([first, third], false);
        True(!first.Enabled && !third.Enabled, "bulk disable");
        session.AdjustPriority([second], 5);
        Equal(5, second.Priority, "priority adjustment");
        Equal(1, session.DuplicateJobs([second]), "duplicate count");
        Equal(4, session.Jobs.Count, "queue count after duplicate");
        Equal(second.Id, session.Jobs[2].Id, "selected item keeps its position");
        True(session.Jobs[3].Id != second.Id && session.Jobs[3].SourcePath == second.SourcePath, "duplicate is inserted immediately after its source item");
        True(session.Jobs.Select(job => job.Id).Distinct().Count() == 4, "stable unique IDs");
        return Task.CompletedTask;
    }

    private static Task QueueDocumentRoundTripsAsync()
    {
        using var fixture = new TemporaryDirectory();
        var source = fixture.CreateFile("queue-source.mp3");
        var path = Path.Combine(fixture.Path, "fixture.mediaforge-queue");
        var job = NewJob(source, MediaKind.Audio);
        job.Enabled = false;
        job.Priority = 4;
        job.SelectedPresetId = Guid.NewGuid();
        job.SelectedPresetName = "Queue preset";
        job.SelectedPresetVersion = "1.0";
        job.SelectedPresetSnapshot = new ConversionOptionOverrides { AudioFormat = "FLAC" };
        job.PerJobOverrides = new ConversionOptionOverrides { AudioNormalize = true };
        var service = new QueueService();
        var document = service.Capture([job], path);
        service.Save(document, path);
        var loaded = service.Load(path);
        True(!loaded.IsReadOnly, "queue schema writable");
        Equal(1, loaded.Jobs.Count, "queue count");
        True(!loaded.Jobs[0].Enabled, "enabled round-trip");
        Equal(4, loaded.Jobs[0].Priority, "priority round-trip");
        Equal("FLAC", loaded.Jobs[0].SelectedPresetSnapshot!.AudioFormat, "preset snapshot round-trip");
        True(loaded.Jobs[0].PerJobOverrides!.AudioNormalize == true, "override round-trip");
        return Task.CompletedTask;
    }

    private static Task QueueRunItemFreezesSourceAndEditPlanAsync()
    {
        var job = NewJob("snapshot.png", MediaKind.Image);
        job.Priority = 4;
        job.EditPlan = new MediaEditPlan { ImageCrop = new CropSelection(0.1, 0.1, 0.8, 0.8) };
        var item = new QueueRunItem(job, CreateConversionOptions());
        job.Priority = 99;
        job.EditPlan!.ImageCrop = CropSelection.Full;

        True(!ReferenceEquals(job, item.Snapshot), "run snapshot is independent");
        Equal(job.Id, item.Snapshot.Id, "run snapshot retains stable identity");
        Equal(4, item.Snapshot.Priority, "priority frozen");
        True(!item.Snapshot.EditPlan!.ImageCrop.IsFullFrame, "edit plan frozen");
        return Task.CompletedTask;
    }

    private static Task QueueEstimatesLabelSizeAndTimeAsync()
    {
        var job = new MediaJob
        {
            SourcePath = "estimate.mp4",
            Kind = MediaKind.Video,
            SourceBytes = 256L * 1024 * 1024,
            SourceLastWriteUtc = DateTimeOffset.UtcNow
        };
        var estimate = new QueueEstimateService().Estimate(job, CreateConversionOptions());
        True(estimate.EstimatedBytes.HasValue, "size estimate available");
        True(estimate.EstimatedProcessingTime.HasValue, "time estimate available");
        True(estimate.Display.Contains("Est.", StringComparison.Ordinal) &&
             estimate.Display.Contains("confidence", StringComparison.OrdinalIgnoreCase), "estimate is explicitly labelled");
        return Task.CompletedTask;
    }

    private static async Task QueueCoordinatorPauseStopsDispatchAsync()
    {
        var conversion = new ControlledConversionService(TimeSpan.FromMilliseconds(220));
        var coordinator = new QueueCoordinator(conversion);
        var jobs = Enumerable.Range(0, 3).Select(index => NewJob($"pause-{index}.png", MediaKind.Image)).ToList();
        var run = coordinator.RunAsync(jobs, CreateConversionOptions(), 1, null, CancellationToken.None);
        await conversion.FirstStart.Task.WaitAsync(TimeSpan.FromSeconds(3));
        coordinator.PauseAfterCurrent();
        True(coordinator.IsDispatchPaused, "pause state");
        await Task.Delay(350);
        Equal(1, jobs.Count(job => job.State == JobState.Completed), "only current job completes while paused");
        Equal(2, jobs.Count(job => job.State == JobState.Pending), "pending jobs remain undispatched");
        True(!run.IsCompleted, "queue remains active while paused");
        coordinator.Resume();
        var result = await run.WaitAsync(TimeSpan.FromSeconds(5));
        True(!result.WasCancelled, "resumed queue completed");
        True(jobs.All(job => job.State == JobState.Completed), "all jobs complete after resume");
    }

    private static async Task QueueCoordinatorOwnsParallelExecutionAsync()
    {
        var conversion = new ControlledConversionService(TimeSpan.FromMilliseconds(80));
        var coordinator = new QueueCoordinator(conversion);
        var jobs = Enumerable.Range(0, 5)
            .Select(index => NewJob($"job-{index}.png", MediaKind.Image))
            .ToList();
        var stateEvents = 0;
        coordinator.StateChanged += (_, _) => Interlocked.Increment(ref stateEvents);

        var result = await coordinator.RunAsync(jobs, CreateConversionOptions(), 2, null, CancellationToken.None);

        True(!result.WasCancelled, "queue completed without cancellation");
        True(!coordinator.IsRunning, "coordinator resets running state");
        True(jobs.All(job => job.State == JobState.Completed && job.Progress == 100), "all jobs completed");
        True(conversion.MaximumConcurrency <= 2 && conversion.MaximumConcurrency >= 1, "parallelism bounded at two");
        True(stateEvents >= jobs.Count * 2, "state events emitted");
    }

    private static async Task QueueCoordinatorCancellationTransitionsJobsAsync()
    {
        var conversion = new ControlledConversionService(TimeSpan.FromSeconds(10));
        var coordinator = new QueueCoordinator(conversion);
        var jobs = Enumerable.Range(0, 3)
            .Select(index => NewJob($"cancel-{index}.png", MediaKind.Image))
            .ToList();

        var run = coordinator.RunAsync(jobs, CreateConversionOptions(), 1, null, CancellationToken.None);
        await conversion.FirstStart.Task.WaitAsync(TimeSpan.FromSeconds(3));
        coordinator.Cancel();
        var result = await run.WaitAsync(TimeSpan.FromSeconds(5));

        True(result.WasCancelled, "queue reports cancellation");
        True(jobs.All(job => job.State == JobState.Cancelled), "all candidates transition to cancelled");
    }

    private static async Task ImagePreflightRejectsOversizedDecodedFramesAsync()
    {
        using var fixture = new TemporaryDirectory();
        var small = Path.Combine(fixture.Path, "small.png");
        WritePngHeader(small, 1920, 1080, bitDepth: 8, colourType: 2);
        var smallInfo = ImageSourcePreflight.ValidateForFfmpeg(small);
        Equal(1920, smallInfo!.Width, "small PNG width");

        var huge = Path.Combine(fixture.Path, "huge.png");
        WritePngHeader(huge, 38400, 21600, bitDepth: 8, colourType: 2);
        var error = await ThrowsAsync<InvalidOperationException>(() =>
            Task.Run(() => ImageSourcePreflight.ValidateForFfmpeg(huge)));
        True(error.Message.Contains("38,400 × 21,600", StringComparison.Ordinal), "dimensions included");
        True(error.Message.Contains("decode the full source", StringComparison.Ordinal), "decode limitation explained");
    }

    private static async Task ConversionPreservesSourceAndCommitsAtomicallyAsync()
    {
        using var fixture = new TemporaryDirectory();
        var source = Path.Combine(fixture.Path, "source.png");
        WritePngHeader(source, 64, 64, bitDepth: 8, colourType: 2);
        var sourceHash = Sha256(source);
        var output = Path.Combine(fixture.Path, "output");
        var service = new MediaConversionService(new OutputWritingProcessRunner());
        var options = CreateConversionOptions(output) with { FileSuffix = "_converted", CollisionPolicy = "Rename" };

        var first = await service.ConvertAsync(NewJob(source, MediaKind.Image), options, null, null, CancellationToken.None);
        var second = await service.ConvertAsync(NewJob(source, MediaKind.Image), options, null, null, CancellationToken.None);

        Equal(sourceHash, Sha256(source), "source hash unchanged");
        True(File.Exists(first.OutputPath) && File.Exists(second.OutputPath), "committed outputs exist");
        True(!string.Equals(first.OutputPath, second.OutputPath, StringComparison.OrdinalIgnoreCase), "collision renamed");
        True(!Directory.EnumerateFiles(output, "*.partial.*", SearchOption.AllDirectories).Any(), "no temporary files remain");
    }

    private static async Task ConversionFailureCleansTemporaryOutputAsync()
    {
        using var fixture = new TemporaryDirectory();
        var source = Path.Combine(fixture.Path, "source.png");
        WritePngHeader(source, 64, 64, bitDepth: 8, colourType: 2);
        var output = Path.Combine(fixture.Path, "output");
        var service = new MediaConversionService(new OutputWritingProcessRunner(fail: true));
        var options = CreateConversionOptions(output) with { FileSuffix = "_failed" };

        await ThrowsAsync<InvalidOperationException>(() =>
            service.ConvertAsync(NewJob(source, MediaKind.Image), options, null, null, CancellationToken.None));

        True(!Directory.EnumerateFiles(output, "*", SearchOption.AllDirectories).Any(), "failed output and temporary file removed");
    }

    private static ConversionOptionInput CreateValidOptionInput(string ffmpegPath, string outputFolder) => new()
    {
        FfmpegPath = ffmpegPath,
        OutputBesideSource = false,
        OutputFolder = outputFolder,
        PreserveFolderTree = true,
        PreserveTimestamps = true,
        StripMetadata = false,
        FileSuffix = "_converted",
        CollisionPolicy = "Rename",
        ParallelJobs = "2",
        ImageFormat = "JPEG",
        ImageQuality = 85,
        ImageResizeMode = "None",
        ImageWidth = "1920",
        ImageHeight = "1080",
        ImageScalePercent = "100",
        ImageAspectRatio = "Free",
        ImageCustomAspectWidth = "16",
        ImageCustomAspectHeight = "9",
        VideoContainer = "MP4",
        VideoCodec = "H.264",
        VideoCrf = 23,
        VideoPreset = "medium",
        VideoResolution = "Keep",
        VideoWidth = "1920",
        VideoHeight = "1080",
        VideoResizeMode = "Fit",
        VideoAspectRatio = "16:9",
        VideoCustomAspectWidth = "16",
        VideoCustomAspectHeight = "9",
        VideoFps = "Keep",
        VideoCustomFps = "30",
        VideoAudioCodec = "AAC",
        VideoAudioBitrate = "192",
        ExtractAudioOnly = false,
        AudioFormat = "MP3",
        AudioBitrate = "192",
        AudioSampleRate = "Keep",
        AudioChannels = "Keep",
        AudioNormalize = false
    };

    private static ConversionOptions CreateConversionOptions(string? outputFolder = null) => new()
    {
        FfmpegPath = "ffmpeg.exe",
        FfprobePath = "ffprobe.exe",
        OutputFolder = outputFolder ?? Path.Combine(Path.GetTempPath(), "MediaForge-tests-output"),
        OutputBesideSource = false,
        PreserveFolderTree = false,
        PreserveTimestamps = false,
        StripMetadata = false,
        FileSuffix = "_converted",
        CollisionPolicy = "Rename",
        ImageFormat = "JPEG",
        ImageQuality = 85,
        ImageResizeMode = "None",
        ImageWidth = 1920,
        ImageHeight = 1080,
        ImageScalePercent = 100,
        ImageAspectRatio = "Free",
        ImageCustomAspectWidth = 16,
        ImageCustomAspectHeight = 9,
        VideoContainer = "MP4",
        VideoCodec = "H.264",
        VideoCrf = 23,
        VideoPreset = "medium",
        VideoResolution = "Keep",
        VideoWidth = 1920,
        VideoHeight = 1080,
        VideoResizeMode = "Fit",
        VideoAspectRatio = "16:9",
        VideoCustomAspectWidth = 16,
        VideoCustomAspectHeight = 9,
        VideoFps = "Keep",
        VideoCustomFps = 30,
        VideoAudioCodec = "AAC",
        VideoAudioBitrate = 192,
        ExtractAudioOnly = false,
        AudioFormat = "MP3",
        AudioBitrate = 192,
        AudioSampleRate = "Keep",
        AudioChannels = "Keep",
        AudioNormalize = false
    };

    private static MediaJob NewJob(string sourcePath, MediaKind kind) => new()
    {
        SourcePath = sourcePath,
        Kind = kind,
        SourceBytes = 1
    };

    private static void WritePngHeader(string path, int width, int height, byte bitDepth, byte colourType)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var bytes = new byte[33];
        new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }.CopyTo(bytes, 0);
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(8, 4), 13);
        "IHDR"u8.CopyTo(bytes.AsSpan(12, 4));
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(16, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(20, 4), height);
        bytes[24] = bitDepth;
        bytes[25] = colourType;
        File.WriteAllBytes(path, bytes);
    }

    private static string Sha256(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static async Task<int> RunChildAsync(string[] args)
    {
        if (args.Length == 0) return 64;
        switch (args[0])
        {
            case "echo-arguments":
                Console.WriteLine(JsonSerializer.Serialize(args.Skip(1).ToArray()));
                return 0;
            case "write-lines":
                var count = int.Parse(args[1]);
                for (var index = 0; index < count; index++)
                {
                    Console.WriteLine($"out-{index}");
                    Console.Error.WriteLine($"err-{index}");
                }
                return 0;
            case "exit":
                return int.Parse(args[1]);
            case "spawn-grandchild":
                using (var child = StartSelf("delayed-sentinel", [args[1]]))
                {
                    Console.WriteLine($"spawned:{child.Id}");
                    await Task.Delay(TimeSpan.FromSeconds(30));
                }
                return 0;
            case "delayed-sentinel":
                await Task.Delay(TimeSpan.FromSeconds(1.5));
                File.WriteAllText(args[1], "descendant survived cancellation");
                return 0;
            default:
                return 64;
        }
    }

    private static ProcessInvocation CreateSelfInvocation(string mode, IReadOnlyList<string> args)
    {
        var processPath = Environment.ProcessPath
            ?? throw new InvalidOperationException("The current process path is unavailable.");
        var arguments = new List<string>();
        if (string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            arguments.Add(Assembly.GetExecutingAssembly().Location);
        }
        arguments.Add("--child");
        arguments.Add(mode);
        arguments.AddRange(args);
        return new ProcessInvocation(processPath, arguments);
    }

    private static Process StartSelf(string mode, IReadOnlyList<string> args)
    {
        var invocation = CreateSelfInvocation(mode, args);
        var startInfo = new ProcessStartInfo
        {
            FileName = invocation.FileName,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in invocation.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }
        return Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start test child process.");
    }

    private static string[] SplitLines(string value) =>
        value.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

    private static TException Throws<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException ex)
        {
            return ex;
        }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    private static async Task<TException> ThrowsAsync<TException>(Func<Task> action) where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException ex)
        {
            return ex;
        }
        throw new InvalidOperationException($"Expected {typeof(TException).Name}.");
    }

    private static void Equal<T>(T expected, T actual, string description)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{description}: expected '{expected}', actual '{actual}'.");
        }
    }

    private static void SequenceEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual, string description)
    {
        if (!expected.SequenceEqual(actual))
        {
            throw new InvalidOperationException(
                $"{description}: expected [{string.Join(", ", expected)}], actual [{string.Join(", ", actual)}].");
        }
    }

    private static void True(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Test cleanup must not hide the original assertion.
        }
    }

    private sealed record ProcessInvocation(string FileName, IReadOnlyList<string> Arguments);

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"MediaForge-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateFile(string relativePath)
        {
            var fullPath = System.IO.Path.Combine(Path, relativePath.Replace('/', System.IO.Path.DirectorySeparatorChar));
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
            File.WriteAllBytes(fullPath, [0]);
            return fullPath;
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
            }
            catch
            {
                // Test cleanup is best-effort.
            }
        }
    }

    private sealed class ControlledConversionService : IMediaConversionService
    {
        private readonly TimeSpan _delay;
        private int _active;
        private int _maximumConcurrency;

        public ControlledConversionService(TimeSpan delay)
        {
            _delay = delay;
        }

        public int MaximumConcurrency => Volatile.Read(ref _maximumConcurrency);
        public TaskCompletionSource<bool> FirstStart { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<ConversionResult> ConvertAsync(
            MediaJob job,
            ConversionOptions options,
            IProgress<double>? progress,
            Action<string>? log,
            CancellationToken cancellationToken)
        {
            var active = Interlocked.Increment(ref _active);
            UpdateMaximum(active);
            FirstStart.TrySetResult(true);
            try
            {
                await Task.Delay(_delay, cancellationToken);
                return new ConversionResult(false, job.SourcePath + ".out", "Completed");
            }
            finally
            {
                Interlocked.Decrement(ref _active);
            }
        }

        private void UpdateMaximum(int value)
        {
            while (true)
            {
                var current = Volatile.Read(ref _maximumConcurrency);
                if (value <= current || Interlocked.CompareExchange(ref _maximumConcurrency, value, current) == current) return;
            }
        }
    }

    private sealed class OutputWritingProcessRunner : IProcessRunner
    {
        private readonly bool _fail;

        public OutputWritingProcessRunner(bool fail = false)
        {
            _fail = fail;
        }

        public Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var outputPath = request.Arguments.Last();
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, "converted");
            request.StandardOutputLineReceived?.Invoke("progress=end");
            return Task.FromResult(_fail
                ? new ProcessRunResult(1, string.Empty, "synthetic conversion failure")
                : new ProcessRunResult(0, "progress=end", string.Empty));
        }
    }
}
