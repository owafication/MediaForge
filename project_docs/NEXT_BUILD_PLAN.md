# Next build plan — workflow foundation validation

**Target:** Proposed MediaForge 1.2.0  
**Active phases:** PH-10 sequencing-exception closure and V1 rollback-baseline establishment
**Status:** The automated Windows PH-07 through PH-10 gate passed. Remaining manual PH-09/PH-10 interaction and source/output-safety fixtures are explicitly deferred under `BR-20260919-01` and remain Unproven. Establish the V1 rollback commit, then adopt the reviewed V2 governance.

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

## Latest evidence - `BR-20260911-01`

Evidence root: `artifacts/ph07-baseline-20260911-132855`.

- Automated Windows baseline passed with exit code 0.
- Static verification passed 39 checks over 124 files; .NET 10.0.302 x64 restore/build passed with 0 warnings and 0 errors.
- FFmpeg/FFprobe provenance and 13 disposable fixture files were captured.
- Characterisation tests passed 37/37.
- Multi-file Win64 publish, schema-2 four-case launch smoke and 406-file package audit passed; installer was intentionally skipped.
- The baseline harness was corrected after its Python wrapper misclassified successful exit codes by capturing child output as return data.

## Deferred / Unproven evidence under `BR-20260919-01`

- WPF preset CRUD/import/export/locking and missing-preset snapshot fixtures;
- two jobs using different effective options in one batch;
- reorder/priority/enable/duplicate/retry and queue round-trip fixtures;
- pause/resume/cancel race and immutable-run-snapshot fixtures;
- PH-09 project/recovery/relink/portable regression fixtures;
- source immutability, collision/destination containment, cancellation, child-process and temporary-output cleanup fixtures;
- accessibility/DPI/keyboard baseline and installer/signing evidence where a release claim requires them.

The manual checklist remains intentionally incomplete. `BR-20260919-01` is the explicit sequencing exception: these checks remain Skipped/Unproven, PH-10 is not a fully verified release gate, and the next repository step is V1 rollback-baseline establishment followed by reviewed V2 governance adoption.

## Immediate commands

```powershell
py -3 .\scripts\verify-source.py --root .
.\scripts\run-characterization-tests.ps1 -Configuration Release
.\scripts\build-release.ps1
Get-Content .\artifacts\MediaForge-1.1.0-win-x64-launch-smoke.json
.\scripts\verify-release-package.ps1 -ZipPath .\artifacts\MediaForge-1.1.0-win-x64.zip
```

## Immediate handoff after `BR-20260919-01`

1. Review the exact final PH-10 closure diff.
2. Stage only the intended eight PH-10 evidence/governance/script files.
3. Commit the branch as the V1 rollback-baseline candidate.
4. Push the exact commit.
5. Create or update the pull request.
6. Verify the PR head SHA equals the pushed commit and required repository checks apply to that exact head.
7. Merge only when the exact-head checks and repository state permit it.
8. Sync local `main` and record the resulting V1 rollback commit.
9. Canonically adopt the reviewed V2 governance.
10. Begin V2 implementation.

The deferred V1 validations are carried into the V2 post-build matrix and remain Unproven until later evidence runs them.
