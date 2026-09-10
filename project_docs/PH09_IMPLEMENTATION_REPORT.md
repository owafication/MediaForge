# PH-09 implementation report

**Report:** `BR-20260728-10`  
**Phase:** `PH-09` — saved projects, autosave and recovery  
**Status:** Implemented in source; native Windows validation pending.  
**Input:** PH-08 complete build/publish-fix source archive.  
**Product version:** Retained at 1.1.0 pending release evidence.

## Objective

Make queue/edit work durable and recoverable without changing the conversion contract or beginning PH-10 preset/queue UX.

## Implemented

- Schema-v1 `.mediaforge` JSON model for project identity, queue order, source references/fingerprints, edit plans, project defaults, output rules, preset references, per-job placeholder overrides, terminal output state and UI state.
- Compatible unknown-field preservation through `JsonExtensionData`; newer schemas open read-only and cannot be saved by this build.
- Atomic canonical save through sibling temporary write, flush-to-disk and replace/move fallback, retaining one `.bak` backup.
- Separate `.mediaforge-recovery` snapshots under local application data; 10-second debounce, five-snapshot maximum and 14-day age retention.
- Recovery snapshot before dirty close/open/new/start-batch transitions; startup recovery chooser supports Recover, Open read-only, Discard, Inspect folder and Later.
- Recent-project store with atomic writes, case-insensitive deduplication and ten-entry retention.
- Missing/changed source reporting using filename, size and modified-time fingerprints.
- Recursive relink planning that distinguishes unique exact, unique changed, ambiguous and unresolved candidates; changed files require separate confirmation.
- Explicit portable mode. Relative paths are written only when enabled and remain within the canonical project root; traversal and reparse-point paths are rejected.
- Project toolbar and dirty/read-only/portable status integrated into the existing WPF window.
- Loaded duplicate queue items are preserved while interactive imports still reject additional duplicates.
- `outputRules` are authoritative when restoring legacy `AppSettings` controls.
- Win64 publish changed to `PublishSingleFile=false`; package verification requires `MediaForge.exe`, `MediaForge.dll`, `.deps.json` and `.runtimeconfig.json` and rejects `singleFile=true`.

## Main paths

- `Models/Projects/`
- `Services/Projects/`
- `Services/Session/ProjectSession.cs`
- `MainWindow.Projects.cs`
- `MainWindow.xaml`
- `RecoveryDialog.xaml(.cs)`
- `tests/MediaForge.Tests/Program.cs`
- `scripts/build-release.ps1`
- `scripts/verify-release-package.ps1`
- `scripts/verify-source.py`

## Verification performed

Ran in the available Linux sandbox:

```text
python3 -m py_compile scripts/verify-source.py
python3 scripts/verify-source.py --root . --json artifacts/ph09-static-verification.json
```

Passed: 27 deterministic static checks over 109 non-generated files, including XML/XAML parsing, unique names, event-handler discovery, process ownership, PH-08 seam contracts, PH-09 persistence contracts, test wiring, .NET 10 contract, source-tree hygiene, multi-file publish guards and Markdown links.


## Windows compile corrections

- First Windows build evidence exposed a missing explicit `System.IO` import for `FileInfo` in `ProjectDocumentMapper.cs`.
- Follow-up Windows build evidence exposed the complete PH-09 I/O dependency set across project UI, recovery, persistence, relink and tests, plus CS0173 for an optional selected `Guid` versus `null` expression.
- This revision adds explicit `System.IO` imports to every implicated file and target-types the optional selected queue-item ID as `Guid?`.
- Static verification now checks the entire import set and nullable-ID expression. A Windows rebuild of this corrected archive remains required.

## Unproven

- C# compilation and package-free characterisation test execution for this PH-09 revision.
- WPF New/Open/Save/Save As/Recent/Relink/Recovery interaction.
- Windows `File.Replace` and interrupted-write behaviour on supported filesystems.
- Multi-file self-contained Win64 publish, ZIP audit, launch and installer.
- Existing conversion safety fixtures after PH-09 UI/session integration.

## Rollback and compatibility

The change is additive around the existing session/options/queue seams. Rollback is restoration of the PH-08 archive. Existing global settings remain JSON-compatible. Schema-v1 project files should be retained during rollback; PH-08 cannot open them. No source media is copied, moved or deleted by project save or relink.

## Next gate

Run source verification, characterisation tests, Win64 release publish/package audit and manual WPF project/recovery fixtures on Windows. Record failures precisely before beginning PH-10.
