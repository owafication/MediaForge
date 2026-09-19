# Current state and gap analysis

**Current report:** `BR-20260919-02`
**Product version:** V1 source remains 1.1.0; exact V2 product/package version is unresolved.
**Platform:** WPF, `net10.0-windows`, local FFmpeg/FFprobe child processes.

## V1 rollback baseline

Rollback commit: `5acaf88e751327eac47ca673178fbbd88a8603f1`.

Implemented source includes PH-08 architecture/.NET 10 seams, PH-09 project/recovery persistence, PH-10 presets/per-job option resolution/queue control, multi-file Win64 publishing, profile-isolated launch diagnostics and the established safety boundaries.

## Evidence boundary

`BR-20260911-01` passed the automated Windows x64 baseline for its recorded scope: static verification, FFmpeg/FFprobe provenance, disposable fixtures, Release restore/build, 37 characterisation tests, multi-file publish, four launch-smoke cases and package audit.

`BR-20260919-01` deferred the remaining PH-09/PH-10 manual/native interaction and real conversion-safety checks. They remain Skipped/Unproven and must be satisfied later by applicable V2 validation before a release claim.

## V2 gaps and direction

- V1 MainWindow remains queue/control-first rather than task-first.
- WorkflowIntent is not yet implemented as a V2 application contract.
- The immutable ProcessingPlan is not yet the single implemented authority across UI summary, preview intent, execution and verification.
- V1 persistence compatibility classes for V2 remain evidence-dependent.
- V2 shell/accessibility behaviour is not implemented or tested.
- Fast post-build and full-release V2 harness entry points are planned, not yet implemented.

## Phase disposition

- `PH-20`: complete canonical governance/design adoption, merged at `d3c1c0958aae71afcc5462bfa2c4e66c6fd7ab55`.
- `PH-21`: V1 closure/rollback bridge already satisfied before PH-20 adoption.
- `PH-11`–`PH-19`: historical superseded sequencing; useful scope is redistributed.
- `PH-22`: active task-first shell/navigation implementation phase.
- `PH-23`–`PH-31`: planned V2 implementation roadmap.
## Immediate next gate

Implement `PH-22` from merged governance commit `d3c1c0958aae71afcc5462bfa2c4e66c6fd7ab55`: task-first WPF shell, conventional menu/navigation, Home/task routing, shared workflow state, progressive disclosure and accessibility foundation.

Before PH-22 exit, run the phase-mapped Release build/XAML/accessibility/navigation checks and record evidence against its acceptance/validation contract. Do not begin PH-23 solely because shell source exists.

Do not claim V2 runtime behaviour, migration compatibility or a 2.x package version before corresponding implementation/decision/evidence exists.
