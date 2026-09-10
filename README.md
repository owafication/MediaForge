# MediaForge Desktop

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

User-provided Windows evidence verifies that PH-10 compiles. The first startup correction then failed one or more launch-smoke cases. A supplied valid recovery snapshot exposed that the harness did not fully isolate Windows special-folder storage and that recovery prompting occurred before the main window rendered. This revision adds explicit MediaForge roaming/local roots for smoke cases, post-render recovery prompting, recovery failure containment and per-case smoke diagnostics. Native confirmation remains pending because .NET, PowerShell and the Windows runtime are unavailable here.

## Startup diagnostics

When startup fails before the main window appears, MediaForge writes a diagnostic log under:

```text
%LOCALAPPDATA%\MediaForge\Logs
```

If Local AppData is unavailable, it falls back to the Windows temporary directory under `MediaForge\Logs`. `build-release.ps1` now refuses to create the release ZIP unless the published executable presents a closeable main window in all isolated smoke cases.

## Product direction

PH-10 is now implemented in source under the user-authorised risk exception. The next phase is PH-11 capability-aware FFmpeg discovery and compatibility, but it remains gated on a clean Windows build/test/UI run for the consolidated PH-09/PH-10 revision.

Later stages add capability-aware FFmpeg planning, FFmpeg-backed preview, hardware profiles, stream/metadata control, lossless operations, output verification, history, guarded automation and desktop polish.

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

`Implemented in source`: PH-08 architecture/.NET 10; PH-09 projects/recovery; PH-10 presets, typed per-job precedence, professional queue controls, immutable run snapshots and advisory estimates; Win64 `SingleFile=false` publishing.  
`Passed in available scope`: 36 deterministic static checks over 123 non-generated files, Python compilation, XML/XAML parsing, handler/ownership checks, explicit OneWay progress binding, normalised startup diagnostics, post-render recovery containment, profile isolation, PH-09/PH-10 contract checks and mandatory launch-smoke packaging guards.  
`User-provided Windows evidence`: restore/build succeeded, all 37 characterisation tests passed and multi-file publish completed; launch diagnostics then proved a TwoWay binding attempt against read-only `OverallProgress`.  
`Unproven for this exact corrected PH-10 revision`: repeat native build/tests, the four-case profile-isolated WPF launch smoke, project/preset/queue workflows, Windows filesystem fixtures, FFmpeg safety fixtures, final multi-file release package and installer.
