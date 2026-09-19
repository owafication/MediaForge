# MediaForge Desktop

## MediaForge 2 direction

The durable V1 rollback point is `5acaf88e751327eac47ca673178fbbd88a8603f1`.

MediaForge 2 is the approved governance/product direction: keep the Windows/WPF/local-FFmpeg processing foundation and safety boundaries, replace queue-first interaction with task-first workflows and progressive disclosure, and use one typed WorkflowIntent/ProcessingPlan authority.

`PH-20` governance adoption is complete at merge commit `d3c1c0958aae71afcc5462bfa2c4e66c6fd7ab55`. The active implementation phase is `PH-22`. No V2 runtime implementation is yet claimed, and the exact 2.x package/version transition remains unresolved.

MediaForge is a local-first Windows media conversion, preparation and lightweight single-track assembly application. The source is a WPF application that invokes local FFmpeg and FFprobe child processes.

## Current source foundation

- Mixed image, video and audio queue with bounded parallel conversion and cancellation
- Recursive import, drag-drop, crop, resize, placement, trim, split, clip ordering and stitching
- Central process, option-resolution, session, queue-coordination and presentation seams from PH-08
- Versioned `.mediaforge` projects from PH-09
- New/Open/Recent/Save/Save As, dirty-state titles and startup recovery UI
- Atomic canonical save with one bounded backup and separate debounced recovery snapshots
- Missing/changed source detection, explicit relinking and traversal-contained portable paths
- Newer project schemas open read-only; compatible unknown JSON fields are retained
- Versioned built-in/user `.mediaforge-preset` catalogue with safe import/export, grouping, defaults and field locks
- Deterministic global → preset → job override → edit guard resolution with independently reported per-job settings
- Priority/order/enable/duplicate/retry/pause queue controls and independent `.mediaforge-queue` persistence
- Immutable source/edit/options run snapshots plus explicitly low-confidence size/time estimates

The application and package-free characterisation tests target `net10.0-windows` through the repository `global.json` pin. The Win64 release script publishes a **multi-file** deployment with `PublishSingleFile=false`; release-package verification requires the executable, application DLL, dependency manifest and runtime configuration. Before creating the release ZIP, the script runs profile-isolated launch/close smoke cases for absent, valid and malformed settings plus unavailable preset storage. Each case records its observed top-level window and any startup diagnostic log.

## Evidence boundary

`BR-20260911-01` passed the consolidated Windows x64 automated baseline: 39 static checks, FFmpeg/FFprobe provenance, disposable fixtures, Release restore/build, 37 characterisation tests, four isolated launch-smoke cases, multi-file publish and package audit.

`BR-20260919-01` explicitly deferred the remaining PH-09/PH-10 manual/native interaction and real conversion-safety fixtures. Those items remain Skipped/Unproven and are carried into the V2 validation programme.

The V1 rollback point is `5acaf88e751327eac47ca673178fbbd88a8603f1`. No V2 runtime, UI, migration or release result exists yet.
## Startup diagnostics

When startup fails before the main window appears, MediaForge writes a diagnostic log under:

```text
%LOCALAPPDATA%\MediaForge\Logs
```

If Local AppData is unavailable, it falls back to the Windows temporary directory under `MediaForge\Logs`. `build-release.ps1` now refuses to create the release ZIP unless the published executable presents a closeable main window in all isolated smoke cases.

## Product direction

`PH-20` adopted the reviewed MediaForge 2 governance and migration direction and is complete. The `PH-21` V1-closure bridge was already satisfied by the merged rollback baseline. `PH-22` is now active for the task-first shell/navigation foundation.

V1 capability work originally sequenced as `PH-11` through `PH-19` is preserved historically and redistributed into the V2 roadmap rather than discarded or reused as phase IDs.
## Product boundary

MediaForge is not a full non-linear editor. It does not include an unlimited multi-track timeline, complex compositing, keyframe animation, cloud accounts, collaborative editing, a plugin marketplace or generative AI by default.

## Run verification on Windows

From the project root:

```powershell
py -3 .\scripts\verify-source.py --root .
.\scripts\run-characterization-tests.ps1 -Configuration Release
.\scripts\build-release.ps1
```

For the complete evidence harness:

```powershell
.\scripts\validate-windows-baseline.ps1 `
  -SourceArchive .\MediaForge-1.1.0-ph10-progress-binding-startup-fix-multifile-source-unverified.zip
```

The release ZIP is intentionally multi-file. Keep all files together when running or distributing it.

## Status

- **V1 source:** PH-08 architecture/.NET 10, PH-09 projects/recovery and PH-10 presets/per-job queue are implemented in source.
- **Passed automated Windows scope:** consolidated x64 build/tests/launch-smoke/publish/package evidence recorded by `BR-20260911-01`.
- **Deferred V1 evidence:** remaining manual/native interaction and real conversion-safety fixtures remain Skipped/Unproven under `BR-20260919-01`.
- **Rollback:** `5acaf88e751327eac47ca673178fbbd88a8603f1`.
- **Current work:** `PH-22` task-first WPF shell/navigation foundation; runtime implementation has not yet been performed.
- **V2 runtime:** Unproven.
