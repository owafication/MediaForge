# MediaForge coding-agent instructions

## Mission
Develop MediaForge as a local-first Windows media converter, preparation tool and lightweight single-track assembler. Prioritise source-file safety, recoverable work, truthful capability reporting, deterministic processing and verified outputs. Do not turn it into a full NLE, cloud service or AI product without an approved scope change.

## Authority
1. Platform, safety, privacy, data-integrity and legal requirements.
2. Current inspected source, tests, build output and logs.
3. Canonical owners in `project_docs/PROJECT_INDEX.md`.
4. Approved requirements and decisions.
5. User instructions.
6. Historical reports and older notes.

Record same-level conflicts; do not guess.

## Core rules
- Read before editing. Distinguish `Observation`, `Assumption`, `Inference`, `Proposed`, `Implemented`, `Ran`, `Passed`, `Failed`, `Skipped`, `Verified` and `Unproven`.
- Preserve contracts unless a managed change is requested. Make narrow, reversible, modular, testable changes.
- Never claim build, launch, conversion, preview, verification, package, installer or release success without visible evidence.
- Protect source media, destination containment, collision reservation, temporary-output commit, project recovery, cancellation, process trees, logs, FFmpeg provenance, installer privilege and update paths.
- Do not accept raw executable arguments from project/preset files. Do not add telemetry, accounts, cloud processing, databases, AI, bundled binaries, silent updates or destructive automation without requirements and decisions.
- Update only canonical owners, then update index, traceability, evidence and changelog when affected.

## Reading sequence
1. `/AGENTS.md`
2. `project_docs/PROJECT_INDEX.md`
3. Identify phase, route and affected risks
4. Read only routed governance
5. Inspect directly relevant source, XAML, tests, scripts, manifests and logs
6. Expand context only for a dependency, conflict or evidence gap

## Task routes
Use `ROUTE-001`–`ROUTE-015` in `PROJECT_INDEX.md`. Current priority is `PH-07` through `PH-10`: native baseline, supported platform/architecture seam, projects/recovery, then presets/per-job queue.

## Reports
Before edits state scope, IDs, risks and checks. After work report inspected, changed, ran, passed, failed, skipped, unproven, governance, rollback and next stop/decision. A check proves only its scope.

## Stop conditions
Stop for missing authority, destructive-action approval, release/signing credentials, FFmpeg provenance/licence decision, schema migration decision, source-safety evidence, failed mandatory validation, ambiguous output commit, or unapproved privilege/network behaviour. Never publish, sign, bundle FFmpeg, add a remote, delete source media or run power/command/webhook actions without explicit authorisation.
