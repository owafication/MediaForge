# Next build plan — workflow foundation validation

**Target:** Proposed MediaForge 1.2.0  
**Active phases:** consolidated `PH-07`–`PH-10` validation  
**Status:** PH-08, PH-09 and PH-10 are implemented in source. Windows restore/build and all 37 characterisation tests passed. Launch diagnostics then identified a read-only progress property bound TwoWay during first layout; the binding is now explicitly OneWay and the launch smoke must be rerun.

## Objective

Build and exercise the PH-10 source on Windows, fix only evidenced regressions, and preserve the multi-file Win64 release contract. Do not begin PH-11 capability work until this gate is green or another explicit risk exception is recorded.

## Frozen boundaries

- Retain WPF, one local application and external FFmpeg/FFprobe.
- Release deployment is multi-file (`PublishSingleFile=false`); all payload files stay together.
- Imported project, preset and queue files contain typed data only and never trusted executable arguments.
- Queue pause stops future dispatch; it never suspends an active FFmpeg process mid-write.
- Estimates are advisory and cannot block processing.
- No phase/release exit claim until native Windows evidence passes.

## Implemented source gates

### `PH-09` projects and recovery

Schema-v1 projects, atomic save/backups, separate recovery, recent projects, relink, contained portable paths and newer-schema read-only handling are source-implemented.

### `PH-10` presets and professional queue

Source-implemented:

- schema-v1 built-in/user presets with CRUD, grouping, defaults, locks and safe import/export;
- typed global → preset → job → edit precedence and per-job source summaries;
- retained preset snapshots in projects/queues;
- stable queue order/priority/enabled state, duplicate/retry and independent queue files;
- pause-after-current/resume and immutable source/edit/options run snapshots;
- explicitly low-confidence size/time estimates;
- package-free characterisation coverage and deterministic source guards.

## Pending exit evidence

- repeat the already-green .NET 10 restore/build and 37-test run after the narrow XAML correction;
- WPF preset CRUD/import/export/locking and missing-preset snapshot fixtures;
- two jobs using different effective options in one batch;
- reorder/priority/enable/duplicate/retry and queue round-trip fixtures;
- pause/resume/cancel race and immutable-run-snapshot fixtures;
- PH-09 project/recovery/relink/portable regression fixtures;
- multi-file publish; confirm all four profile-isolated launch-smoke cases pass, inspect the schema-2 case details/logs, then run package audit and the existing conversion safety matrix.

## Immediate commands

```powershell
py -3 .\scripts\verify-source.py --root .
.\scripts\run-characterization-tests.ps1 -Configuration Release
.\scripts\build-release.ps1
Get-Content .\artifacts\MediaForge-1.1.0-win-x64-launch-smoke.json
.\scripts\verify-release-package.ps1 -ZipPath .\artifacts\MediaForge-1.1.0-win-x64.zip
```
