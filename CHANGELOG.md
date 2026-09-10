# Changelog

All notable MediaForge changes should be recorded here. Source presence is not a release claim.

## [1.2.0] — Proposed

### PH-08 implemented in source

- Migrated application and package-free test projects to `net10.0-windows` with repository SDK pinning.
- Centralised production child-process lifecycle behind `ProcessRunner`.
- Extracted UI-independent effective-option validation and compatibility resolution.
- Added in-memory `ProjectSession` ownership for jobs, revision and dirty state.
- Added `QueueCoordinator` ownership for bounded parallel execution, cancellation and job-state transitions.
- Added a minimal `MainViewModel` for queue summary, totals, progress and running-state projection.
- Expanded architecture and source/output safety characterisation tests.
- Added conservative oversized-image preflight with actionable FFmpeg failure messaging.
- Preserved product version 1.1.0, settings JSON and installer contracts pending release evidence.
- Resolved CA2014 in JPEG header probing by moving fixed-size stack buffers outside the marker loop.
- Isolated publish intermediates from the normal `obj` tree and added bounded fresh-path retries for file-lock/access-denied failures.
- Changed Win64 release publishing to `PublishSingleFile=false`; package audit now requires the multi-file application payload and rejects a `singleFile=true` manifest.

### PH-09 implemented in source

- Added schema-v1 UTF-8 JSON `.mediaforge` project DTOs with stable project, queue-item and source-reference IDs.
- Added project New/Open/Recent/Save/Save As commands, dirty-state display and newer-schema read-only handling.
- Added atomic sibling-temp canonical save with a bounded `.bak` backup.
- Added separate debounced recovery snapshots, count/age retention and startup Recover/Open read-only/Discard/Inspect workflow.
- Added source size/mtime fingerprints, missing/changed reporting and explicit exact/changed/ambiguous relink handling.
- Added opt-in portable relative paths contained inside the canonical project root, including traversal and reparse-point rejection.
- Preserved compatible unknown JSON fields and loaded duplicate queue items without weakening interactive duplicate-import prevention.
- Added PH-09 characterisation coverage for round-trip state, forward schema, output-rule precedence, duplicate queues, recovery, recent projects, relinking, autosave and portable paths.
- Added explicit `System.IO` imports across all PH-09 persistence, recovery, relink, project UI and test files after Windows compilation exposed missing `Path`/`File`/stream symbols; added explicit nullable `Guid?` target typing for optional selected-item state and full static regression guards.

### PH-10 implemented in source

- Added schema-v1 `.mediaforge-preset` and `.mediaforge-queue` typed JSON contracts with atomic writes and compatible extension-data preservation.
- Added grouped read-only built-ins plus user preset create, rename/group, duplicate, delete, default, import and export workflows.
- Rejected imported executable-path, command and raw-argument property names before catalogue adoption.
- Added typed partial overrides, locked fields and deterministic global → preset → job → edit resolution with field-source reporting.
- Persisted global and per-job preset references plus typed snapshots in projects and queues so deleted catalogue entries do not erase workflow values.
- Added stable queue reorder, priority, enable/disable, duplicate, selected retry, source/output navigation, diagnostics and independent queue save/load.
- Added pause-after-current/resume dispatch without suspending active FFmpeg writes.
- Added immutable source/edit/options run snapshots and low-confidence size/time estimates that never block processing.
- Added PH-10 characterisation coverage and expanded deterministic static verification.
- Hardened PH-10 startup after a Windows build was reported to exit before showing a window: explicit application startup now records crash diagnostics, preset-catalogue storage failures fall back to built-ins/global settings, and workflow initialization can be contained without terminating the core converter.
- Made `build-release.ps1` run the isolated launch/close smoke test before packaging, including a blocked preset-storage fixture; release packaging now fails when the executable does not present a closeable main window.
- Corrected launch-smoke profile isolation after a real recovery snapshot leaked into the smoke process through Windows special-folder resolution: MediaForge now honours explicit roaming/local storage roots for settings, presets, recent projects, recovery, bundled FFmpeg discovery and startup logs.
- Deferred startup recovery prompting until after the main window renders and contained recovery-dialog failures so a valid or damaged snapshot cannot make startup appear blank or terminate the core window.
- Expanded launch-smoke evidence with the failing case, observed window title/handle and isolated startup-log content.
- Fixed the Windows startup crash exposed by launch diagnostics: `ProgressBar.Value` now binds `MainViewModel.OverallProgress` explicitly `OneWay`, matching the view-model's read-only public contract.
- Removed the remaining nullable startup-diagnostics warning by passing the already-normalised non-null stage into report generation.

### Still required before phase/release exit

- Clean Windows .NET 10 restore/build/test/launch evidence.
- Settings and fixture regression comparison.
- Manual source immutability, destination, collision, cancellation and filesystem checks.
- Reproducible executable package and installer evidence.

### Explicitly not included in the first 1.2.0 milestone

- FFmpeg capability engine, FFmpeg-backed preview, hardware profiles, track/subtitle/chapter editor, lossless smart processing and watch folders.

## [1.1.1] — Optional stabilisation checkpoint

### Implemented in source
- PH-07 static source verifier with JSON source manifest
- Reproducible non-sensitive FFmpeg fixture generator and probe manifest
- Windows baseline evidence orchestrator with environment, Git and tool provenance capture
- Isolated absent/valid/malformed-settings launch smoke test
- Versioned release/source ZIP naming, payload manifest and SHA-256 output
- Release ZIP content/version/hash audit

### Pending
- Native Windows build/runtime and output-safety validation of the refined 1.1.0 source
- Mandatory manual source immutability, destination, collision, cancellation and filesystem fixtures
- No new product feature required
- Release decision remains open under `DEC-028`

## [1.1.0] — Source implemented; release unverified

- Mixed-media converter and lightweight crop/trim/stitch editor
- Static/archive checks passed in prior evidence
- Native Windows build, launch, package and installer remain unproven in the supplied environment
