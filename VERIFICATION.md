# Verification record

**Current report:** `BR-20260919-01`
**Status:** The automated Windows PH-07 through PH-10 baseline passed. Remaining PH-09/PH-10 manual interaction and real conversion-safety fixtures are explicitly deferred under `BR-20260919-01`; they remain Skipped/Unproven and are carried into the V2 post-build validation programme.

## Implemented and evidenced through `BR-20260911-01`

- PH-08 .NET 10 architecture seams and package-free characterisation executable.
- PH-09 schema-v1 projects, atomic save/backups, separate recovery, recent/relink/portable workflows and newer-schema read-only handling.
- PH-10 schema-v1 built-in/user presets and independent queue files.
- Preset CRUD/group/default/import/export, built-in immutability, typed field locks, safe unknown-field retention and rejection of command/executable/raw-argument property names.
- Deterministic global → preset → job override → edit guard resolution with field-source metadata.
- Project/queue retention of preset ID/version/name and typed snapshots.
- Stable queue reorder, priority, enable/disable, duplicate, retry, diagnostics, priority dispatch and pause-after-current/resume.
- Immutable source/edit/options `QueueRunItem` snapshots for active conversions.
- Explicitly low-confidence output-size and processing-time estimates that never block a run.
- Win64 multi-file release publishing with `PublishSingleFile=false`; package audit requires the complete application payload and rejects `singleFile=true`.
- Explicit `App.OnStartup` ownership, startup exception diagnostics under Local AppData with a temporary-directory fallback, and fail-soft PH-10 workflow initialization.
- Preset catalogue loading retains built-ins when user preset storage cannot be created or enumerated.
- `build-release.ps1` now blocks packaging unless the isolated launch smoke test passes absent, valid, malformed-settings and blocked-preset-storage cases.
- Central MediaForge application-data path resolution honours explicit test roots, preventing real settings, presets, recent projects, recovery snapshots and logs from entering isolated launch cases.
- Recovery prompting is deferred until after `ContentRendered`, with diagnostic fail-soft containment around the recovery chooser.
- Launch-smoke schema 2 records per-case window handle/title and isolated startup diagnostics, and the thrown error now identifies each failed case.
- Main-window overall progress uses an explicit OneWay binding, preventing WPF from attempting to write to the read-only view-model property during first layout.
- Startup diagnostics pass the normalised `safeStage` value to report generation, eliminating CS8604 without weakening nullable analysis.

## Evidence available in this environment

Environment: Linux sandbox with Python and FFmpeg/FFprobe; no .NET SDK, PowerShell, Windows/WPF runtime or Inno Setup.

Ran:

```text
python3 -m py_compile scripts/verify-source.py
python3 scripts/verify-source.py --root . --json <report-path>
```

Passed for the stated scope:

- 36 deterministic static checks over 123 non-generated files.
- XML/XAML/project parsing, unique XAML names and referenced-handler discovery.
- Production process centralisation and PH-08 ownership contracts.
- PH-09 schema, atomic-write, recovery, recent/relink, forward-schema and portable-path contracts.
- PH-10 preset ownership/import safety, typed precedence, project/queue snapshots, immutable run items, queue priority/pause and estimate contracts.
- Explicit startup ownership, post-render recovery containment, profile-isolated storage roots, diagnostic capture and mandatory release launch-smoke gating.
- Characterisation-test wiring for PH-08–PH-10 workflows.
- Explicit multi-file publish and package-audit guards.
- Version consistency, source-tree hygiene and relative Markdown links.

These checks are not a C# compiler, PowerShell parser, Windows filesystem interruption test, WPF interaction test or FFmpeg conversion fixture run.

## User-provided Windows evidence

The first user-run PH-09 characterisation build stopped with CS0246 because `ProjectDocumentMapper.cs` referenced `FileInfo` without an explicit `System.IO` import.

The second build exposed the full PH-09 I/O dependency set and CS0173 for an optional `Guid`/`null` expression. The corrected PH-09 build-fix2 source added explicit imports across every implicated file and target-typed the selected ID as `Guid?`.

The user then ran the corrected source on Windows. Restore and build succeeded, and all 37 package-free characterisation tests passed. Publish also succeeded with the intended multi-file contract, but all four launch-smoke cases failed. The supplied startup diagnostic proves the immediate startup exception: WPF attempted a TwoWay binding to the read-only `MainViewModel.OverallProgress` property while showing `MainWindow`. This source correction makes that binding explicitly OneWay. The same Windows build reported one CS8604 warning in `StartupDiagnostics`; report generation now receives the non-null normalised stage. A launch-smoke rerun is still required.

The predecessor Windows run also completed the self-contained multi-file publish into an isolated temporary directory before the mandatory smoke gate stopped packaging. The prior single-file-host antivirus/file-lock problem remains avoided by `PublishSingleFile=false`; the corrected run below completed the ZIP/package audit.

## Current native evidence — `BR-20260911-01`

Environment: Windows PowerShell with .NET SDK 10.0.302, x64 WPF publish, repository HEAD `b42bd93613e82b78c41c98948c329a53a3f373c4`, clean worktree at validation start.

Ran and passed:

```text
.\scripts\validate-windows-baseline.ps1 -Architecture x64
```

