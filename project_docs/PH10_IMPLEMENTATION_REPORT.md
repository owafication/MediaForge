# PH-10 implementation report

**Report:** `BR-20260729-14`  
**Phase:** `PH-10` presets, per-job options and professional queue  
**Status:** Windows build and 37 characterisation tests passed; startup binding correction implemented in source and launch-smoke rerun pending.

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

## Verification performed

```text
python3 -m py_compile scripts/verify-source.py
python3 scripts/verify-source.py --root . --json <report-path>
```

Passed in the available Linux environment: 36 static checks over 123 non-generated files, including XML/XAML parse and handler checks, the explicit read-only progress binding contract, startup diagnostics, PH-08 ownership, PH-09 persistence, PH-10 preset/queue contracts, test wiring, source hygiene and multi-file release guards.

## Evidence boundary

The current sandbox still has no .NET SDK, PowerShell, Windows/WPF runtime or Inno Setup. User-provided Windows evidence confirms restore/build and 37 package-free tests, then proves a first-layout WPF binding exception. This revision corrects that exception in source; successful launch/close remains to be rerun.

## Required Windows exit evidence

1. Clean restore/build and all package-free characterisation tests.
2. Preset CRUD/default/import/export/locked-field fixtures.
3. Two-job independent effective-options batch fixture.
4. Project and independent queue round-trip with a deleted catalogue preset.
5. Reorder/priority/enable/duplicate/retry fixture.
6. Pause/resume/cancel race fixture proving no dispatch while paused.
7. Immutable source/edit/options run-snapshot fixture.
8. PH-09 save/recovery/relink/portable regression suite.
9. Existing source/output/collision/cancellation conversion safety suite.
10. Multi-file Win64 publish, launch, package audit and installer evidence.

## Next phase gate

Do not claim PH-10 exit or start PH-11 capability work until the evidence above is green or another explicit risk exception is recorded.
