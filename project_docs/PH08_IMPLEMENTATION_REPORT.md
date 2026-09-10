# PH-08 implementation report — through `BR-20260728-07`

## Objective

Implement the complete PH-08 supported-platform and architecture-seam scope while preserving existing user-visible conversion intent, settings and installer contracts.

## Implemented

- `ProcessRunner` central production process lifecycle.
- `EffectiveOptionsResolver` typed validation and compatibility snapshot.
- `ProjectSession` in-memory jobs, duplicate prevention, revision and dirty state.
- `QueueCoordinator` parallel execution, cancellation and job transitions.
- `MainViewModel` queue summary/progress/running projection.
- `IMediaConversionService` queue/conversion boundary.
- Package-free characterisation tests for the above seams and source/output safety.
- Application/tests migrated to `net10.0-windows` with `global.json`.
- Conservative decoded-frame preflight for oversized images and actionable FFmpeg invalid-size translation.

## Explicitly excluded

- PH-09 project persistence, autosave and recovery.
- PH-10 presets, per-job overrides and professional queue features.
- A new tiled image decoder or non-FFmpeg image backend.

## Contracts

- Product version remains 1.1.0.
- `AppSettings` schema and storage path are unchanged.
- Inno Setup identifiers/version remain unchanged.
- Existing conversion option names and collision semantics are retained.
- No persistent data migration is required for rollback.

## Evidence

- User-provided consolidated PH-08 Windows restore and compile passed with .NET SDK 10.0.301.
- That run exposed CA2014 and a `GenerateBundle` access-denied failure on the default `singlefilehost.exe` intermediate.
- CA2014 is corrected by hoisting the fixed-size buffers outside the JPEG marker loop.
- Release publish now uses unique temporary intermediates and up to three fresh-path attempts.
- Available Linux-scoped static/XML/XAML/fixture/lexical checks passed.
- Corrected publish, tests, launch and conversion evidence remain pending.

## Large-image incident

The supplied runtime log showed FFmpeg rejecting a 38,400 × 21,600 PNG before decoding/output. MediaForge now reads supported image headers before conversion and rejects estimated decoded frames above a conservative backend threshold with dimensions and remediation. This prevents a long generic FFmpeg failure and preserves the source, but does not make the current backend capable of converting that image.

## Rollback

Restore the PH-08A.2 source/package. No settings or project schema migration was introduced.