- Static source verification: 39 checks over 124 files passed.
- FFmpeg/FFprobe provenance capture and generation of 13 disposable fixture files passed.
- Release restore/build: 0 warnings and 0 errors.
- Package-free characterisation: 37 passed, 0 failed.
- Launch smoke: schema 2, all four isolated cases passed with a closeable `MediaForge — Unsaved project` window and no startup diagnostics.
- Package audit: 406 files passed; required entries present; `singleFile=false`; ZIP SHA-256 `06df87a547c516ade7a4bba529aa4b909e2c6ec2f7b67d474604116371799022`.

Evidence root: `artifacts/ph07-baseline-20260911-132855`. The first consolidated run exposed a harness-only Python exit-code capture defect; the wrapper was corrected to keep child output in the transcript while returning only the native exit code, and this final run exited 0. A separate publish attempt was also blocked by a user-open MediaForge window holding `Accessibility.dll`; the process was not terminated, and the retry after the user closed it passed.

## Required Windows commands

```powershell
py -3 .\scripts\verify-source.py --root .
.\scripts\run-characterization-tests.ps1 -Configuration Release
.\scripts\build-release.ps1
.\scripts\verify-release-package.ps1 -ZipPath .\artifacts\MediaForge-1.1.0-win-x64.zip
```

Full evidence run:

```powershell
.\scripts\validate-windows-baseline.ps1 `
  -SourceArchive .\MediaForge-1.1.0-ph10-progress-binding-startup-fix-multifile-source-unverified.zip
```

## Manual PH-10 fixtures

- Create, rename/group, duplicate, delete, default, import and export presets; built-ins remain read-only.
- Reject command/raw-argument/executable-path import fields; retain safe extension fields.
- Apply different presets/job overrides to two jobs and confirm independent effective settings and outputs.
- Delete a referenced catalogue preset and reopen project/queue state from retained typed snapshots.
- Reorder, reprioritise, enable/disable, duplicate and retry; save/load the independent queue.
- Pause after current and verify no new dispatch; resume and cancel separately.
- Mutate live pending edit/queue state after start and verify the active conversion keeps its captured source/edit/options snapshot.
- Confirm estimates show size, time and low/unproven confidence and never block processing.
- Re-run all PH-09 project/recovery/relink/portable fixtures and existing conversion safety fixtures.
- Publish Win64 and verify the complete multi-file payload and `singleFile=false` manifest.

## Unproven

- WPF preset/project/queue interaction, accessibility and thread-affinity behaviour; the Windows UI helper exited before exposing a target window, so no UI actions were taken.
- Real FFmpeg mixed-batch output, source immutability, collision/destination containment, pause/cancel races, close-during-work, child-process and temporary-output cleanup fixtures.
- Windows atomic replacement/interruption outcomes and PH-09 recovery/relink/portable interactive regression fixtures.
- Installer, signing, FFmpeg licence/acquisition decision and any release claim for architectures other than Win64.

## PH-10 sequencing exception — `BR-20260919-01`

**Authorisation:** On 2026-09-19 the user explicitly chose to defer the remaining ad-hoc PH-09/PH-10 manual validation so MediaForge 2 development can proceed. The deferred checks are not deleted and are not converted into pass evidence.

### Passed evidence retained

The following previously executed Windows evidence remains Passed for its exact scope:

- static source verification;
- FFmpeg/FFprobe provenance capture;
- deterministic disposable fixture generation;
- .NET 10 x64 restore/build with zero warnings and zero errors;
- all 37 package-free characterisation tests;
- four-case isolated schema-2 launch smoke;
- multi-file Win64 publish;
- 406-file package audit with `singleFile=false`.

Closure preparation also rechecked the seven recorded disposable source fixture hashes and all seven matched their pre-test hashes. That observation proves only those files were unchanged at the time of the comparison; it does not replace the skipped conversion-safety fixture matrix.

### Skipped / Unproven

The following remain unproven until later executable evidence covers them:

- native WPF preset CRUD/import/export/locking end-to-end behaviour;
- missing-catalogue preset snapshot behaviour through the UI;
- native queue reorder/priority/enable/duplicate/retry round-trip behaviour;
- pause/resume/cancel race behaviour;
- immutable active-run behaviour under live state mutation;
- PH-09 project/recovery/relink/portable interactive regression;
- Windows interrupted atomic-write behaviour;
- real FFmpeg mixed-media conversion;
- source immutability across success, failure, skip, overwrite-target and cancellation cases;
- destination containment and full Rename/Skip/Overwrite collision fixtures;
- child-process cleanup and temporary-output cleanup;
- close-during-work cleanup;
- full accessibility validation;
- installer/signing validation where later claimed.

A partial manual Pass-A session reached the preset export/import workflow but its evidence harness then failed to locate the expected deterministic export artefact. It remains diagnostic history only and is not a completed manual validation pass.

### V2 carry-forward

The externally reviewed V2 planning pack now contains `V2_POST_BUILD_VALIDATION_MATRIX.md`, covering inherited and proposed `VAL-001` through `VAL-081`.

Its execution classification currently targets:

- 63 validation IDs as automated;
- 10 as UI automation plus human spot-check;
- 7 as environment-dependent automated;
- 1 as human review.

This planning evidence is external until canonical V2 governance adoption.

### Claim boundary

This exception closes the PH-10 sequencing gate for the purpose of establishing a V1 rollback baseline and proceeding to V2 governance.

It does **not** mean the deferred checks passed, PH-10 received complete manual acceptance, or MediaForge 1.1.0 is a fully verified release.

Deferred safety validation must be satisfied by later executable V2 evidence before a corresponding V2 release claim.
