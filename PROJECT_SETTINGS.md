# Project Settings — MediaForge Desktop

## Current MediaForge 2 sequence

The next active phase is `PH-23` after the PH-22 closure PR is verified merged, followed by `PH-24` through `PH-31`. `PH-20` governance adoption is complete, and `PH-21` remains the immutable V1-closure/rollback bridge already satisfied before V2 adoption.

Historical `PH-11` through `PH-19` are not reused. Their useful technical scope is carried into the V2 phases where mapped.

MediaForge 2 retains all local-first, evidence, Git, PowerShell and source-safety rules. Governance adoption does not itself change the product/package version or prove V2 runtime behaviour.

Read `/AGENTS.md`, then `project_docs/PROJECT_INDEX.md`, before editing. Identify the active `PH`, route, affected `REQ`, `AC`, `VAL`, `DEC` and `RISK` IDs. Load only relevant governance and source.

## Evidence and claims

Do not claim compilation, launch, conversion, preview fidelity, packaging, installer creation, compatibility or release without visible evidence. A result proves only its covered environment and checks. User-reported results remain user-reported until reproduced in visible tool context.

## Change discipline

Preserve behaviour and file contracts unless a managed change is approved. Prefer narrow, reversible, testable changes. Avoid unrelated refactors, speculative layers, cloud dependencies, databases, runtime AI and new third-party packages without a requirement.

Retain WPF and the single local process. Use explicit composition. PH-08 service/ViewModel seams are now source-implemented; do not collapse process, session, queue or effective-option ownership back into window code-behind.

## Active sequence

1. `PH-20`: complete governance-only MediaForge 2 adoption, merged via PR #2 at `d3c1c0958aae71afcc5462bfa2c4e66c6fd7ab55`.
2. `PH-21`: satisfied V1 closure/rollback bridge; no future implementation work is scheduled under this ID.
3. `PH-22`: user-approved shell-scope closure, effective on verified merge of `BR-20260924-01`.
4. `PH-23`: next active phase after verified PH-22 closure merge - WorkflowIntent, capability/compatibility and immutable ProcessingPlan seam.
5. `PH-24` onward: follow the canonical phase contracts in `project_docs/IMPLEMENTATION_PLAN.md`.

Do not begin dependent PH-23 implementation until the PH-22 closure PR is independently verified merged and the local branch is safely based on that merged commit.
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
