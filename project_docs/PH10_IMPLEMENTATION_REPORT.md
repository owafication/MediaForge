# PH-10 implementation report

**Report:** `BR-20260919-01`
**Phase:** `PH-10` presets, per-job options and professional queue  
**Status:** PH-10 remains source-complete for its declared scope. The automated Windows gate passed; the remaining native/manual interaction and real conversion-safety checks are explicitly Skipped/Unproven under `BR-20260919-01`. PH-10 is closed for sequencing/rollback-baseline purposes, not as a fully verified release.

## Objective

Make repeated workflows reusable, allow independent typed options in one batch, and add durable queue controls without broadening media-processing capability or accepting executable configuration.

## Implemented

- Versioned schema-v1 `.mediaforge-preset` and `.mediaforge-queue` UTF-8 JSON contracts.
- Grouped built-in read-only presets and user create, rename/group, duplicate, delete, default, import and export operations.
- Import pre-scan rejects command, raw-argument, executable and FFmpeg-path property names; safe schema-v1 unknown fields remain extension data.
- `ConversionOptionOverrides` typed partial values and field locks.
- One `EffectiveOptionsResolver` merge: current global input → selected preset → per-job override → edit requirements/guards.
- Per-field source metadata and per-job effective summary.
- Project and queue persistence of preset ID/version/name and typed snapshots.
- Stable queue IDs, reorder, priority, enable/disable, duplicate, retry, source/output navigation and privacy-redacted diagnostic copy.
- Independent `.mediaforge-queue` save/load with fingerprints and newer-schema read-only result.
- Priority-then-order dispatch, pause-after-current/resume and separate cancellation.
- `QueueRunItem` copies source identity and edit state and retains immutable `ConversionOptions`, preventing live pending edits from changing active conversion input.
- Low-confidence output-size and processing-time heuristics labelled as estimates and never used as run gates.
- WPF preset editor and workflow toolbar.
- Explicit OneWay main progress binding so the read-only `MainViewModel.OverallProgress` projection cannot be selected as a WPF binding source update target.
- Characterisation tests for ownership/CRUD, import safety and extension preservation, precedence/locks, project/queue round-trip, queue mutations, immutable snapshots, estimates and pause dispatch.

## Prior verification performed

```text
python3 -m py_compile scripts/verify-source.py
python3 scripts/verify-source.py --root . --json <report-path>
```

Passed in the earlier Linux-scoped environment: 36 static checks over 123 non-generated files, including XML/XAML parse and handler checks, the explicit read-only progress binding contract, startup diagnostics, PH-08 ownership, PH-09 persistence, PH-10 preset/queue contracts, test wiring, source hygiene and multi-file release guards.

## Current Windows evidence - `BR-20260911-01`

Evidence root: `artifacts/ph07-baseline-20260911-132855`. Repository HEAD at validation start: `b42bd93613e82b78c41c98948c329a53a3f373c4`.

- Windows PowerShell with .NET SDK 10.0.302 and win-x64 passed the consolidated evidence harness.
- Static source verification passed 39 checks over 124 files.
- FFmpeg/FFprobe provenance and 13 disposable fixture files were captured.
- Restore/build passed with 0 warnings and 0 errors; all 37 characterisation tests passed.
- Multi-file Win64 publish, schema-2 four-case launch smoke and 406-file package audit passed.
- The installer step was intentionally skipped.

The harness initially misclassified successful Python steps because its wrapper returned child output together with the exit code. The wrapper now writes captured output to the transcript and returns only the native exit code; the final harness run exited 0.

## Deferred / Unproven Windows evidence under `BR-20260919-01`

1. Preset CRUD/default/import/export/locked-field fixtures.
2. Two-job independent effective-options batch fixture.
3. Project and independent queue round-trip with a deleted catalogue preset.
4. Reorder/priority/enable/duplicate/retry fixture.
5. Pause/resume/cancel race fixture proving no dispatch while paused.
6. Immutable source/edit/options run-snapshot fixture.
7. PH-09 save/recovery/relink/portable regression suite.
8. Existing source/output/collision/cancellation conversion safety suite.
9. Accessibility/DPI/keyboard baseline and installer/signing evidence for any release claim.

## Sequencing gate

`BR-20260919-01` is the explicit sequencing exception. The evidence above remains Skipped/Unproven and cannot support a fully verified V1 release claim. The next step is to establish the V1 rollback baseline and then adopt the reviewed V2 governance rather than proceeding through the superseded PH-11 route.

## Sequencing-exception closure — `BR-20260919-01`

The remaining PH-09/PH-10 manual validation was explicitly deferred by the user on 2026-09-19.

The automated Windows evidence already obtained remains valid for its original scope. The skipped manual/native checks remain Unproven.

The deferred checks are now carried into the proposed V2 post-build validation architecture rather than being treated as discarded requirements.

This permits the current PH-10 implementation to become the V1 rollback baseline after normal Git review/commit/PR/merge steps.

It does not establish a fully verified 1.1.0 release.
