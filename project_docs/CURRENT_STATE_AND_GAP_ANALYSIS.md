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

- `PH-20`: active canonical governance/design adoption.
- `PH-21`: V1 closure/rollback bridge already satisfied before PH-20 adoption.
- `PH-11`–`PH-19`: historical superseded sequencing; useful scope is redistributed.
- `PH-22`–`PH-31`: active V2 implementation roadmap.

## Immediate next gate

Finish review, commit, push and merge the governance-only PH-20 adoption. Record PH-20 exit evidence. Then begin PH-22 shell/navigation work from the merged canonical governance state.

Do not claim V2 runtime behaviour, migration compatibility or a 2.x package version before corresponding implementation/decision/evidence exists.
