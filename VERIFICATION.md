# Verification record

**Current report:** `BR-20260729-14`  
**Status:** Windows restore/build and all 37 package-free characterisation tests passed. The four-case launch smoke then exposed a WPF startup crash: `ProgressBar.Value` defaulted to a TwoWay binding against read-only `MainViewModel.OverallProgress`. The binding and the remaining nullable diagnostics warning are corrected in source; launch-smoke rerun remains pending.

## Implemented through `BR-20260729-14`

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

The current Windows run also completed the self-contained multi-file publish into an isolated temporary directory before the mandatory smoke gate stopped packaging. The prior single-file-host antivirus/file-lock problem remains avoided by `PublishSingleFile=false`; final ZIP/package audit still depend on a green launch smoke.

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

- Native launch-smoke confirmation for this binding correction.
- WPF preset/project/queue interaction and thread-affinity behaviour.
- Pause/cancel race behaviour under real conversions.
- Windows atomic replacement/interruption outcomes.
- Existing conversion safety fixtures after PH-10 integration.
- Multi-file self-contained publish, the new four-case launch smoke, installer and signing.
