# Current state and gap analysis

**Current report:** `BR-20260729-13`  
**Product version:** 1.1.0 source; proposed 1.2.0 workflow milestone.  
**Platform:** WPF, `net10.0-windows`, local FFmpeg/FFprobe child processes.

## Implemented source state

- PH-07 deterministic source/fixture/package evidence tooling.
- PH-08 `ProcessRunner`, `EffectiveOptionsResolver`, `ProjectSession`, `QueueCoordinator` and `MainViewModel` seams.
- PH-09 schema-v1 project persistence, atomic save/backups, separate autosave/recovery, recent projects, forward-schema read-only handling, fingerprints, relink and portable paths.
- PH-10 schema-v1 built-in/user presets, safe import/export, typed locked overrides and global → preset → job → edit precedence.
- PH-10 stable professional queue controls, independent queue files, priority dispatch, pause-after-current/resume, immutable source/edit/options run snapshots and advisory size/time estimates.
- Multi-file Win64 release publishing (`PublishSingleFile=false`) and corresponding package-audit requirements.
- Override-aware MediaForge application-data roots for isolated validation, post-render recovery prompting and per-case launch-smoke diagnostics.

## Evidence boundary

The current PH-10 correction passed 34 Linux-scoped static checks over 123 non-generated files. No .NET SDK, PowerShell or Windows/WPF runtime is available here. User-provided Windows evidence proves PH-10 compilation, followed by a failed first launch-smoke correction. The prior smoke JSON was not supplied, so the exact failed case remains unknown; the supplied valid recovery snapshot established a concrete profile-leak and pre-render modal risk that this correction addresses.

## Remaining architecture gaps

- Native PH-09/PH-10 build, package-free tests and WPF workflow evidence remain absent.
- `MediaConversionService` still owns a large FFmpeg argument builder; immutable capability-aware `ProcessingPlan` work remains PH-11 and later.
- Editor interaction remains largely in `MediaEditorWindow` code-behind.
- No capability discovery, FFmpeg-backed preview-fidelity engine, output verification service or history store exists yet.
- Preset availability is not yet filtered against the selected FFmpeg build; built-in names are workflow intent, not compatibility guarantees.

## Immediate next gate

Build and run the consolidated PH-10 source on Windows. Exercise projects/recovery plus preset CRUD/import/export/locks, independent per-job options, queue persistence, priority/pause/cancel and immutable snapshot fixtures. Fix only evidenced regressions before PH-11 unless another explicit risk exception is recorded.
