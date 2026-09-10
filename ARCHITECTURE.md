# MediaForge architecture and contracts

**Purpose:** Canonical architecture and managed migration rules for the MediaForge WPF source.  
**Owner:** Technical maintainer.  
**Linked IDs:** `REQ-001`–`REQ-055`, `PH-07`–`PH-19`, `RISK-001`–`RISK-050`.

## Current implemented shape

```text
MainWindow / MediaEditorWindow (WPF interaction)
  ├─ MainViewModel (summary/progress presentation state)
  ├─ ProjectSession (jobs, revision, dirty state and project replacement)
  ├─ ProjectService / ProjectDocumentMapper (schema-v1 persistence/materialisation)
  ├─ ProjectRecoveryStore / RecentProjectStore / ProjectRelinkService
  ├─ PresetService / QueueService (versioned workflow persistence)
  ├─ EffectiveOptionsResolver (global/preset/job/edit precedence)
  ├─ QueueCoordinator (priority dispatch, pause/cancel and immutable run snapshots)
  ├─ MediaConversionService / MediaProbeService / FfmpegLocator
  └─ ProcessRunner (all production child-process lifecycle)
                                      ↓
                             ffmpeg.exe / ffprobe.exe
```

The application remains one local WPF process with explicit composition and no dependency-injection framework. Global settings remain JSON through `AppSettings`; edit plans remain attached to `MediaJob` instances and are mapped into schema-v1 `.mediaforge` project DTOs. PH-09 persistence and PH-10 preset/per-job/queue workflow contracts are implemented in source. Native Windows validation of the consolidated revision remains pending.

## PH-08 ownership

1. `ProcessRunner` owns `ProcessStartInfo.ArgumentList`, redirected output, bounded tails, cancellation and entire-process-tree termination.
2. `EffectiveOptionsResolver` owns UI-independent option validation, compatibility checks and immutable `ConversionOptions` snapshots.
3. `ProjectSession` owns queue membership, revision and dirty state. Interactive imports prevent duplicate source paths; project replacement preserves explicitly persisted duplicate queue items. Runtime progress does not dirty the project, while structural/edit changes and terminal output references do.
4. `QueueCoordinator` owns bounded parallelism, run cancellation and job state/progress/error transitions.
5. `MainViewModel` owns queue summary, total progress and running/status projection. WPF-specific file dialogs, visual editor interaction and control capture remain in code-behind.
6. `MediaConversionService` retains FFmpeg argument construction and atomic temporary-output commit pending the later canonical `ProcessingPlan` phase.

## PH-09 persistence ownership

1. `ProjectService` owns schema probing, supported-schema validation, newer-schema read-only results and canonical serialization.
2. `ProjectDocumentMapper` owns translation between persistence DTOs and current `MediaJob`/`AppSettings` models. `outputRules` are authoritative when applying a project.
3. `AtomicFile` owns sibling-temp writes, flush-to-disk, replacement/move fallback and one bounded backup.
4. `ProjectRecoveryStore` owns separate snapshot location and retention; recovery cleanup is best-effort and cannot convert a successful canonical save into a failure.
5. `ProjectPathPolicy` owns portable-root containment and rejects traversal/reparse-point resolution. Relative source paths are written only in explicit portable mode.
6. `ProjectRelinkService` plans exact, changed, ambiguous and unresolved matches; the WPF layer confirms exact and changed application separately.
7. `RecentProjectStore` owns the bounded local recent-project list. Project files remain local and can contain sensitive local paths.


## PH-10 workflow ownership

1. `PresetService` owns the built-in/read-only and user preset catalogue, default selection, atomic import/export and rejection of executable/raw-argument fields. Safe schema-v1 unknown fields remain extension data.
2. `ConversionOptionOverrides` is a typed partial settings contract. `EffectiveOptionsResolver` alone applies global input, preset values, job overrides and edit guards, and returns field-source metadata.
3. Projects and queues retain preset IDs, versions, names and typed snapshots so deleting a catalogue entry does not erase existing effective values.
4. `QueueService` owns schema-v1 `.mediaforge-queue` capture/save/load independently of project defaults.
5. `ProjectSession` owns stable queue identity, reorder, enable/disable, priority and duplication. WPF disables mutations while a run is active.
6. `QueueRunItem` freezes source identity, edit state and `ConversionOptions`; `QueueCoordinator` dispatches by priority then stable order and pauses only future dispatch.
7. `QueueEstimateService` exposes low-confidence source-size heuristics for output size and processing time. Estimates never block execution.

## Migration strategy

Retain WPF, one local application process and external CLI tools. Extract only seams that improve correctness and testability. Code-behind may retain purely visual WPF interactions. The repository now targets `net10.0-windows`; product version and settings/installer contracts remain at 1.1.0 until the workflow milestone is release-ready.

## Data contracts

- `AppSettings`: device-wide preferences and last-used defaults; PH-09 adds compatible unknown-field preservation.
- `.mediaforge`: implemented PH-09 schema-v1 UTF-8 JSON project format; canonical manual saves use atomic replacement and recovery snapshots remain separate.
- `.mediaforge-preset`: implemented PH-10 schema-v1 typed preset format with extension-data preservation and import safety scanning.
- `.mediaforge-queue`: implemented PH-10 schema-v1 independent queue format.
- Recovery snapshots: implemented separate `.mediaforge-recovery` local files, never canonical manual saves.
- History: proposed bounded rolling JSONL; not required for 1.2.0.

Persistent imported formats must be versioned typed data and must never contain raw executable command fragments.

## Processing and output safety

- Use `ProcessStartInfo.ArgumentList`; never concatenate executable command lines.
- Centralise child-process cancellation and process-tree termination.
- Never intentionally modify source media.
- Reserve destinations deterministically for the run snapshot.
- Write temporary output beside the intended destination, then commit atomically where the filesystem permits.
- On failure or cancellation, remove temporary output and preserve prior destinations.
- Very large images are preflighted from headers. A rejected source remains unchanged and FFmpeg is not started.

## Later canonical processing contract

A future immutable `ProcessingPlan` will own input identities, streams, trims/crops/filter intent, output container/codecs, metadata policy, hardware path, expected output and verification contract. UI summaries, FFmpeg arguments, preview and verification must eventually derive from that same plan.

## Extension rule

Add a format, codec, hardware path, filter or metadata operation end-to-end: capability discovery → typed option → compatibility rule → processing plan → UI reason/summary → FFmpeg compilation → fixture → verification → documentation. Unknown remains unsupported or unproven.
