# Setup and handoff

**Purpose:** Validate the consolidated PH-09/PH-10 source on Windows and preserve evidence.  
**Status:** Windows build and 37 characterisation tests passed; rerun the launch/package gate after the OneWay progress-binding correction.

## Prerequisites

- Windows 10/11 x64.
- .NET 10 SDK compatible with `global.json`.
- FFmpeg and FFprobe in the same folder.
- PowerShell.
- Inno Setup only when installer creation is explicitly required.

## Run source checks, tests and multi-file publish

```powershell
py -3 .\scripts\verify-source.py --root .
.\scripts\run-characterization-tests.ps1 -Configuration Release
.\scripts\build-release.ps1
.\scripts\verify-release-package.ps1 -ZipPath .\artifacts\MediaForge-1.1.0-win-x64.zip
```

The release is intentionally multi-file. Keep `MediaForge.exe`, `MediaForge.dll`, dependency/runtime manifests and runtime payload together.

The prior launch failure was caused by `ProgressBar.Value` defaulting to TwoWay against read-only `MainViewModel.OverallProgress`; this source uses an explicit OneWay binding. If launch still fails, inspect the schema-2 smoke JSON and isolated startup diagnostics before changing the gate.

## Complete evidence harness

```powershell
.\scripts\validate-windows-baseline.ps1 `
  -SourceArchive .\MediaForge-1.1.0-ph10-progress-binding-startup-fix-multifile-source-unverified.zip
```

## Manual PH-09 regression focus

Exercise canonical save/backup, dirty-close recovery, startup recovery decisions, recent projects, exact/changed/ambiguous relink, portable-folder movement, traversal rejection and newer-schema read-only mode.

## Manual PH-10 focus

- Create, rename/group, duplicate, delete, default, import and export user presets; verify built-ins remain read-only.
- Reject a preset containing command/raw-argument/executable-path fields and retain safe unknown fields.
- Run two jobs with different preset/job overrides and compare effective summaries and outputs.
- Delete a referenced catalogue preset and reopen the project/queue from its retained typed snapshot.
- Reorder, reprioritise, enable/disable, duplicate and retry selected queue items; save/load the independent queue file.
- Pause after current and verify no new jobs start; resume and cancel separately.
- Change live queue/edit state after run start and verify active conversion uses the captured source/edit/options snapshot.
- Confirm size/time estimates remain labelled low/unproven and never block a run.

## Handoff decision

- Green build/tests/manual fixtures/package audit: record PH-10 exit evidence and consider PH-11.
- Compile/test/UI failure: fix only the evidenced regression and rerun the same gate.
- Imported command execution, running-job mutation, ambiguous pause state, canonical-save corruption or traversal escape: stop and treat as blocking.
