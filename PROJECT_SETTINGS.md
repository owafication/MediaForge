# Project Settings — MediaForge Desktop

Read `/AGENTS.md`, then `project_docs/PROJECT_INDEX.md`, before editing. Identify the active `PH`, route, affected `REQ`, `AC`, `VAL`, `DEC` and `RISK` IDs. Load only relevant governance and source.

## Evidence and claims

Do not claim compilation, launch, conversion, preview fidelity, packaging, installer creation, compatibility or release without visible evidence. A result proves only its covered environment and checks. User-reported results remain user-reported until reproduced in visible tool context.

## Change discipline

Preserve behaviour and file contracts unless a managed change is approved. Prefer narrow, reversible, testable changes. Avoid unrelated refactors, speculative layers, cloud dependencies, databases, runtime AI and new third-party packages without a requirement.

Retain WPF and the single local process. Use explicit composition. PH-08 service/ViewModel seams are now source-implemented; do not collapse process, session, queue or effective-option ownership back into window code-behind.

## Active sequence

1. `PH-07`: evidence automation implemented; native safety evidence remains pending.
2. `PH-08`: full source scope implemented, including `net10.0-windows`; clean Windows build/test/launch and fixture comparison remain the exit gate.
3. `PH-09`: source-implemented versioned projects, atomic save, autosave/recovery, recent projects, relinking and portable mode; native validation pending.
4. `PH-10`: source-implemented presets, deterministic per-job options, immutable run snapshots and professional queue control; native validation pending.
5. `PH-11`: capability and compatibility engine only after the consolidated PH-09/PH-10 Windows gate is green or another explicit risk exception is recorded.

Do not claim phase exit or begin dependent persistence work while the current native gate is failed.

## Data and safety

Never intentionally modify source media. Preserve deterministic destination reservation and same-directory temporary output. A failure must not silently replace a valid destination. Never trust raw command fragments from imported files. Keep local media and path data local by default. Do not add telemetry, accounts, cloud processing, advertisements or AI without a new requirement and privacy decision.

## Platform and tools

The source targets `.NET 10` WPF (`net10.0-windows`) and product version `1.1.0`. `global.json` pins SDK `10.0.100` with feature-band roll-forward. Settings JSON and Inno Setup product/version contracts are unchanged. Do not change runtime identifiers, installer architecture, FFmpeg distribution or package dependencies without recording the decision and rollback.

## Validation

At minimum:

- XML/XAML parse, unique names and handler resolution for UI changes.
- Clean restore/build and automated tests for C# changes.
- Project/preset/queue round-trip and migration tests for persistence changes.
- Fixture conversions and plan snapshots for FFmpeg changes.
- Source hashes, destination containment, collision, cancellation and temp cleanup for processing changes.
- Version, package, installer, licence, inventory and hash checks for release changes.

Record exact commands, environment, results, artefact paths/hashes, failures, skipped checks and unproven items in `/VERIFICATION.md`.
