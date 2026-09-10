# Source evidence

**Current report:** `BR-20260728-08`  
**Scope:** Evidence directly visible in the current source tree and user-provided predecessor logs.

## Current source observations

- `MediaForge.csproj` and test project target `net10.0-windows`; product version remains 1.1.0.
- `Models/Projects/ProjectDocument.cs` defines schema-v1 project DTOs and unknown-field extension points.
- `Services/Projects/ProjectService.cs` probes schema, opens newer schemas read-only and delegates canonical writes to `AtomicFile`.
- `ProjectRecoveryStore` writes separate `.mediaforge-recovery` snapshots with five-count/14-day retention.
- `ProjectPathPolicy` contains portable paths and rejects traversal/reparse points.
- `ProjectRelinkService` distinguishes unique exact, unique changed, ambiguous and unresolved candidates.
- `MainWindow.Projects.cs` integrates project commands, dirty tracking, autosave, recovery and relink UI.
- `scripts/build-release.ps1` explicitly uses `-p:PublishSingleFile=false`; package manifest records `singleFile = $false`.
- `scripts/verify-release-package.ps1` requires the multi-file application payload and rejects a single-file manifest.

## Ran in this revision

```text
python3 -m py_compile scripts/verify-source.py
python3 scripts/verify-source.py --root . --json artifacts/ph09-static-verification.json
```

Result: 25 checks passed over 108 non-generated files.

## User-provided predecessor evidence

The preceding consolidated PH-08 source restored and compiled on Windows with .NET SDK 10.0.301. Its single-file publish failed on access to `singlefilehost.exe`. The current multi-file change removes the bundle-generation requirement, but no current publish result is available.

## Unproven

- PH-09 native compile and tests.
- WPF project/recovery behaviour.
- Windows atomic/interrupted save fixtures.
- Multi-file publish/ZIP/launch/installer.
- Conversion regression fixtures for the current revision.
